using System.Runtime.CompilerServices;
using Windows.Foundation;
using Uno.Collections;

namespace Windows.UI.Xaml.Controls.Primitives
{
	partial class LayoutInformation
	{
		private static readonly UnsafeWeakAttachedDictionary<object, string> _layoutProperties = new();

		#region AvailableSize
		public static Size GetAvailableSize(UIElement element)
			=> element.LastAvailableSize;

		internal static Size GetAvailableSize(object view)
			=> view is IUIElement iue
				? iue.LastAvailableSize
				: _layoutProperties.GetValue(view, "availablesize", () => default(Size));

		internal static void SetAvailableSize(object view, Size value)
		{
			if (view is IUIElement iue)
			{
				iue.LastAvailableSize = value;
			}
			else
			{
				_layoutProperties.SetValue(view, "availablesize", value);
			}
		}
		#endregion

		#region LayoutSlot
		public static Rect GetLayoutSlot(FrameworkElement element)
			=> element.LayoutSlot;

		internal static Rect GetLayoutSlot(object view)
			=> view is IUIElement iue
				? iue.LayoutSlot
				: _layoutProperties.GetValue(view, "layoutslot", () => default(Rect));

		internal static void SetLayoutSlot(object view, Rect value)
		{
			if (view is IUIElement iue)
			{
				iue.LayoutSlot = value;
			}
			else
			{
				_layoutProperties.SetValue(view, "layoutslot", value);
			}
		}
		#endregion

		#region DesiredSize
		internal static Size GetDesiredSize(UIElement element)
			=> element.DesiredSize;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Size GetDesiredSize(object view)
		{
			switch (view)
			{
				case IUIElement iue:
					return iue.DesiredSize;
				default:
					return _layoutProperties.GetValue(view, "desiredSize", () => default(Size));
			}
		}

		internal static void SetDesiredSize(object view, Size desiredSize)
		{
			switch (view)
			{
				case IUIElement iue:
					iue.DesiredSize = desiredSize;
					break;
				default:
					_layoutProperties.SetValue(view, "desiredSize", desiredSize);
					break;
			}
		}
		#endregion
	}
}
