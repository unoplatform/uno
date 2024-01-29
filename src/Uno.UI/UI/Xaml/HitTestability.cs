#nullable enable

using System;
using System.Linq;

namespace Microsoft.UI.Xaml
{
	internal delegate (HitTestability elementTestability, GetHitTestability childrenGetHitTestability) GetHitTestability(UIElement element);

	internal enum HitTestability
	{
		/// <summary>
		/// The element and its children can't be targeted by hit-testing.
		/// </summary>
		/// <remarks>
		/// This occurs when IsHitTestVisible="False", IsEnabled="False", or Visibility="Collapsed".
		/// </remarks>
		Collapsed,

		/// <summary>
		/// The element can't be targeted by hit-testing, but it's children might be.
		/// </summary>
		/// <remarks>
		/// This usually occurs if an element doesn't have a Background/Fill.
		/// </remarks>
		Invisible,

		/// <summary>
		/// The element can be targeted by hit-testing.
		/// </summary>
		Visible,
	}

	internal record struct StalePredicate(PredicateOfUIElement Method, string Name);

	internal delegate bool PredicateOfUIElement(UIElement element);
}
