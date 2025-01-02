using Windows.UI.Xaml;

namespace Uno.UI.Extensions
{
	public static partial class UIElementExtensions
	{

		/// <summary>
		/// Returns the root of the view's local visual tree.
		/// </summary>
		internal static FrameworkElement GetTopLevelParent(this UIElement view)
		{
			var current = view as FrameworkElement;
			while (current != null)
			{
				if (current.Parent is FrameworkElement parent)
				{
					current = parent;
				}
				else
				{
					break;
				}
			}

			return current;
		}
	}
}
