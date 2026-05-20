using System.Diagnostics;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;

namespace Uno.UI.DevServer.Cli.Helpers;

/// <summary>
/// Ensures the Windows Firewall has an inbound Allow rule for <c>dotnet.exe</c>
/// on the <em>Private</em> and <em>Domain</em> network profiles.
///
/// Why this is needed: the .NET SDK installer creates a rule for <c>dotnet.exe</c>
/// that covers the <em>Public</em> profile only.  Wi-Fi home/office networks are
/// classified as <em>Private</em> and corporate Active Directory networks as
/// <em>Domain</em>, so physical Android/iOS devices and remote clients on those
/// networks are silently blocked.  <c>CreateNoWindow = true</c> on the host process
/// prevents the Windows Firewall dialog from ever appearing, so no rule is created
/// automatically.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class WindowsFirewallHelper
{
	private const string RuleDisplayName = "Uno DevServer (.NET Host)";

	// Profiles covered by the .NET SDK installer rule for dotnet.exe: Public only.
	// We add Private + Domain so that physical devices on home/office Wi-Fi (Private)
	// and developers working on corporate networks as local admins (Domain) can reach
	// the DevServer.  Public is already handled by the SDK rule.
	private const string RuleProfiles = "private,domain";

	/// <summary>
	/// Checks whether a Private+Domain inbound Allow rule already exists for the
	/// current <c>dotnet.exe</c>, and adds one via an elevated <c>netsh</c> call if
	/// not.  Gracefully degrades (warning + manual instructions) if UAC is declined
	/// or if the <c>netsh</c> invocation fails.
	/// </summary>
	public static async Task EnsureFirewallRuleAsync(ILogger logger, CancellationToken ct)
	{
		var dotnetPath = ResolveDotnetPath();
		if (dotnetPath is null)
		{
			logger.LogDebug("WindowsFirewall: could not resolve dotnet.exe path — skipping firewall check.");
			return;
		}

		if (await HasRequiredRuleAsync(dotnetPath, ct))
		{
			logger.LogDebug("WindowsFirewall: Private+Domain inbound rule already exists for {Path}.", dotnetPath);
			return;
		}

		logger.LogInformation(
			"WindowsFirewall: no Private/Domain inbound rule found for dotnet.exe. " +
			"A UAC prompt will appear to add one — this happens once per machine.");

		await AddFirewallRuleAsync(dotnetPath, logger, ct);
	}

	// -------------------------------------------------------------------------
	// Private helpers
	// -------------------------------------------------------------------------

	private static string? ResolveDotnetPath()
	{
		// The CLI itself runs inside dotnet.exe — its path is the most reliable source.
		var processPath = Environment.ProcessPath;
		if (processPath is not null
			&& processPath.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase)
			&& File.Exists(processPath))
		{
			return processPath;
		}

		// Fallback: DOTNET_ROOT environment variable.
		var dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
		if (!string.IsNullOrWhiteSpace(dotnetRoot))
		{
			var candidate = Path.Combine(dotnetRoot, "dotnet.exe");
			if (File.Exists(candidate))
			{
				return candidate;
			}
		}

		// Last resort: scan PATH.
		var pathVar = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
		foreach (var dir in pathVar.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
		{
			var candidate = Path.Combine(dir.Trim(), "dotnet.exe");
			if (File.Exists(candidate))
			{
				return candidate;
			}
		}

		return null;
	}

	private static async Task<bool> HasRequiredRuleAsync(string dotnetPath, CancellationToken ct)
	{
		try
		{
			using var proc = new Process
			{
				StartInfo = new ProcessStartInfo("netsh",
					"advfirewall firewall show rule name=all dir=in verbose")
				{
					UseShellExecute = false,
					RedirectStandardOutput = true,
					CreateNoWindow = true,
				}
			};

			proc.Start();
			var output = await proc.StandardOutput.ReadToEndAsync(ct);
			await proc.WaitForExitAsync(ct);

			return ParseHasRequiredRule(output, dotnetPath);
		}
		catch (Exception)
		{
			// If netsh itself fails, assume no rule exists so we attempt to add one.
			return false;
		}
	}

	/// <summary>
	/// Parses <c>netsh ... show rule --verbose</c> output and returns <c>true</c> when
	/// at least one enabled inbound Allow rule references <paramref name="dotnetPath"/>
	/// and covers the Private or Domain profile (or Any, which covers all profiles).
	///
	/// netsh emits rules as blank-line-separated blocks, e.g.:
	/// <code>
	/// Rule Name:                            Some Rule
	/// Program:                              C:\Program Files\dotnet\dotnet.exe
	/// Profiles:                             Public
	/// Action:                               Allow
	/// Enabled:                              Yes
	/// LocalIP:                              Any
	/// RemoteIP:                             Any
	/// </code>
	/// Fields like "LocalIP: Any" / "RemoteIP: Any" must not be mistaken for a
	/// profile of "Any" — this method parses the <c>Profiles:</c> field specifically.
	/// </summary>
	internal static bool ParseHasRequiredRule(string netshOutput, string dotnetPath)
	{
		// Normalise path separators so the comparison is robust.
		var normalPath = dotnetPath.Replace('/', '\\').TrimEnd('\\');

		// netsh --verbose emits rules as blocks separated by blank lines.
		var blocks = netshOutput.Split("\r\n\r\n", StringSplitOptions.RemoveEmptyEntries);
		foreach (var block in blocks)
		{
			if (!block.Contains(normalPath, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			// Parse individual field lines rather than searching the whole block, so
			// that values like "LocalIP: Any" / "RemoteIP: Any" don't produce false
			// positives when checking whether the Profiles field contains "Any".
			var profiles = GetFieldValue(block, "Profiles");
			var action = GetFieldValue(block, "Action");
			var enabled = GetFieldValue(block, "Enabled");

			// Profile must cover Private, Domain, or Any (= all three profiles).
			var coversRequired =
				profiles.Contains("Private", StringComparison.OrdinalIgnoreCase) ||
				profiles.Contains("Domain", StringComparison.OrdinalIgnoreCase) ||
				profiles.Equals("Any", StringComparison.OrdinalIgnoreCase);

			var isAllow = action.Equals("Allow", StringComparison.OrdinalIgnoreCase);

			// "Enabled: No" means the rule exists but is disabled — treat as absent.
			var isEnabled = !enabled.Equals("No", StringComparison.OrdinalIgnoreCase);

			if (coversRequired && isAllow && isEnabled)
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Extracts the value of a named field from a netsh rule block.
	/// Lines have the form "FieldName:&lt;whitespace&gt;Value".
	/// Returns an empty string when the field is not present.
	/// </summary>
	private static string GetFieldValue(string block, string fieldName)
	{
		var prefix = fieldName + ":";
		foreach (var line in block.Split('\n'))
		{
			var trimmed = line.TrimStart();
			if (trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
			{
				return trimmed[prefix.Length..].Trim();
			}
		}

		return string.Empty;
	}

	private static async Task AddFirewallRuleAsync(string dotnetPath, ILogger logger, CancellationToken ct)
	{
		// netsh advfirewall requires admin rights → launch elevated via runas.
		var arguments =
			$"""advfirewall firewall add rule name="{RuleDisplayName}" dir=in action=allow program="{dotnetPath}" enable=yes profile={RuleProfiles}""";

		try
		{
			using var proc = new Process
			{
				StartInfo = new ProcessStartInfo("netsh", arguments)
				{
					UseShellExecute = true,
					Verb = "runas",   // triggers UAC prompt
					CreateNoWindow = true,
				}
			};

			proc.Start();
			await proc.WaitForExitAsync(ct);

			if (proc.ExitCode == 0)
			{
				logger.LogInformation(
					"WindowsFirewall: Private+Domain inbound rule added for dotnet.exe.");
			}
			else
			{
				LogManualInstructions(dotnetPath, logger,
					$"netsh exited with code {proc.ExitCode}.");
			}
		}
		catch (Exception ex) when (!ct.IsCancellationRequested)
		{
			// UAC declined → Win32Exception; dialog dismissed → OperationCanceledException.
			// Either way, log and continue — the DevServer still starts.
			LogManualInstructions(dotnetPath, logger, ex.Message);
		}
	}

	private static void LogManualInstructions(string dotnetPath, ILogger logger, string reason)
	{
		logger.LogWarning(
			"""
			WindowsFirewall: could not add the Private/Domain inbound rule ({Reason}).
			Physical Android/iOS devices and remote clients on Private or Domain
			networks may not be able to connect to the DevServer.

			To add the rule manually (run PowerShell as Administrator):

			  New-NetFirewallRule `
			    -DisplayName "{RuleName}" `
			    -Direction Inbound -Action Allow `
			    -Program "{DotnetPath}" `
			    -Profile @("Private", "Domain")
			""",
			reason,
			RuleDisplayName,
			dotnetPath);
	}
}
