#nullable enable

using System;
using System.IO;

using Windows.ApplicationModel;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

using Uno.Foundation.Logging;
using Uno.Runtime.Skia;
using Uno.UI.Xaml.Core;
using Uno.UI.Hosting;

namespace Uno.UI.Runtime.Skia.MacOS;

public partial class MacSkiaHost : SkiaHost, ISkiaApplicationHost, Uno.UI.Hosting.IXamlRootHost {

	private readonly Func<Application> _appBuilder;

	[ThreadStatic] private static bool _isDispatcherThread;
	[ThreadStatic] private static MacSkiaHost? _current;

	static unsafe MacSkiaHost()
	{
		MacOSMetalRenderer.Register();				// must be intialized first to load libSkiaSharp

		MacOSAnalyticsInfoExtension.Register();
		MacOSApplicationViewExtension.Register();
		MacOSClipboardExtension.Register();			// work in progress
		MacOSCoreApplicationExtension.Register();
		MacOSCoreWindowExtension.Register();		// ICoreWindowExtension seems used only for native elements, which Skia avoids ?!?
		MacOSDisplayInformationExtension.Register();
		MacOSFileOpenPickerExtension.Register();
		MacOSFileSavePickerExtension.Register();
		MacOSFolderPickerExtension.Register();
		MacOSLauncherExtension.Register();
		MacOSSystemNavigationManagerPreviewExtension.Register();
		MacOSSystemThemeHelperExtension.Register();
		MacOSUnoCorePointerInputSource.Register();	// work in progress
		MacOSUnoKeyboardInputSource.Register();
		
		// TODO - implement Uno.UI.Xaml.Controls.Extensions.IOverlayTextBoxViewExtension
	}

	public MacSkiaHost(Func<Application> appBuilder)
	{
		_current = this;
		_appBuilder = appBuilder;
	}

	internal static MacSkiaHost? Current => _current;

	public UIElement? RootElement { get; set; }

	internal static XamlRootMap<IXamlRootHost> XamlRootMap { get; } = new();

	protected override unsafe void Initialize()
	{
		_isDispatcherThread = true;

		CoreDispatcher.DispatchOverride = MacOSDispatcher.DispatchNativeSingle;
		CoreDispatcher.HasThreadAccessOverride = () => _isDispatcherThread;

		CoreServices.Instance.ContentRootCoordinator.CoreWindowContentRootSet += OnCoreWindowContentRootSet;

		void CreateApp(ApplicationInitializationCallbackParams _)
		{
			var app = _appBuilder();
			app.Host = this;
		}
		Application.StartWithArguments(CreateApp);

		var appView = ApplicationView.GetForCurrentView();

		// Create the native NSApplication, NSWindow, NSViewController and MTKView and return the Metal context we need for SkiaSharp
		// this must be done before other actions, like setting the app icon
		var ctx = NativeUno.uno_app_initialize(appView.Title);
		Window.Current.OnNativeWindowCreated();

		UpdateWindowPropertiesFromPackage(appView);
		UpdateWindowPropertiesFromApplicationView(appView);
		appView.PropertyChanged += (sender, args) => UpdateWindowPropertiesFromPackage(appView);

		var size = ApplicationView.PreferredLaunchViewSize;
		if (!size.IsEmpty)
		{
			var main = NativeUno.uno_app_get_main_window();
			NativeUno.uno_window_resize(main, size.Width, size.Height);
		}

		MacOSMetalRenderer.CreateContext(ctx);
	}

	private void OnCoreWindowContentRootSet(object? sender, object e)
	{
		var contentRoot = CoreServices.Instance.ContentRootCoordinator.CoreWindowContentRoot;
		var xamlRoot = (contentRoot?.GetOrCreateXamlRoot()) ?? throw new InvalidOperationException("XamlRoot was not properly initialized");
		contentRoot!.SetHost(this);
		XamlRootMap.Register(xamlRoot, this);

		CoreServices.Instance.ContentRootCoordinator.CoreWindowContentRootSet -= OnCoreWindowContentRootSet;
	}

	private void UpdateWindowPropertiesFromPackage(ApplicationView appView)
	{
		if (Package.Current.Logo is { } uri)
		{
			var basePath = uri.OriginalString.Replace('\\', Path.DirectorySeparatorChar);
			var iconPath = Path.Combine(Package.Current.InstalledPath, basePath);

			if (File.Exists(iconPath))
			{
				if (this.Log().IsEnabled(LogLevel.Information))
				{
					this.Log().Info($"Loading icon file [{iconPath}] from Package.appxmanifest file");
				}

				NativeUno.uno_application_set_icon(iconPath);
			}
			else if (BitmapImage.GetScaledPath(basePath) is { } scaledPath && File.Exists(scaledPath))
			{
				if (this.Log().IsEnabled(LogLevel.Information))
				{
					this.Log().Info($"Loading icon file [{scaledPath}] scaled logo from Package.appxmanifest file");
				}

				NativeUno.uno_application_set_icon(iconPath);
			}
			else
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().Warn($"Unable to find icon file [{iconPath}] specified in the Package.appxmanifest file.");
				}
			}
		}

		if (string.IsNullOrEmpty(appView.Title))
		{
			appView.Title = Package.Current.DisplayName;
		}
	}

	private void UpdateWindowPropertiesFromApplicationView(ApplicationView appView)
	{
		var main = NativeUno.uno_app_get_main_window();
		NativeUno.uno_window_set_title(main, appView.Title);
		var minSize = appView.PreferredMinSize;
		NativeUno.uno_window_set_min_size(main, minSize.Width, minSize.Height);
	}

	// `argc` and `argv` parameters are ignored by macOS, see https://developer.apple.com/documentation/appkit/1428499-nsapplicationmain?language=objc
	protected override void RunLoop() => NativeMac.NSApplicationMain(argc: 0, argv: nint.Zero);

	public void InvalidateRender()
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace("MacSkiaHost.InvalidateRender");
		}

		Window.Current.RootElement?.XamlRoot?.InvalidateOverlays();

		var main = NativeUno.uno_app_get_main_window();
		NativeUno.uno_window_invalidate(main);
	}
}
