using Android.Graphics;

namespace Windows.UI.Xaml.Media
{
	// Android partial for SolidColorBrush
	public partial class SolidColorBrush : Brush
	{

		protected override Paint GetPaintInner(Windows.Foundation.Rect destinationRect)
		{
			return new Paint() { Color = this.ColorWithOpacity, AntiAlias = true };
		}

		/// <summary>
		/// Blends the Color set on the SolidColorBrush with its Opacity. Should generally be used for rendering rather than the Color property itself.
		/// </summary>
		internal Windows.UI.Color ColorWithOpacity
		{
			get; set;
		}

		partial void UpdateColorWithOpacity(Color newColor, double opacity)
		{
			ColorWithOpacity = Color.FromArgb((byte)(opacity * newColor.A), newColor.R, newColor.G, newColor.B);
		}
	}
}
