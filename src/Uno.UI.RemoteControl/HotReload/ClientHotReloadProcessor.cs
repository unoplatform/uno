#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Uno.Foundation.Logging;
using Uno.UI.RemoteControl.HotReload.Messages;
using Uno.UI.Tasks.HotReloadInfo;

namespace Uno.UI.RemoteControl.HotReload;

public partial class ClientHotReloadProcessor : IClientProcessor, IDisposable
{
	private string? _projectPath;
	private readonly IRemoteControlClient _rcClient;
	private HotReloadMode? _forcedHotReloadMode;

	private Dictionary<string, string>? _msbuildProperties;

	public ClientHotReloadProcessor(IRemoteControlClient rcClient)
	{
		_rcClient = rcClient;
		_status = new(this);
	}

	partial void InitializeMetadataUpdater();

	public void Dispose()
	{
		_agent?.Dispose();
		_agent = null;

		// On runtimes where AssemblyLoadContext.Unloading is not raised for collectible contexts
		// (browser-wasm, dotnet/runtime#34072) an explicit host-driven Dispose is the load-bearing
		// release path, so when this processor copy is owned by a collectible context also release
		// the per-context statics — most importantly the shared ElementUpdateAgent, whose
		// process-wide AppDomain.AssemblyLoad subscription would otherwise pin the context after
		// unload. The default/non-collectible case is deliberately left untouched: disposing a
		// host-context processor must never tear down the shared element agent.
		if (_processorAlc != AssemblyLoadContext.Default && _processorAlc.IsCollectible)
		{
			if (ReferenceEquals(_instance, this))
			{
				_instance = null;
			}

			ReleasePerContextStatics();
		}
	}

	string IClientProcessor.Scope => WellKnownScopes.HotReload;

	public async Task Initialize()
		=> await ConfigureServer();

