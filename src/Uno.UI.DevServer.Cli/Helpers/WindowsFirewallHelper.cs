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
	private const string RuleProfiles = "private,domain";

	// Set UNO_DEVSERVER_SKIP_FIREWALL_CHECK=1 to disable the check entirely.
	// Useful for CI runners, GPO-managed machines, or any environment where
	// elevation is not permitted or desired.
	internal static bool IsOptedOut =>
		string.Equals(
			Environment.GetEnvironmentVariable("UNO_DEVSERVER_SKIP_FIREWALL_CHECK"),
			"1", StringComparison.Ordinal) ||
		string.Equals(
			Environment.GetEnvironmentVariable("UNO_DEVSERVER_SKIP_FIREWALL_CHECK"),
			"true", StringComparison.OrdinalIgnoreCase);

	/// <summary>
	/// Checks whether the DevServer inbound Allow rule already exists, and adds one
	/// via an elevated <c>netsh</c> call if not.  Gracefully degrades (warning +
	/// manual instructions) if UAC is declined or the <c>netsh</c> call fails.
	/// Set <c>UNO_DEVSERVER_SKIP_FIREWALL_CHECK=1</c> to bypass entirely.
	/// </summary>
	public static async Task EnsureFirewallRuleAsync(ILogger logger, CancellationToken ct)
	{
		if (IsOptedOut)
		{
			logger.LogDebug("WindowsFirewall: check skipped (UNO_DEVSERVER_SKIP_FIREWALL_CHECK is set).");
			return;
		}

		var dotnetPath = ResolveDotnetPath();
		if (dotnetPath is null)
		{
			logger.LogDebug("WindowsFirewall: could not resolve dotnet.exe path — skipping firewall check.");
			return;
		}

		if (await RuleExistsAsync(ct))
		{
			logger.LogDebug("WindowsFirewall: inbound rule '{RuleName}' already exists.", RuleDisplayName);
			return;
		}

		logger.LogInformation(
			"WindowsFirewall: inbound rule '{RuleName}' not found. " +
			"A UAC prompt will appear to add one — this happens once per machine.",
			RuleDisplayName);

		await AddFirewallRuleAsync(dotnetPath, logger, ct);
	}

	// -------------------------------------------------------------------------
	// Private helpers
	// -------------------------------------------------------------------------

	/// <summary>
	/// Checks whether the Uno DevServer named rule exists by exit code.
	/// No output parsing — avoids any dependency on localized netsh field labels.
	/// </summary>
	private static async Task<bool> RuleExistsAsync(CancellationToken ct)
	{
		try
		{
			using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
			cts.CancelAfter(TimeSpan.FromSeconds(10));

			using var proc = new Process
			{
				StartInfo = new ProcessStartInfo(
					"netsh",
					$"""advfirewall firewall show rule name="{RuleDisplayName}" dir=in""")
				{
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					CreateNoWindow = true,
				}
			};

			proc.Start();

			// Drain both streams to avoid deadlock; we only care about exit code.
			var drainOut = proc.StandardOutput.ReadToEndAsync(cts.Token);
			var drainErr = proc.StandardError.ReadToEndAsync(cts.Token);
			await Task.WhenAll(drainOut, drainErr);
			await proc.WaitForExitAsync(cts.Token);

			// Exit 0 = rule found, non-0 = not found (language-agnostic).
			return proc.ExitCode == 0;
		}
		catch (OperationCanceledException) when (ct.IsCancellationRequested)
		{
			throw;
		}
		catch (Exception)
		{
			// netsh unavailable or timed out — assume no rule, attempt to add.
			return false;
		}
	}

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

	private static async Task AddFirewallRuleAsync(string dotnetPath, ILogger logger, CancellationToken ct)
	{
		// netsh advfirewall requires admin rights — launch elevated via runas.
		var arguments =
			$"""advfirewall firewall add rule name="{RuleDisplayName}" dir=in action=allow program="{dotnetPath}" enable=yes profile={RuleProfiles}""";

		try
		{
			// Allow up to 60 s for UAC interaction + netsh execution.
			using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
			cts.CancelAfter(TimeSpan.FromSeconds(60));

			using var proc = new Process
			{
				StartInfo = new ProcessStartInfo("netsh", arguments)
				{
					UseShellExecute = true,
					Verb = "runas",   // triggers UAC prompt
				}
			};

			proc.Start();
			await proc.WaitForExitAsync(cts.Token);

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
		catch (OperationCanceledException) when (ct.IsCancellationRequested)
		{
			throw;
		}
		catch (Exception ex)
		{
			// UAC declined → Win32Exception; local timeout → OperationCanceledException
			// on the inner cts (not ct).  Either way, log and continue.
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
