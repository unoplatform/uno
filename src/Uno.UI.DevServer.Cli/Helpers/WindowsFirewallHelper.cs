using System.Diagnostics;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;

namespace Uno.UI.DevServer.Cli.Helpers;

/// <summary>
/// Ensures the Windows Firewall has an inbound Allow rule for <c>dotnet.exe</c>
/// on the <em>Private</em> network profile.
///
/// Why this is needed: the .NET SDK installer creates a rule for <c>dotnet.exe</c>
/// that covers the <em>Public</em> profile only.  Wi-Fi networks are classified as
/// <em>Private</em> in Windows, so physical Android/iOS devices on the same LAN
/// are silently blocked.  <c>CreateNoWindow = true</c> on the host process prevents
/// the Windows Firewall dialog from ever appearing, so no rule is ever created
/// automatically after the first SDK install.
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
	/// Checks whether a Private-profile inbound Allow rule already exists for the
	/// current <c>dotnet.exe</c>, and adds one via an elevated <c>netsh</c> call if
	/// not.  Gracefully degrades (warning + manual instructions) if UAC is declined
	/// or if the <c>netsh</c> invocation fails.
	/// </summary>
	public static async Task EnsurePrivateRuleAsync(ILogger logger, CancellationToken ct)
	{
		var dotnetPath = ResolveDotnetPath();
		if (dotnetPath is null)
		{
			logger.LogDebug("WindowsFirewall: could not resolve dotnet.exe path — skipping firewall check.");
			return;
		}

		if (await HasPrivateRuleAsync(dotnetPath, ct))
		{
			logger.LogDebug("WindowsFirewall: Private inbound rule already exists for {Path}.", dotnetPath);
			return;
		}

		logger.LogInformation(
			"WindowsFirewall: no Private-profile inbound rule found for dotnet.exe. " +
			"A UAC prompt will appear to add one — this happens once per machine.");

		await AddPrivateRuleAsync(dotnetPath, logger, ct);
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

	/// <summary>
	/// Returns <c>true</c> if <c>netsh</c> reports at least one inbound Allow rule
	/// that references <paramref name="dotnetPath"/> and covers Private, Domain, or Any.
	/// Public is already covered by the .NET SDK installer rule, so it is not checked.
	/// </summary>
	private static async Task<bool> HasPrivateRuleAsync(string dotnetPath, CancellationToken ct)
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
	/// Parses <c>netsh ... show rule</c> output and returns <c>true</c> when a rule
	/// block contains both the <paramref name="dotnetPath"/> and "Private" in its
	/// profile line.
	///
	/// netsh emits rules as blank-line-separated blocks, e.g.:
	/// <code>
	/// Rule Name:                            Some Rule
	/// ...
	/// Program:                              C:\Program Files\dotnet\dotnet.exe
	/// ...
	/// Profiles:                             Public
	/// Action:                               Allow
	/// </code>
	/// </summary>
	internal static bool ParseHasRequiredRule(string netshOutput, string dotnetPath)
	{
		// Normalise path separators so the comparison is robust.
		var normalPath = dotnetPath.Replace('/', '\\').TrimEnd('\\');

		var blocks = netshOutput.Split("\r\n\r\n", StringSplitOptions.RemoveEmptyEntries);
		foreach (var block in blocks)
		{
			var hasPath = block.Contains(normalPath, StringComparison.OrdinalIgnoreCase);
			if (!hasPath)
			{
				continue;
			}

			// A rule may cover multiple profiles separated by commas,
			// e.g. "Profiles: Private,Public" or "Profiles: Any".
			// The rule is considered sufficient if it covers Private, Domain, or Any
		// (Any = all three profiles).  Public is already covered by the SDK rule.
		var hasPrivate = block.Contains("Private", StringComparison.OrdinalIgnoreCase)
				|| block.Contains("Domain", StringComparison.OrdinalIgnoreCase)
				|| block.Contains("Any", StringComparison.OrdinalIgnoreCase);

			var isAllow = block.Contains("Allow", StringComparison.OrdinalIgnoreCase);

			if (hasPrivate && isAllow)
			{
				return true;
			}
		}

		return false;
	}

	private static async Task AddPrivateRuleAsync(string dotnetPath, ILogger logger, CancellationToken ct)
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
					"WindowsFirewall: Private-profile inbound rule added for dotnet.exe.");
			}
			else
			{
				LogManualInstructions(dotnetPath, logger,
					$"netsh exited with code {proc.ExitCode}.");
			}
		}
		catch (Exception ex) when (!ct.IsCancellationRequested)
		{
			// UAC declined → OperationCanceledException, or the user dismissed the dialog
			// → Win32Exception.  Either way, log and continue — the DevServer still starts.
			LogManualInstructions(dotnetPath, logger, ex.Message);
		}
	}

	private static void LogManualInstructions(string dotnetPath, ILogger logger, string reason)
	{
		logger.LogWarning(
			"""
			WindowsFirewall: could not add the Private/Domain inbound rule ({Reason}).
			Physical Android/iOS devices and remote clients on Private or Domain
			networks may not be able to connect to the DevServer for Hot Reload.

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
