#nullable enable

using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Microsoft.Web.WebView2.Core;
using Uno.Foundation.Extensibility;
using Uno.Graphics;
using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Controls.Extensions;
using Uno.WinUI.Runtime.Skia.Android;
using Uno.WinUI.Runtime.Skia.Android.UI.Xaml.Controls.TextBox;
using Windows.UI.Core;
using Windows.UI.ViewManagement;

namespace Uno.UI.Runtime.Skia.Android;

internal static class ExtensionsRegistrar
{
	private static bool _registered;

	internal static void Register()
	{
		if (_registered)
		{
			return;
		}

		ApiExtensibility.Register(typeof(INativeWindowFactoryExtension), o => new AndroidSkiaWindowFactory());
		ApiExtensibility.Register(typeof(IUnoCorePointerInputSource), o => AndroidCorePointerInputSource.Instance);
		ApiExtensibility.Register(typeof(IUnoKeyboardInputSource), o => AndroidKeyboardInputSource.Instance);
		ApiExtensibility.Register(typeof(ITextBoxNotificationsProviderSingleton), _ => AndroidSkiaTextBoxNotificationsProviderSingleton.Instance);
		ApiExtensibility.Register<ContentPresenter>(typeof(ContentPresenter.INativeElementHostingExtension), o => new AndroidSkiaNativeElementHostingExtension(o));
		ApiExtensibility.Register<CoreWebView2>(typeof(INativeWebViewProvider), o => new AndroidNativeWebViewProvider(o));
		ApiExtensibility.Register(typeof(ISkiaNativeDatePickerProviderExtension), _ => new AndroidSkiaDatePickerProvider());
		ApiExtensibility.Register(typeof(ISkiaNativeTimePickerProviderExtension), _ => new AndroidSkiaTimePickerProvider());
		ApiExtensibility.Register(typeof(IInputPaneExtension), _ => new InputPaneExtension());
		ApiExtensibility.Register(typeof(IGestureRecognizerExtension), _ => AndroidGestureRecognizerExtension.Instance);
		ApiExtensibility.Register<MediaPlayerPresenter>(typeof(IMediaPlayerPresenterExtension), o => new AndroidSkiaMediaPlayerPresenterExtension(o));
		ApiExtensibility.Register(typeof(IFontFallbackService), _ => AndroidSkiaFontFallbackService.Instance);
		ApiExtensibility.Register(typeof(IImeTextBoxExtension), _ => new AndroidImeTextBoxExtension());
		ApiExtensibility.Register<XamlRoot>(typeof(INativeOpenGLWrapper), xamlRoot => new AndroidNativeOpenGLWrapper(xamlRoot));

		_registered = true;
	}
}
