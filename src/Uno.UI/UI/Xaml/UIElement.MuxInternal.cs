using System;
using Windows.System;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml
{
	partial class UIElement
	{
#if __CROSSRUNTIME__
		internal bool IsInLiveTree => IsLoading || IsLoaded;
#elif __ANDROID__
		internal bool IsInLiveTree => base.IsAttachedToWindow;
#elif __IOS__ || __MACOS__
		internal bool IsInLiveTree => base.Window != null;
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
