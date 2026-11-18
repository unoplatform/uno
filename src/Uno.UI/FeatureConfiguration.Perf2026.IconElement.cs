#nullable enable

namespace Uno.UI;

public static partial class FeatureConfiguration
{
	public static partial class Perf2026
	{
		private static bool? _iconElementNoGridContainer;

		/// <summary>
		/// When enabled, <see cref="Microsoft.UI.Xaml.Controls.FontIcon"/> and
		/// <see cref="Microsoft.UI.Xaml.Controls.BitmapIcon"/> host their inner <c>TextBlock</c>/<c>Image</c>
		/// directly instead of nesting it in a <c>Grid</c> filled with a transparent brush, saving two
		/// objects and one layout/render level per icon. Defaults to <see cref="EnableAll"/>.
		/// </summary>
		/// <remarks>
		/// This removes a level from the visual tree, so code reaching into an icon by index
		/// (<c>VisualTreeHelper.GetChild(icon, 0)</c>) observes the inner element instead of the grid.
		/// The value is captured when an icon creates its child, so it must be set before icons are created.
		/// </remarks>
		public static bool IconElementNoGridContainer
		{
			get => _iconElementNoGridContainer ?? EnableAll;
			set => _iconElementNoGridContainer = value;
		}
	}
}
