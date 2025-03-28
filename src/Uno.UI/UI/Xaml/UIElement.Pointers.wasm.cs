#nullable enable

using Uno.UI;
using Uno.UI.Xaml;

namespace Windows.UI.Xaml;

public partial class UIElement : DependencyObject
{
	private protected virtual void OnHitTestVisibilityChanged(HitTestability oldValue, HitTestability newValue)
	{
		// By default, elements have 'pointer-event' set to 'none' (see Uno.UI.css .uno-uielement class)
		// which is aligned with HitTestVisibilityProperty's default value of Visible.
		// If HitTestVisibilityProperty is calculated to Invisible or Collapsed,
		// we don't want to be the target of hit-testing and raise any pointer events.
		// This is done by setting 'pointer-events' to 'none'.
		// However, setting it to 'none' will allow pointer event to pass through the element (a.k.a. Invisible)

		// Note: Even if we are now dispatching events in managed code only, we still want to properly set the pointer-events value in order to
		//		1. support managed only element, 
		//		2. support "default" behavior of browser (like scrolling).

		WindowManagerInterop.SetPointerEvents(HtmlId, newValue is HitTestability.Visible);

		if (FeatureConfiguration.UIElement.AssignDOMXamlProperties)
		{
			UpdateDOMProperties();
		}
	}
}
