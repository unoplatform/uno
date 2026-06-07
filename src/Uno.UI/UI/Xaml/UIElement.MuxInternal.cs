using System;
using Windows.System;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml
{
	partial class UIElement
	{
#if __CROSSRUNTIME__
		internal bool IsInLiveTree => IsActiveInVisualTree;
#elif __ANDROID__
		internal bool IsInLiveTree => base.IsAttachedToWindow;
#elif __APPLE_UIKIT__
		internal bool IsInLiveTree => base.Window != null;
#elif IS_UNIT_TESTS // There is no visual tree concept in unit tests. This is here just in case it's needed indirectly, e.g. in UIElement.GetTransform
		internal bool IsInLiveTree => true;
#else
		internal bool IsInLiveTree => throw new NotSupportedException();
#endif

#if !__CROSSRUNTIME__
		// The enhanced-lifecycle visual-tree-active flag is defined (and maintained) only in
		// UIElement.crossruntime.cs / the UNO_HAS_ENHANCED_LIFECYCLE block of UIElement.mux.cs. The theme
		// partials (DependencyObjectStore.Theming.cs, FrameworkElement.Theming.cs) compile on every flavor
		// but only run their tree-walk paths under enhanced lifecycle (Skia/WASM). These flavors (unit tests,
		// native Android/iOS) have no such tracking, so the flag is constant false: theme references resolve
		// via the parse-time pinned dictionary, as they do for a not-yet-active element.
		internal bool IsActiveInVisualTree => false;

		internal void RemoveChild(UIElement viewToRemove) => VisualTreeHelper.RemoveChild(this, viewToRemove);

		internal void AddChild(UIElement viewToAdd) => VisualTreeHelper.AddChild(this, viewToAdd);

		internal UIElement ReplaceChild(int index, UIElement viewToRemove) => VisualTreeHelper.ReplaceChild(this, index, viewToRemove);
#endif
	}
}
