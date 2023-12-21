#nullable enable

#if __ANDROID__
using View = Android.Views.View;
#elif __IOS__
using View = UIKit.UIView;
#elif __MACOS__
using View = AppKit.NSView;
#else
using View = Microsoft.UI.Xaml.UIElement;
#endif


namespace Microsoft.UI.Xaml;

internal interface IFrameworkTemplateInternal
{
	View? LoadContent();
}