	public async Task ProcessFrame(Messages.Frame frame)
	{
		switch (frame.Name)
		{
			case AssemblyDeltaReload.Name:
				ProcessAssemblyReload(frame.GetContent<AssemblyDeltaReload>());
				break;

			case UpdateSingleFileResponse.Name:
				// Dev server is not in sync with the application ... this should not happen, but we can safely handle that
				var single = frame.GetContent<UpdateSingleFileResponse>();
				var multi = new UpdateFileResponse(single.RequestId, null, [new FileEditResult(single.FilePath, single.Result, single.Error)], single.HotReloadCorrelationId);
				ProcessUpdateFileResponse(multi);
				break;

			case UpdateFileResponse.Name:
				ProcessUpdateFileResponse(frame.GetContent<UpdateFileResponse>());
				break;

			case HotReloadWorkspaceLoadResult.Name:
				ProcessWorkspaceLoadResult(frame.GetContent<HotReloadWorkspaceLoadResult>());
				break;

			case HotReloadStatusMessage.Name:
				await ProcessServerStatus(frame.GetContent<HotReloadStatusMessage>());
				break;

			default:
				// An unrecognized frame name does not indicate a client error: a frame can be
				// relayed over a scope this processor participates in while being addressed to a
				// different consumer (for example host-only diagnostics frames). Log at Debug so a
				// benign, expected frame does not surface as a red error in the console.
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().LogDebug($"Received unknown frame [{frame.Scope}/{frame.Name}]");
				}
				break;
		}
	}

	partial void ProcessUpdateFileResponse(UpdateFileResponse response);

	#region Configure hot-reload
	private async Task ConfigureServer()
	{
		var assembly = _rcClient.AppType.Assembly;
		if (assembly.GetCustomAttributes(typeof(ProjectConfigurationAttribute), false) is ProjectConfigurationAttribute[] { Length: > 0 } configs)
		{
			_status.ReportServerState(HotReloadState.Initializing);

			try
			{
				var config = configs.First();

				_projectPath = config.ProjectPath;

				_msbuildProperties = Messages.ConfigureServer.ParseMSBuildProperties(config.MSBuildProperties);

				ConfigureHotReloadMode();
				InitializeMetadataUpdater();

				if (!_supportsMetadataUpdates)
				{
					_status.ReportLocallyDisabledState("Environment not supported");
				}

				var hrDebug = Debugger.IsAttached && Environment.GetEnvironmentVariable("__UNO_SUPPORT_DEBUG_HOT_RELOAD__") == "true";
				var message = new ConfigureServer(
					_projectPath,
					GetMetadataUpdateCapabilities(),
					config.MSBuildProperties,
					HotReloadInfoHelper.GetInfoFilePath(assembly),
					_serverMetadataUpdatesEnabled,
					hrDebug,
					GetRuntimeTargetFramework(assembly));

				await _rcClient.SendMessage(message);

				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"Successfully sent request to configure HR server for project '{_projectPath}'.");
				}
			}
			catch (Exception error)
			{
				_status.ReportServerState(HotReloadState.Disabled);

				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().LogError("Unable to configure HR server", error);
				}
			}
		}
		else
		{
			_status.ReportServerState(HotReloadState.Disabled);

			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().LogError("Unable to configure HR server as ProjectConfigurationAttribute is missing.");
			}
		}
	}

	/// <summary>
	/// Composes the target framework the application is running on, from data available at
	/// runtime only: the platform is probed through <see cref="OperatingSystem"/> checks, the
	/// framework version comes from the application assembly's
	/// <see cref="System.Runtime.Versioning.TargetFrameworkAttribute"/> (falling back to the
	/// runtime version). This intentionally does NOT rely on MSBuild property capture, so it
	/// stays correct regardless of how the IDE orchestrated the build.
	/// </summary>
	// Internal (rather than private) to allow validation from runtime tests via InternalsVisibleTo.
	internal static string GetRuntimeTargetFramework(System.Reflection.Assembly appAssembly)
	{
		var frameworkName = appAssembly.GetCustomAttributes(typeof(System.Runtime.Versioning.TargetFrameworkAttribute), false)
			is [System.Runtime.Versioning.TargetFrameworkAttribute { FrameworkName: { Length: > 0 } name }, ..]
			? name
			: null;

		var frameworkVersion = ResolveFrameworkVersion(frameworkName);

		// The platform MUST be probed at runtime, not from compile-time symbols: the skia flavor
		// of this assembly is compiled for the plain `netX.0` TFM yet serves every skia-rendering
		// head — the uno-runtime asset selection swaps it in for `netX.0-android`, `netX.0-ios`,
		// etc. heads too, where `__ANDROID__`-style symbols were never defined. Mac Catalyst is
		// tested before iOS as it also reports IsIOS(). On desktop no check matches, and the
		// flavor cannot tell a `netX.0-desktop` head from a plain `netX.0` one, so it reports the
		// `skia` pseudo-platform which the server treats as matching either spelling.
		var platform =
			OperatingSystem.IsAndroid() ? "android"
			: OperatingSystem.IsTvOS() ? "tvos"
			: OperatingSystem.IsMacCatalyst() ? "maccatalyst"
			: OperatingSystem.IsIOS() ? "ios"
			: OperatingSystem.IsBrowser() ? "browserwasm"
			: "skia";

		return $"net{frameworkVersion.Major}.{frameworkVersion.Minor}-{platform}";
	}

	/// <summary>
	/// The framework version reported when the application's target framework cannot be resolved
	/// (absent, malformed, or a non-.NETCoreApp moniker) — the version of the running .NET runtime.
	/// </summary>
	internal static Version FallbackVersion => Environment.Version;

	/// <summary>
	/// Parses the .NETCoreApp framework version out of a
	/// <see cref="System.Runtime.Versioning.TargetFrameworkAttribute.FrameworkName"/> value
	/// (e.g. <c>".NETCoreApp,Version=v10.0"</c> → <c>10.0</c>), falling back to
	/// <see cref="FallbackVersion"/> when the value is absent, malformed, or not a
	/// .NETCoreApp moniker. Kept free of the platform <c>#if</c> in
	/// <see cref="GetRuntimeTargetFramework"/> so the fallback is unit-testable.
	/// </summary>
	internal static Version ResolveFrameworkVersion(string? frameworkName)
	{
		try
		{
			if (frameworkName is { Length: > 0 }
				&& new System.Runtime.Versioning.FrameworkName(frameworkName) is { Identifier: ".NETCoreApp" } parsed)
			{
				return parsed.Version;
			}
		}
		catch (ArgumentException)
		{
			// Malformed FrameworkName — fall back to the runtime version below.
		}

		return FallbackVersion;
	}

	private void ConfigureHotReloadMode()
	{
		var unoHotReloadMode = GetMSBuildProperty("UnoHotReloadMode");

		if (!string.IsNullOrEmpty(unoHotReloadMode))
		{
			if (!Enum.TryParse<HotReloadMode>(unoHotReloadMode, true, out var hotReloadMode))
			{
				throw new NotSupportedException($"The hot reload mode {unoHotReloadMode} is not supported.");
			}

			_forcedHotReloadMode = hotReloadMode;

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"Forced Hot Reload Mode:{_forcedHotReloadMode}");
			}
		}
	}

	private string GetMSBuildProperty(string property, string defaultValue = "")
	{
		var output = defaultValue;

		if (_msbuildProperties is not null && !_msbuildProperties.TryGetValue(property, out output))
		{
			return defaultValue;
		}

		return output;
	}
	#endregion

	private async Task ProcessServerStatus(HotReloadStatusMessage status)
	{
#if HAS_UNO_WINUI
		_status.ReportServerStatus(status);
#endif
	}
}
