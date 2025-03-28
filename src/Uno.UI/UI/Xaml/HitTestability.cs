#nullable enable

using System;
using System.Linq;

namespace Windows.UI.Xaml
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

	internal static class GetHitTestabilityExtensions
	{
		/// <summary>
		/// Wrap the given hitTest delegate to exclude (i.e., consider as <see cref="HitTestability.Collapsed"/>) the provided element.
		/// </summary>
		/// <param name="hitTest">The hit-testing delegate to wrap.</param>
		/// <param name="element">The element that should be considered as <see cref="HitTestability.Collapsed"/>).</param>
		/// <returns>A new hit-testing delegate that treats the specified element as collapsed.</returns>
		internal static GetHitTestability Except(this GetHitTestability hitTest, UIElement element)
		{
			GetHitTestability hitTestExceptElement = default!;
			hitTestExceptElement = elt =>
			{
				if (elt == element)
				{
					return (HitTestability.Collapsed, hitTest);
				}
				else
				{
					var (hitTestability, childrenGetHitTestability) = hitTest(elt);

					// If the childrenGetHitTestability is no longer the provided 'hitTest' we need to re-wrap it!
					childrenGetHitTestability = childrenGetHitTestability == hitTest
						? hitTestExceptElement!
						: childrenGetHitTestability.Except(element);

					return (hitTestability, childrenGetHitTestability);
				}
			};

			return hitTestExceptElement;
		}
	}
}
