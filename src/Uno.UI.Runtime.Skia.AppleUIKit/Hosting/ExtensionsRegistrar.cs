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
using Uno.WinUI.Runtime.Skia.AppleUIKit.Controls;
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
		ApiExtensibility.Register<IXamlRootHost>(typeof(IUnoCorePointerInputSource),
			o => (o as RootViewController)?.PointerInputSource ?? throw new ArgumentException($"{nameof(o)} must be a {nameof(RootViewController)} instance"));
		ApiExtensibility.Register<IXamlRootHost>(typeof(IUnoKeyboardInputSource),
			o => (o as RootViewController)?.KeyboardInputSource ?? throw new ArgumentException($"{nameof(o)} must be a {nameof(RootViewController)} instance"));
		ApiExtensibility.Register<ContentPresenter>(typeof(ContentPresenter.INativeElementHostingExtension), o => new UIKitNativeElementHostingExtension(o));
		ApiExtensibility.Register<TextBoxView>(typeof(IOverlayTextBoxViewExtension), o => new InvisibleTextBoxViewExtension(o));
		ApiExtensibility.Register(typeof(IImeTextBoxExtension), _ => AppleUIKitImeTextBoxExtension.Instance);
		ApiExtensibility.Register<MediaPlayerPresenter>(typeof(IMediaPlayerPresenterExtension), o => new MediaPlayerPresenterExtension(o));
		ApiExtensibility.Register<InputPane>(typeof(IInputPaneExtension), o => new InputPaneExtension());
#if !__TVOS__
		ApiExtensibility.Register<CoreWebView2>(typeof(INativeWebViewProvider), o => new UIKitNativeWebViewProvider(o));
#endif
		// GLCanvasElement (OpenGL ES via EAGL) - available on iOS/tvOS only.
		Uno.UI.Runtime.Skia.AppleUIKit.AppleUIKitNativeOpenGLWrapper.Register();

		_registered = true;
	}
}
