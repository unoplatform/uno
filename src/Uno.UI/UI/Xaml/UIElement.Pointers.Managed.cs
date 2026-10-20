#if UNO_HAS_MANAGED_POINTERS
#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

using Uno.Disposables;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.DataBinding;
using Uno.UI.Extensions;
using Uno.UI.Helpers.Boxes;
using Windows.UI.Core;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.UI;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Islands;

using Microsoft.UI.Input;

namespace Microsoft.UI.Xaml
{
	partial class UIElement
	{
		private HitTestability _hitTestVisibility = HitTestability.Collapsed;

		/// <summary>
		/// Represents the final calculated hit-test visibility of the element: it is computed from the element's own state
		/// and its parent's value, and is never set directly.
		/// </summary>
		internal HitTestability HitTestVisibility => _hitTestVisibility;

		/// <summary>
		/// Recomputes <see cref="HitTestVisibility"/> after one of its inputs changed, and pushes the change down the subtree.
		/// </summary>
		/// <remarks>
		/// Kept out of the property system on purpose: it used to be an inherited coerced DP, and every change paid the
		/// generic inheritance and precedence cost on every descendant, though the computation itself is a few checks.
		/// </remarks>
		internal void UpdateHitTest() => UpdateHitTest(GetParentHitTestVisibility());

		private void UpdateHitTest(HitTestability parentValue)
		{
			var value = ComputeHitTestVisibility(parentValue);
			if (value == _hitTestVisibility)
			{
				return; // The subtree was computed against the same value, so it is up to date.
			}

			_hitTestVisibility = value;

			if (value == HitTestability.Collapsed)
			{
				_currentPointerEventDispatch.VisualTreeAltered = true;
				ClearPointerState();
			}

			var children = _children;
			for (var i = 0; i < children.Count; i++)
			{
				children[i].UpdateHitTest(value);
			}
		}

		private HitTestability GetParentHitTestVisibility()
		{
			// The value flows along the store parents, skipping any non-UIElement in between, as the inherited DP did.
			for (var parent = this.GetParent(); parent is not null; parent = parent.GetParent())
			{
				if (parent is UIElement parentElement)
				{
					return parentElement._hitTestVisibility;
				}
			}

			return HitTestability.Collapsed;
		}

		/// <summary>
		/// This calculates the final hit-test visibility of an element.
		/// </summary>
		private HitTestability ComputeHitTestVisibility(HitTestability parentValue)
		{
			if (this is RootVisual or XamlIslandRoot)
			{
				return HitTestability.Visible;
			}

			// If the parent is collapsed, we should be collapsed as well. This takes priority over everything else, even if we would be visible otherwise.
			if (parentValue == HitTestability.Collapsed)
			{
				return HitTestability.Collapsed;
			}

			// If we're not locally hit-test visible, visible, or enabled, we should be collapsed. Our children will be collapsed as well.
			if (
				!IsLoaded ||
				!IsHitTestVisible || Visibility != Visibility.Visible || !IsEnabledOverride())
			{
				return HitTestability.Collapsed;
			}

			// If we're not hit (usually means we don't have a Background/Fill), we're invisible. Our children will be visible or not, depending on their state.
			if (!IsViewHit())
			{
				return HitTestability.Invisible;
			}

			// If we're not collapsed or invisible, we can be targeted by hit-testing. This means that we can be the source of pointer events.
			return HitTestability.Visible;
		}
	}
}
#endif
