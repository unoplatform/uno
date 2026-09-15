#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Uno.Foundation.Extensibility;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Background;

namespace Uno.UI.Runtime.Skia;

internal sealed class LinuxBackgroundTaskSchedulerExtension :
	IBackgroundTaskSchedulerExtension
{
	private static readonly LinuxBackgroundTaskSchedulerExtension Instance = new();
	private static readonly IReadOnlyDictionary<string, string?> SystemdEnvironment =
		new Dictionary<string, string?>
		{
			["SYSTEMD_COLORS"] = "0",
			["SYSTEMD_PAGER"] = string.Empty,
			// systemctl diagnostics are matched by text below, so pin the message locale.
			["LC_ALL"] = "C",
			["LANGUAGE"] = "C",
			["LANG"] = "C"
		};
	private static readonly string[] SystemctlPrefix = ["--user", "--no-pager"];
	private readonly Lazy<bool> _isSupported = new(CheckIsSupported);

	private static string SystemctlPath =>
		File.Exists("/usr/bin/systemctl")
			? "/usr/bin/systemctl"
			: File.Exists("/bin/systemctl")
				? "/bin/systemctl"
				: "systemctl";

	public bool IsSupported => _isSupported.Value;

	private static bool CheckIsSupported()
	{
		if (!OperatingSystem.IsLinux())
		{
			return false;
		}

		try
		{
			return RunSystemctl(["show-environment"]).ExitCode == 0;
		}
		catch (InvalidOperationException)
		{
			return false;
		}
	}

	internal static void RegisterExtension()
		=> ApiExtensibility.Register(
			typeof(IBackgroundTaskSchedulerExtension),
			_ => Instance);

	public void Reconcile()
	{
	}

	public void Register(BackgroundTaskRegistrationRecord registration)
	{
		if (!IsSupported)
		{
			throw new PlatformNotSupportedException(
				"A running systemd user manager is required for background tasks.");
		}

		var servicePath = GetServicePath(registration);
		var timerPath = GetTimerPath(registration);
		Directory.CreateDirectory(Path.GetDirectoryName(servicePath)!);

		WriteAtomic(servicePath, BuildService(registration));
		try
		{
			WriteAtomic(timerPath, BuildTimer(registration));
			RunSystemctlRequired(
				["daemon-reload"],
				"Reloading the systemd user manager");
			RunSystemctlRequired(
				["enable", "--now", GetTimerName(registration)],
				"Enabling the systemd background task");
		}
		catch (Exception error) when (
			error is IOException
				or UnauthorizedAccessException
				or InvalidOperationException)
		{
			try
			{
				var disable = RunSystemctl(
					["disable", "--now", GetTimerName(registration)]);
				if (disable.ExitCode != 0 && !IsMissingUnit(disable))
				{
					throw new InvalidOperationException(
						disable.GetError(
							"Rolling back the systemd background task"));
				}

				DeleteIfExists(timerPath);
				DeleteIfExists(servicePath);
				RunSystemctlRequired(
					["daemon-reload"],
					"Reloading systemd after background task rollback");
			}
			catch (Exception rollbackError) when (
				rollbackError is IOException
					or UnauthorizedAccessException
					or InvalidOperationException)
			{
				throw new AggregateException(
					"The systemd background task could not be registered or rolled back.",
					error,
					rollbackError);
			}

			throw;
		}
	}

	public void Unregister(
		BackgroundTaskRegistrationRecord registration,
		bool cancelTask)
	{
		var timerName = GetTimerName(registration);
		var disable = RunSystemctl(["disable", "--now", timerName]);
		if (disable.ExitCode != 0 && !IsMissingUnit(disable))
		{
			throw new InvalidOperationException(
				disable.GetError("Disabling the systemd background task"));
		}

		if (cancelTask)
		{
			var stop = RunSystemctl(["stop", GetServiceName(registration)]);
			if (stop.ExitCode != 0 && !IsMissingUnit(stop))
			{
				throw new InvalidOperationException(
					stop.GetError("Stopping the systemd background task"));
			}
		}

		DeleteIfExists(GetTimerPath(registration));
		DeleteIfExists(GetServicePath(registration));
		RunSystemctlRequired(
			["daemon-reload"],
			"Reloading the systemd user manager");
	}

	public void CompleteOneShot(BackgroundTaskRegistrationRecord registration)
		=> Unregister(registration, cancelTask: false);

	internal static string BuildService(BackgroundTaskRegistrationRecord registration)
	{
		var command = string.Join(
			" ",
			new[] { registration.ExecutablePath }
				.Concat(registration.ExecutableArguments)
				.Select(EscapeSystemdArgument));

		return $"""
[Unit]
Description=Uno background task {registration.TaskId:D}

[Service]
Type=oneshot
ExecStart={command}
WorkingDirectory={EscapeSystemdArgument(registration.WorkingDirectory)}
KillMode=control-group
TimeoutStopSec=30s
""";
	}

	internal static string BuildTimer(BackgroundTaskRegistrationRecord registration)
	{
		var repeat = registration.Trigger.OneShot
			? string.Empty
			: $"OnUnitActiveSec={registration.Trigger.FreshnessTime}min{Environment.NewLine}";
		return $"""
[Unit]
Description=Uno background task timer {registration.TaskId:D}

[Timer]
OnActiveSec={registration.Trigger.FreshnessTime}min
{repeat}AccuracySec=1min
Unit={GetServiceName(registration)}

[Install]
WantedBy=timers.target
""";
	}

	internal static string EscapeSystemdArgument(string value)
	{
		var builder = new StringBuilder(value.Length + 2);
		builder.Append('"');
		foreach (var character in value)
		{
			switch (character)
			{
				case '\\':
					builder.Append(@"\\");
					break;
				case '"':
					builder.Append("\\\"");
					break;
				case '$':
					builder.Append("$$");
					break;
				case '%':
					builder.Append("%%");
					break;
				case '\n':
					builder.Append(@"\n");
					break;
				case '\r':
					builder.Append(@"\r");
					break;
				case '\t':
					builder.Append(@"\t");
					break;
				default:
					builder.Append(character);
					break;
			}
		}
		builder.Append('"');
		return builder.ToString();
	}

	private static string GetServicePath(BackgroundTaskRegistrationRecord registration)
		=> Path.Combine(GetUnitDirectory(), GetServiceName(registration));

	private static string GetTimerPath(BackgroundTaskRegistrationRecord registration)
		=> Path.Combine(GetUnitDirectory(), GetTimerName(registration));

	private static string GetServiceName(BackgroundTaskRegistrationRecord registration)
		=> GetUnitPrefix() + "-" + registration.TaskId.ToString("N") + ".service";

	private static string GetTimerName(BackgroundTaskRegistrationRecord registration)
		=> GetUnitPrefix() + "-" + registration.TaskId.ToString("N") + ".timer";

	private static string GetUnitPrefix()
	{
		var appName = Package.Current.Id.Name;
		if (string.IsNullOrWhiteSpace(appName))
		{
			appName = "uno-app";
		}

		var hash = SHA256.HashData(Encoding.UTF8.GetBytes(appName));
		return "uno-background-" + Convert.ToHexString(hash.AsSpan(0, 8)).ToLowerInvariant();
	}

	private static string GetUnitDirectory()
	{
		var configHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
		if (string.IsNullOrWhiteSpace(configHome))
		{
			var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			if (string.IsNullOrWhiteSpace(home))
			{
				throw new InvalidOperationException(
					"The current user's home directory could not be determined.");
			}

			configHome = Path.Combine(home, ".config");
		}

		return Path.Combine(configHome, "systemd", "user");
	}

	private static NativeProcessResult RunSystemctl(IEnumerable<string> arguments)
		=> NativeProcessRunner.Run(
			SystemctlPath,
			SystemctlPrefix.Concat(arguments),
			SystemdEnvironment);

	private static void RunSystemctlRequired(
		IEnumerable<string> arguments,
		string operation)
	{
		var result = RunSystemctl(arguments);
		if (result.ExitCode != 0)
		{
			throw new InvalidOperationException(result.GetError(operation));
		}
	}

	private static bool IsMissingUnit(NativeProcessResult result)
		=> result.StandardError.Contains("not loaded", StringComparison.OrdinalIgnoreCase)
			|| result.StandardError.Contains("does not exist", StringComparison.OrdinalIgnoreCase)
			|| result.StandardError.Contains("not found", StringComparison.OrdinalIgnoreCase);

	private static void WriteAtomic(string path, string contents)
	{
		// systemd unit files are line-based; keep LF regardless of the checkout that compiled
		// the raw string literals above.
		contents = contents.ReplaceLineEndings("\n");
		var temporaryPath = path + ".tmp";
		try
		{
			using (var stream = new FileStream(
				temporaryPath,
				FileMode.Create,
				FileAccess.Write,
				FileShare.None,
				4096,
				FileOptions.WriteThrough))
			using (var writer = new StreamWriter(
				stream,
				new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
				bufferSize: 1024,
				leaveOpen: true))
			{
				writer.Write(contents);
				writer.Flush();
				stream.Flush(flushToDisk: true);
			}

			File.Move(temporaryPath, path, overwrite: true);
		}
		finally
		{
			DeleteIfExists(temporaryPath);
		}
	}

	private static void DeleteIfExists(string path)
	{
		if (File.Exists(path))
		{
			File.Delete(path);
		}
	}
}
