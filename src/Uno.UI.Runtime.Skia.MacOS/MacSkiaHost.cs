using Windows.UI.Core;
using Microsoft.UI.Xaml;

using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia.MacOS;

public class MacSkiaHost : ISkiaApplicationHost
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
		MacOSClipboardExtension.Register();         // work in progress
		MacOSCoreApplicationExtension.Register();
		MacOSDisplayInformationExtension.Register();
		MacOSFileOpenPickerExtension.Register();
		MacOSFileSavePickerExtension.Register();
		MacOSFolderPickerExtension.Register();
		MacOSLauncherExtension.Register();
		MacOSNativeElementHostingExtension.Register();  // seems used only for native elements, which Skia avoids ?!?
		MacOSNativeWindowFactoryExtension.Register();
		MacOSSystemNavigationManagerPreviewExtension.Register();
		MacOSSystemThemeHelperExtension.Register();
		MacOSUnoCorePointerInputSource.Register();  // work in progress
		MacOSUnoKeyboardInputSource.Register();

		// Uno.UI.Xaml.Controls.Extensions.IOverlayTextBoxViewExtension is not required
	}

	public MacSkiaHost(Func<Application> appBuilder)
	{
		_current = this;
		_appBuilder = appBuilder;
	}

	internal static MacSkiaHost? Current => _current;

	internal MacOSWindowNative? InitialWindow { get; set; }

	public RenderSurfaceType RenderSurfaceType { get; set; }

	public void Run()
	{
		if (!InitializeMac())
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"Could not create the native NSApplication host");
			}
			return;
		}

		InitializeDispatcher();

		StartApp();

		// `argc` and `argv` parameters are ignored by macOS
		// see https://developer.apple.com/documentation/appkit/1428499-nsapplicationmain?language=objc
#pragma warning disable CA1806
		NativeMac.NSApplicationMain(argc: 0, argv: nint.Zero);
#pragma warning restore CA1806
	}

	private void InitializeDispatcher()
	{
		_isDispatcherThread = true;

		CoreDispatcher.DispatchOverride = MacOSDispatcher.DispatchNativeSingle;
		CoreDispatcher.HasThreadAccessOverride = () => _isDispatcherThread;
	}

	private void StartApp()
	{
		void CreateApp(ApplicationInitializationCallbackParams _)
		{
			var app = _appBuilder();
			app.Host = this;
		}
		Application.StartWithArguments(CreateApp);
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
