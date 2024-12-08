using System;
using Windows.System;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml
{
	partial class UIElement
	{
#if __CROSSRUNTIME__
		internal bool IsInLiveTree => IsActiveInVisualTree;
#elif __ANDROID__
		internal bool IsInLiveTree => base.IsAttachedToWindow;
#elif __IOS__ || __MACOS__
		internal bool IsInLiveTree => base.Window != null;
#elif IS_UNIT_TESTS // There is no visual tree concept in unit tests. This is here just in case it's needed indirectly, e.g. in UIElement.GetTransform
		internal bool IsInLiveTree => true;
#else
		internal bool IsInLiveTree => throw new NotSupportedException();
#endif

#if !__CROSSRUNTIME__
		internal void RemoveChild(UIElement viewToRemove) => VisualTreeHelper.RemoveChild(this, viewToRemove);

		internal void AddChild(UIElement viewToAdd) => VisualTreeHelper.AddChild(this, viewToAdd);

		internal UIElement ReplaceChild(int index, UIElement viewToRemove) => VisualTreeHelper.ReplaceChild(this, index, viewToRemove);
#endif

#if !HAS_UNO_WINUI
		// This is to ensure forward compatibility with WinUI
		protected internal DispatcherQueue DispatcherQueue => DispatcherQueue.GetForCurrentThread();
#endif
	}
}
