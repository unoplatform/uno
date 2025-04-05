using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Uno.Foundation.Extensibility;
using Uno.UI.Hosting;
using Uno.UI.Xaml.Controls.Extensions;
using Uno.UI.Xaml.Controls;
using Windows.UI.Core;
using Uno.WinUI.Runtime.Skia.AppleUIKit.UI.Xaml;
using Uno.UI.Runtime.Skia.AppleUIKit;
using Microsoft.Web.WebView2.Core;
using Windows.UI.ViewManagement;

namespace Uno.WinUI.Runtime.Skia.AppleUIKit.Extensions;

internal class ExtensionsRegistrar
{
	private static bool _registered;

	internal static void Register()
	{
		if (_registered)
		{
			return;
		}

		ApiExtensibility.Register(typeof(INativeWindowFactoryExtension), o => new NativeWindowFactoryExtension());
		ApiExtensibility.Register<IXamlRootHost>(typeof(IUnoCorePointerInputSource), o => AppleUIKitCorePointerInputSource.Instance);
		ApiExtensibility.Register<IXamlRootHost>(typeof(IUnoKeyboardInputSource), o => UnoKeyboardInputSource.Instance);
		ApiExtensibility.Register<ContentPresenter>(typeof(ContentPresenter.INativeElementHostingExtension), o => new UIKitNativeElementHostingExtension(o));
		ApiExtensibility.Register<TextBoxView>(typeof(IOverlayTextBoxViewExtension), o => new InvisibleTextBoxViewExtension(o));
		ApiExtensibility.Register<MediaPlayerPresenter>(typeof(IMediaPlayerPresenterExtension), o => new MediaPlayerPresenterExtension(o));
		ApiExtensibility.Register<InputPane>(typeof(IInputPaneExtension), o => new InputPaneExtension());
#if !__TVOS__
		ApiExtensibility.Register<CoreWebView2>(typeof(INativeWebViewProvider), o => new UIKitNativeWebViewProvider(o));
#endif

		_registered = true;
	}
}
