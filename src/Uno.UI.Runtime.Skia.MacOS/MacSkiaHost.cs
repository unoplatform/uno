#nullable enable

using System;

using Windows.UI.Core;
using Microsoft.UI.Xaml;

using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia.MacOS;

public partial class MacSkiaHost : ISkiaApplicationHost
{
	private readonly Func<Application> _appBuilder;

	[ThreadStatic] private static bool _isDispatcherThread;
	[ThreadStatic] private static MacSkiaHost? _current;

	static unsafe MacSkiaHost()
	{
		MacOSMetalRenderer.Register();				// must be initialized first to load libSkiaSharp

		MacOSAnalyticsInfoExtension.Register();
		MacOSApplicationViewExtension.Register();
		MacOSClipboardExtension.Register();			// work in progress
		MacOSCoreApplicationExtension.Register();
		MacOSDisplayInformationExtension.Register();
		MacOSFileOpenPickerExtension.Register();
		MacOSFileSavePickerExtension.Register();
		MacOSFolderPickerExtension.Register();
		MacOSLauncherExtension.Register();
		MacOSNativeElementHostingExtension.Register();	// seems used only for native elements, which Skia avoids ?!?
		MacOSNativeWindowFactoryExtension.Register();
		MacOSSystemNavigationManagerPreviewExtension.Register();
		MacOSSystemThemeHelperExtension.Register();
		MacOSUnoCorePointerInputSource.Register();	// work in progress
		MacOSUnoKeyboardInputSource.Register();
		
		// Uno.UI.Xaml.Controls.Extensions.IOverlayTextBoxViewExtension is not required
	}

	public MacSkiaHost(Func<Application> appBuilder)
	{
		_current = this;
		_appBuilder = appBuilder;
	}

	internal static MacSkiaHost? Current => _current;

	internal MacOSWindowWrapper? InitialWindow { get; set; }

	public void Run()
	{
		if (!InitializeMac())
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error("Could not create the native NSApplication host.");
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
			// Create the native NSApplication and a main window
			NativeUno.uno_app_initialize();
			return true;
		}
		catch(TypeInitializationException)
		{
			return false;
		}
	}
}
