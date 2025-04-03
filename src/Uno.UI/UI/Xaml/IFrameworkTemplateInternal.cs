#nullable enable

#if __ANDROID__
using View = Android.Views.View;
#elif __APPLE_UIKIT__
using View = UIKit.UIView;
#else
using View = Microsoft.UI.Xaml.UIElement;
#endif


namespace Microsoft.UI.Xaml;

internal interface IFrameworkTemplateInternal
{
	View? LoadContent(DependencyObject? templatedParent);
}
