using System;
using Windows.System;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml
{
	partial class UIElement
	{
#if __CROSSRUNTIME__
		internal bool IsInLiveTree => IsActiveInVisualTree;
#else
		internal bool IsInLiveTree => throw new NotSupportedException();
#endif

#if !__CROSSRUNTIME__
		// Non-enhanced-lifecycle flavors (unit tests, native Android/iOS) have no live-tree tracking;
		// constant false means theme references resolve via the parse-time pinned dictionary.
		internal bool IsActiveInVisualTree => false;

		internal void RemoveChild(UIElement viewToRemove) => VisualTreeHelper.RemoveChild(this, viewToRemove);

		internal void AddChild(UIElement viewToAdd) => VisualTreeHelper.AddChild(this, viewToAdd);

		internal UIElement ReplaceChild(int index, UIElement viewToRemove) => VisualTreeHelper.ReplaceChild(this, index, viewToRemove);
#endif
	}
}
