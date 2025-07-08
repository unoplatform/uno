using Windows.UI.Core;
using Microsoft.UI.Xaml;

using Uno.Foundation.Logging;
using Microsoft.UI.Xaml.Media;

namespace Uno.UI.Runtime.Skia.MacOS;

public class MacSkiaHost : SkiaHost, ISkiaApplicationHost
{
	private readonly Func<Application> _appBuilder;

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
	}

	internal static MacSkiaHost Current => _current!;

	internal MacOSWindowNative? InitialWindow { get; set; }

	public RenderSurfaceType RenderSurfaceType { get; set; }

	protected override void Initialize()
	{
		if (!InitializeMac())
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"Could not create the native NSApplication host");
			}
			return;
		}

		// if packaged as an app bundle, set the resource path base to the Resources folder
		if (NativeUno.uno_application_is_bundled())
		{
			Windows.Storage.StorageFile.ResourcePathBase = Path.Combine(Windows.ApplicationModel.Package.Current.InstalledPath, "..", "Resources");
		}

		InitializeDispatcher();
	}

	protected override Task RunLoop()
	{
		StartApp();

		// `argc` and `argv` parameters are ignored by macOS
		// see https://developer.apple.com/documentation/appkit/1428499-nsapplicationmain?language=objc
		_ = NativeMac.NSApplicationMain(argc: 0, argv: nint.Zero);

		return Task.CompletedTask;
	}

	private void InitializeDispatcher()
	{
		_isDispatcherThread = true;

		CoreDispatcher.DispatchOverride = MacOSDispatcher.DispatchNativeSingle;
		CoreDispatcher.HasThreadAccessOverride = () => _isDispatcherThread;

		// We do not have a display timer on this target, we can use
		// a constant timer.
		CompositionTargetTimer.Start();
	}

	private void StartApp()
	{
		void CreateApp(ApplicationInitializationCallbackParams _)
		{
			var app = _appBuilder();
			app.Host = this;
		}
		Application.Start(CreateApp);
	}

	private bool InitializeMac()
	{
		try
		{
			// Initialize with Metal unless software rendering is requested
			var metal = RenderSurfaceType != RenderSurfaceType.Software;

			// Create the native NSApplication and a main window
			var result = NativeUno.uno_app_initialize(ref metal);

			switch (RenderSurfaceType)
			{
				case RenderSurfaceType.Auto:
					RenderSurfaceType = metal ? RenderSurfaceType.Metal : RenderSurfaceType.Software;
					break;
				case RenderSurfaceType.Metal:
					if (!metal)
					{
						throw new NotSupportedException("Metal is not supported on this hardware or configuration. Try enabling the software-based renderer. See https://aka.platform.uno/skia-macos");
					}
					break;
				case RenderSurfaceType.Software:
					if (metal)
					{
						if (this.Log().IsEnabled(LogLevel.Warning))
						{
							this.Log().Warn("Metal is supported on this hardware but software rendering was requested.");
						}
					}
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
