using Windows.Foundation;
using Android.Graphics;
using Uno.UI;

namespace Windows.UI.Xaml.Shapes
{
	public partial class Ellipse : ArbitraryShapeBase
	{
		public Ellipse()
		{
			//Set default stretch value
			this.SetValue(StretchProperty, Media.Stretch.Fill, DependencyPropertyValuePrecedences.DefaultValue);
		}

		protected override Android.Graphics.Path GetPath(Size availableSize)
		{
			var bounds = this.ApplySizeConstraints(availableSize).LogicalToPhysicalPixels();

			var output = new Android.Graphics.Path();
			output.AddOval(
				new RectF(0, 0, (float)bounds.Width, (float)bounds.Height),
				Android.Graphics.Path.Direction.Cw);

			return output;
		}
	}
}
