using System.Diagnostics;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;

namespace Uno.UI.DevServer.Cli.Helpers;

/// <summary>
/// Ensures the Windows Firewall has an inbound Allow rule for
/// <c>Uno.UI.RemoteControl.Host.exe</c> on the <em>Private</em> and
/// <em>Domain</em> network profiles.
///
/// Why this is needed: the DevServer CLI spawns the host with
/// <c>CreateNoWindow = true</c>, which suppresses the Windows Firewall
/// interactive dialog that would normally prompt the user to allow inbound
/// connections for a new executable.  Without that dialog, no Private or Domain
/// rule is ever created, so physical Android/iOS devices and remote clients on
/// those networks are silently blocked.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class WindowsFirewallHelper
{
	private const string RuleDisplayName = "Uno DevServer (.NET Host)";
	private const string RuleProfiles = "private,domain";

	// Set UNO_DEVSERVER_SKIP_FIREWALL_CHECK=1 to disable the check entirely.
	// Useful for CI runners, GPO-managed machines, or any environment where
	// elevation is not permitted or desired.
	internal static bool IsOptedOut
	{
		get
		{
			var value = Environment.GetEnvironmentVariable("UNO_DEVSERVER_SKIP_FIREWALL_CHECK");
			return string.Equals(value, "1", StringComparison.Ordinal)
				|| string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
		}
	}

	/// <summary>
	/// Checks whether the DevServer inbound Allow rule already exists for
	/// <paramref name="hostExePath"/>, and adds one via an elevated <c>netsh</c>
	/// call if not.  Gracefully degrades (warning + manual instructions) if UAC
	/// is declined or the <c>netsh</c> call fails.
	/// Set <c>UNO_DEVSERVER_SKIP_FIREWALL_CHECK=1</c> to bypass entirely.
	/// </summary>
	/// <param name="hostExePath">
	/// Absolute path to <c>Uno.UI.RemoteControl.Host.exe</c> — the executable
	/// that opens the inbound DevServer port.
	/// </param>
	public static async Task EnsureFirewallRuleAsync(string hostExePath, ILogger logger, CancellationToken ct)
	{
		if (IsOptedOut)
		{
			logger.LogDebug("WindowsFirewall: check skipped (UNO_DEVSERVER_SKIP_FIREWALL_CHECK is set).");
			return;
		}

		// Validate before interpolating into an elevated netsh command.
		// A path with a quote character could break argument delimitation and inject
		// arbitrary netsh sub-commands executed as Administrator via runas.
		if (!Path.IsPathFullyQualified(hostExePath)
			|| hostExePath.Contains('"')
			|| !File.Exists(hostExePath))
		{
			logger.LogWarning(
				"WindowsFirewall: hostExePath '{Path}' is not a valid fully-qualified existing path; skipping firewall check.",
				hostExePath);
			return;
		}

		if (await RuleExistsAsync(logger, ct))
		{
			logger.LogDebug("WindowsFirewall: inbound rule '{RuleName}' already exists — no action needed.", RuleDisplayName);
			return;
		}

		logger.LogInformation(
			"WindowsFirewall: inbound rule '{RuleName}' not found. " +
			"A UAC prompt will appear to add one — this happens once per machine.",
			RuleDisplayName);

		await AddFirewallRuleAsync(hostExePath, logger, ct);
	}

	// -------------------------------------------------------------------------
	// Private helpers
	// -------------------------------------------------------------------------

	/// <summary>
	/// Checks whether the Uno DevServer named rule exists by exit code.
	/// No output parsing — avoids any dependency on localized netsh field labels.
	/// </summary>
	private static async Task<bool> RuleExistsAsync(ILogger logger, CancellationToken ct)
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
		catch (Exception ex)
		{
			// netsh unavailable or timed out — assume no rule, attempt to add.
			logger.LogDebug("WindowsFirewall: could not check for existing rule ({Reason}); will attempt to add.", ex.Message);
			return false;
		}
	}

	private static async Task AddFirewallRuleAsync(string hostExePath, ILogger logger, CancellationToken ct)
	{
		// netsh advfirewall requires admin rights — launch elevated via runas.
		var arguments =
			$"""advfirewall firewall add rule name="{RuleDisplayName}" dir=in action=allow program="{hostExePath}" enable=yes profile={RuleProfiles}""";

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
					"WindowsFirewall: Private+Domain inbound rule added for '{HostExe}'.",
					hostExePath);
			}
			else
			{
				LogManualInstructions(hostExePath, logger,
					$"netsh exited with code {proc.ExitCode}.");
			}
		}
		catch (OperationCanceledException) when (ct.IsCancellationRequested)
		{
			throw;
		}
		catch (OperationCanceledException)
		{
			// Inner 60 s timeout elapsed — UAC was not interacted with in time.
			LogManualInstructions(hostExePath, logger, "firewall check timed out after 60 s");
		}
		catch (Exception ex)
		{
			// UAC declined → Win32Exception; other netsh failures.
			LogManualInstructions(hostExePath, logger, ex.Message);
		}
	}

	private static void LogManualInstructions(string hostExePath, ILogger logger, string reason)
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
			    -Program "{HostExePath}" `
			    -Profile @("Private", "Domain")
			""",
			reason,
			RuleDisplayName,
			hostExePath);
	}
}
