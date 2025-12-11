using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Windows.UI.Core;
using Microsoft.UI.Xaml;

using Uno.Foundation.Logging;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Dispatching;

namespace Uno.UI.Runtime.Skia.MacOS;

public class MacSkiaHost : SkiaHost, ISkiaApplicationHost
{
	/// <summary>
	/// The application builder used to create the application instance, used in normal startup.
	/// </summary>
	private static Func<Application>? _appBuilder;

	/// <summary>
	/// The application builder used to create an ALC based application instance.
	/// </summary>
	private Func<Application>? _instanceAppBuilder;

	/// <summary>
	/// Whether the host has been initialized for the whole process.
	/// </summary>
	/// <remarks>This field does not need synchronized since it's set only once at the beginning of the process.</remarks>
	private static bool _isInitialized;
	/// <summary>
	/// Whether the main run loop has been started for the whole process.
	/// </summary>
	/// <remarks>This field does not need synchronized since it's set only once at the beginning of the process.</remarks>
	private static bool _isRunning;

	[ThreadStatic] private static bool _isDispatcherThread;
	[ThreadStatic] private static MacSkiaHost? _current;

	static MacSkiaHost()
	{
		MacOSWindowHost.Register(); // must be initialized first to load libSkiaSharp

		MacOSAnalyticsInfoExtension.Register();
		MacOSApplicationViewExtension.Register();
		MacOSBadgeUpdaterExtension.Register();
		MacOSClipboardExtension.Register();
		MacOSCoreApplicationExtension.Register();
		MacOSFileOpenPickerExtension.Register();
		MacOSFileSavePickerExtension.Register();
		MacOSFolderPickerExtension.Register();
		MacOSLauncherExtension.Register();
		MacOSNativeElementHostingExtension.Register();
		MacOSNativeWindowFactoryExtension.Register();
		MacOSSystemThemeHelperExtension.Register();
		MacOSNativeOpenGLWrapper.Register();
		MacOSNativeWebViewProvider.Register();
		MacOSMediaPlayerExtension.Register();
		MacOSMediaPlayerPresenterExtension.Register();
	}

	public MacSkiaHost(Func<Application> appBuilder)
	{
		_current = this;
		_appBuilder = appBuilder;
		_instanceAppBuilder = appBuilder;
	}

	internal static MacSkiaHost Current => _current!;

	internal MacOSWindowNative? InitialWindow { get; set; }

	public RenderSurfaceType RenderSurfaceType { get; set; }

	protected override void Initialize()
	{
		if (!_isInitialized)
		{
			_isInitialized = true;

			if (!InitializeMac())
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"Could not create the native NSApplication host");
				}
				return;
			}

			InitializeDispatcher();
		}
		else
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug("Main NSApplication is already initialized, skipping initialization.");
			}
		}
	}

	protected override unsafe Task RunLoop()
	{
		if (!_isRunning)
		{
			_isRunning = true;

			// we'll call to NSApplication so it needs to be initialized first (and know what to call once done)
			NativeUno.uno_set_application_start_callback(&StartApp);

			// `argc` and `argv` parameters are ignored by macOS
			// see https://developer.apple.com/documentation/appkit/1428499-nsapplicationmain?language=objc
			_ = NativeMac.NSApplicationMain(argc: 0, argv: nint.Zero);
		}
		else
		{
			// For ALC scenarios, we need to start the secondary application on the main thread
			NativeDispatcher.Main.Enqueue(() =>
			{
				Application CreateApp(ApplicationInitializationCallbackParams _)
				{
					if (this.Log().IsEnabled(LogLevel.Debug))
					{
						this.Log().Debug("Creating secondary App");
					}

					var app = _instanceAppBuilder!();
					app.Host = Current;
					return app;
				}

				Application.Start(CreateApp);

			}, NativeDispatcherPriority.Normal);
		}

		return Task.CompletedTask;
	}

	private void InitializeDispatcher()
	{
		_isDispatcherThread = true;

		CoreDispatcher.DispatchOverride = MacOSDispatcher.DispatchNativeSingle;
		CoreDispatcher.HasThreadAccessOverride = () => _isDispatcherThread;
	}

	// called from native code to ensure NSApplication is fully initialized
	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	static private void StartApp()
	{
		try
		{
			// if packaged as an app bundle, set the resource path base to the Resources folder
			if (NativeUno.uno_application_is_bundled())
			{
				Windows.Storage.StorageFile.ResourcePathBase = Path.Combine(Windows.ApplicationModel.Package.Current.InstalledPath, "Resources");
			}

			Application CreateApp(ApplicationInitializationCallbackParams _)
			{
				var app = _appBuilder!();
				app.Host = Current;
				return app;
			}
			Application.Start(CreateApp);
		}
		catch (Exception e)
		{
			// the exception would be printed on the console if this was not called from native code
			Console.WriteLine(e);
			throw;
		}
	}

	private bool InitializeMac()
	{
		try
		{
			// Initialize with Metal unless software rendering is requested
			var metal = FeatureConfiguration.Rendering.UseMetalOnMacOS ?? true;

			// Create the native NSApplication and a main window
			var result = NativeUno.uno_app_initialize(ref metal);

			switch (FeatureConfiguration.Rendering.UseMetalOnMacOS)
			{
				case null:
					RenderSurfaceType = metal ? RenderSurfaceType.Metal : RenderSurfaceType.Software;
					break;
				case true:
					if (!metal)
					{
						throw new NotSupportedException("Metal is not supported on this hardware or configuration. Try enabling the software-based renderer. See https://aka.platform.uno/skia-macos");
					}
					RenderSurfaceType = RenderSurfaceType.Metal;
					break;
				case false:
					if (metal)
					{
						if (this.Log().IsEnabled(LogLevel.Warning))
						{
							this.Log().Warn("Metal is supported on this hardware but software rendering was requested.");
						}
					}
					RenderSurfaceType = RenderSurfaceType.Software;
					break;
			}
			return result;
		}
		catch (Exception e)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"Could not create the native NSApplication host: {e}");
			}
			return false;
		}
	}
}
