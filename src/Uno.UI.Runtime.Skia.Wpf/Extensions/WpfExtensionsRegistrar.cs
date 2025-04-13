﻿using Uno.ApplicationModel.DataTransfer;
using Uno.Extensions.ApplicationModel.Core;
using Uno.Extensions.ApplicationModel.DataTransfer;
using Uno.Extensions.Networking.Connectivity;
using Uno.Extensions.Storage.Pickers;
using Uno.Extensions.System;
using Uno.Extensions.System.Profile;
using Uno.Foundation.Extensibility;
using Uno.Helpers.Theming;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.Wpf.Extensions.Helpers.Theming;
using Uno.UI.Runtime.Skia.Wpf.Extensions.UI.Xaml.Controls;
using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Controls.Extensions;
using Uno.UI.XamlHost.Skia.Wpf;
using Windows.Graphics.Display;
using Windows.Networking.Connectivity;
using Windows.Storage.Pickers;
using Windows.System.Profile.Internal;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Runtime.Skia.Extensions.System;
using Uno.UI.Runtime.Skia.Wpf.Input;
using Microsoft.Web.WebView2.Core;
using Uno.Graphics;

namespace Uno.UI.Runtime.Skia.Wpf.Extensions;

internal static class WpfExtensionsRegistrar
{
	private static bool _registered;

	internal static void Register()
	{
		if (_registered)
		{
			return;
		}

		ApiExtensibility.Register(typeof(INativeWindowFactoryExtension), o => new NativeWindowFactoryExtension());
		ApiExtensibility.Register(typeof(Uno.ApplicationModel.Core.ICoreApplicationExtension), o => new CoreApplicationExtension(o));
		ApiExtensibility.Register<IXamlRootHost>(typeof(Windows.UI.Core.IUnoKeyboardInputSource), o => new WpfKeyboardInputSource(o));
		ApiExtensibility.Register<IXamlRootHost>(typeof(Windows.UI.Core.IUnoCorePointerInputSource), o => new WpfCorePointerInputSource(o));
		ApiExtensibility.Register<ContentPresenter>(typeof(ContentPresenter.INativeElementHostingExtension), o => new WpfNativeElementHostingExtension(o));
		ApiExtensibility.Register(typeof(Windows.UI.ViewManagement.IApplicationViewExtension), o => new WpfApplicationViewExtension(o));
		ApiExtensibility.Register(typeof(ISystemThemeHelperExtension), o => new WpfSystemThemeHelperExtension(o));
		ApiExtensibility.Register(typeof(IDisplayInformationExtension), o => new WpfDisplayInformationExtension(o));
		ApiExtensibility.Register<DragDropManager>(typeof(Windows.ApplicationModel.DataTransfer.DragDrop.Core.IDragDropExtension), o => new WpfDragDropExtension(o));
		ApiExtensibility.Register(typeof(IFileOpenPickerExtension), o => new FileOpenPickerExtension(o));
		ApiExtensibility.Register<FolderPicker>(typeof(IFolderPickerExtension), o => new FolderPickerExtension(o));
		ApiExtensibility.Register(typeof(IFileSavePickerExtension), o => new FileSavePickerExtension(o));
		ApiExtensibility.Register(typeof(IConnectionProfileExtension), o => new WindowsConnectionProfileExtension(o));
		ApiExtensibility.Register<TextBoxView>(typeof(IOverlayTextBoxViewExtension), o => new TextBoxViewExtension(o));
		ApiExtensibility.Register(typeof(ILauncherExtension), o => new WindowsLauncherExtension(o));
		ApiExtensibility.Register(typeof(IClipboardExtension), o => new ClipboardExtensions(o));
		ApiExtensibility.Register(typeof(IAnalyticsInfoExtension), o => new AnalyticsInfoExtension());
		ApiExtensibility.Register<CoreWebView2>(typeof(INativeWebViewProvider), o => new WpfNativeWebViewProvider(o));
		ApiExtensibility.Register<XamlRoot>(typeof(INativeOpenGLWrapper), xamlRoot => new WpfNativeOpenGLWrapper(xamlRoot));

		_registered = true;
	}
}
