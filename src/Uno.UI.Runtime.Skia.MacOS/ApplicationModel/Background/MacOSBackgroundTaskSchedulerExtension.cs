#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.UI.Runtime.Skia;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Background;
using Windows.Storage;

namespace Uno.UI.Runtime.Skia.MacOS;

internal sealed partial class MacOSBackgroundTaskSchedulerExtension :
	IBackgroundTaskSchedulerExtension
{
	private const string LaunchctlPath = "/bin/launchctl";
	private static readonly MacOSBackgroundTaskSchedulerExtension Instance = new();
	private static readonly IReadOnlyDictionary<string, string?> LaunchctlEnvironment =
		new Dictionary<string, string?>
		{
			// launchctl diagnostics are matched by text below, so pin the message locale.
			["LC_ALL"] = "C",
			["LANGUAGE"] = "C",
			["LANG"] = "C"
		};

	public bool IsSupported =>
		OperatingSystem.IsMacOS()
		&& File.Exists(LaunchctlPath)
		&& string.IsNullOrEmpty(
			Environment.GetEnvironmentVariable("APP_SANDBOX_CONTAINER_ID"));

	internal static void RegisterExtension()
		=> ApiExtensibility.Register(
			typeof(IBackgroundTaskSchedulerExtension),
			_ => Instance);

	public void Reconcile()
	{
		var directory = GetCleanupDirectory();
		if (!Directory.Exists(directory))
		{
			return;
		}

		foreach (var markerPath in Directory.EnumerateFiles(
			directory,
			GetLabelPrefix() + "*.cleanup"))
		{
			var label = Path.GetFileNameWithoutExtension(markerPath);
			var result = RunLaunchctl("bootout", $"{GetDomain()}/{label}");
			if (result.ExitCode == 0 || IsMissingJob(result))
			{
				var enable = RunLaunchctl("enable", $"{GetDomain()}/{label}");
				if (enable.ExitCode == 0)
				{
					DeleteIfExists(markerPath);
				}
				else if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().Warn(
						enable.GetError(
							"Clearing a completed launchd background task override"));
				}
			}
			else if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn(
					result.GetError("Reconciling a completed launchd background task"));
			}
		}
	}

	public void Register(BackgroundTaskRegistrationRecord registration)
	{
		if (!IsSupported)
		{
			throw new PlatformNotSupportedException(
				"launchd background tasks require a non-sandboxed macOS application.");
		}

		var path = GetLaunchAgentPath(registration);
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		WriteAtomic(path, BuildPropertyList(registration));
		try
		{
			var result = RunLaunchctl("bootstrap", GetDomain(), path);
			if (result.ExitCode != 0)
			{
				throw new InvalidOperationException(
					result.GetError("Registering the launchd background task"));
			}
		}
		catch (Exception error) when (
			error is IOException
				or UnauthorizedAccessException
				or InvalidOperationException)
		{
			try
			{
				var rollback = RunLaunchctl(
					"bootout",
					$"{GetDomain()}/{GetLabel(registration)}");
				if (rollback.ExitCode != 0 && !IsMissingJob(rollback))
				{
					throw new InvalidOperationException(
						rollback.GetError("Rolling back the launchd background task"));
				}
				DeleteIfExists(path);
			}
			catch (Exception rollbackError) when (
				rollbackError is IOException
					or UnauthorizedAccessException
					or InvalidOperationException)
			{
				throw new AggregateException(
					"The launchd background task could not be registered or rolled back.",
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
		// launchd has no unregister-without-cancel operation. Bootout removes the
		// schedule and terminates an active job regardless of cancelTask.
		_ = cancelTask;
		var result = RunLaunchctl(
			"bootout",
			$"{GetDomain()}/{GetLabel(registration)}");
		if (result.ExitCode != 0 && !IsMissingJob(result))
		{
			throw new InvalidOperationException(
				result.GetError("Removing the launchd background task"));
		}

		DeleteIfExists(GetLaunchAgentPath(registration));
		DeleteIfExists(GetCleanupMarkerPath(registration));
	}

	public void CompleteOneShot(BackgroundTaskRegistrationRecord registration)
	{
		var target = $"{GetDomain()}/{GetLabel(registration)}";
		var disable = RunLaunchctl("disable", target);
		if (disable.ExitCode != 0)
		{
			throw new InvalidOperationException(
				disable.GetError("Disabling the completed launchd background task"));
		}

		try
		{
			var markerPath = GetCleanupMarkerPath(registration);
			Directory.CreateDirectory(Path.GetDirectoryName(markerPath)!);
			WriteAtomic(markerPath, string.Empty);
			DeleteIfExists(GetLaunchAgentPath(registration));
		}
		catch (Exception error) when (
			error is IOException or UnauthorizedAccessException)
		{
			var enable = RunLaunchctl("enable", target);
			DeleteIfExists(GetCleanupMarkerPath(registration));
			if (enable.ExitCode != 0)
			{
				throw new AggregateException(
					"The completed launchd task could not be staged for cleanup or re-enabled.",
					error,
					new InvalidOperationException(
						enable.GetError("Re-enabling the launchd background task")));
			}

			throw;
		}
	}

	internal static string BuildPropertyList(
		BackgroundTaskRegistrationRecord registration)
	{
		var output = new StringBuilder();
		using var stringWriter = new Utf8StringWriter(output);
		using var writer = XmlWriter.Create(
			stringWriter,
			new XmlWriterSettings
			{
				Indent = true,
				OmitXmlDeclaration = false
			});

		writer.WriteStartDocument();
		writer.WriteDocType(
			"plist",
			"-//Apple//DTD PLIST 1.0//EN",
			"http://www.apple.com/DTDs/PropertyList-1.0.dtd",
			subset: null);
		writer.WriteStartElement("plist");
		writer.WriteAttributeString("version", "1.0");
		writer.WriteStartElement("dict");

		WriteString(writer, "Label", GetLabel(registration));
		writer.WriteElementString("key", "ProgramArguments");
		writer.WriteStartElement("array");
		writer.WriteElementString("string", registration.ExecutablePath);
		foreach (var argument in registration.ExecutableArguments)
		{
			writer.WriteElementString("string", argument);
		}
		writer.WriteEndElement();
		WriteString(writer, "WorkingDirectory", registration.WorkingDirectory);
		WriteInteger(
			writer,
			"StartInterval",
			checked((long)registration.Trigger.FreshnessTime * 60));
		if (registration.Trigger.OneShot)
		{
			writer.WriteElementString("key", "LaunchOnlyOnce");
			writer.WriteStartElement("true");
			writer.WriteEndElement();
		}
		WriteString(writer, "ProcessType", "Background");
		WriteInteger(writer, "ExitTimeOut", 30);
		writer.WriteElementString("key", "RunAtLoad");
		writer.WriteStartElement("false");
		writer.WriteEndElement();

		writer.WriteEndElement();
		writer.WriteEndElement();
		writer.WriteEndDocument();
		return output.ToString();
	}

	private static void WriteString(XmlWriter writer, string key, string value)
	{
		writer.WriteElementString("key", key);
		writer.WriteElementString("string", value);
	}

	private static void WriteInteger(XmlWriter writer, string key, long value)
	{
		writer.WriteElementString("key", key);
		writer.WriteElementString(
			"integer",
			value.ToString(CultureInfo.InvariantCulture));
	}

	private static string GetLaunchAgentPath(
		BackgroundTaskRegistrationRecord registration)
	{
		var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		if (string.IsNullOrWhiteSpace(home))
		{
			throw new InvalidOperationException(
				"The current user's home directory could not be determined.");
		}

		return Path.Combine(
			home,
			"Library",
			"LaunchAgents",
			GetLabel(registration) + ".plist");
	}

	private static string GetLabel(BackgroundTaskRegistrationRecord registration)
		=> GetLabelPrefix() + registration.TaskId.ToString("N");

	private static string GetLabelPrefix()
		=> $"org.unoplatform.background.{GetScopeHash()}.";

	private static string GetScopeHash()
	{
		var appName = Package.Current.Id.Name;
		if (string.IsNullOrWhiteSpace(appName))
		{
			appName = "uno-app";
		}

		var hash = SHA256.HashData(Encoding.UTF8.GetBytes(appName));
		return Convert.ToHexString(hash.AsSpan(0, 8)).ToLowerInvariant();
	}

	private static string GetDomain()
		=> $"gui/{NativeMethods.getuid()}";

	private static string GetCleanupDirectory()
		=> Path.Combine(
			ApplicationData.Current.LocalFolder.Path,
			"BackgroundTasks",
			"LaunchdCleanup");

	private static string GetCleanupMarkerPath(
		BackgroundTaskRegistrationRecord registration)
		=> Path.Combine(
			GetCleanupDirectory(),
			GetLabel(registration) + ".cleanup");

	private static NativeProcessResult RunLaunchctl(params string[] arguments)
		=> NativeProcessRunner.Run(LaunchctlPath, arguments, LaunchctlEnvironment);

	private static bool IsMissingJob(NativeProcessResult result)
		=> result.StandardError.Contains(
			"Could not find service",
			StringComparison.OrdinalIgnoreCase)
			|| result.StandardError.Contains(
				"No such process",
				StringComparison.OrdinalIgnoreCase)
			|| result.StandardError.Contains(
				"service not found",
				StringComparison.OrdinalIgnoreCase);

	private static void WriteAtomic(string path, string contents)
	{
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

	private sealed class Utf8StringWriter(StringBuilder builder) :
		StringWriter(builder, CultureInfo.InvariantCulture)
	{
		public override Encoding Encoding => Encoding.UTF8;
	}

	private static partial class NativeMethods
	{
		[LibraryImport("libSystem.B.dylib")]
		internal static partial uint getuid();
	}
}
