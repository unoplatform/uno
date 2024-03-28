using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Input.PointersTests
{
	[Sample("Pointers", "Shapes")]
	public sealed partial class HitTest_Shapes : Page
	{
		public HitTest_Shapes()
		{
			this.InitializeComponent();

			foreach (var elt in GetElements())
			{
				elt.PointerPressed += (snd, e) =>
				{
					e.Handled = true;
					LastPressed.Text = elt.Name;
					LastPressedSrc.Text = (e.OriginalSource as FrameworkElement)?.Name ?? "-unknown-";
				};
				elt.PointerMoved += (snd, e) =>
				{
					e.Handled = true;
					LastHovered.Text = elt.Name;
					LastHoveredSrc.Text = (e.OriginalSource as FrameworkElement)?.Name ?? "-unknown-";
				};
			}
		}

		private IEnumerable<FrameworkElement> GetElements()
		{
			yield return Root;

			yield return RectangleFilled;
			yield return RectangleNotFilled;
			yield return RectangleHidden;
			yield return RectangleHiddenSubElement;

			yield return EllipseFilled;
			yield return EllipseNotFilled;
			yield return EllipseHidden;
			yield return EllipseHiddenSubElement;

			yield return LineFilled;
			yield return LineNotFilled;
			yield return LineHidden;
			yield return LineHiddenSubElement;

			yield return PathFilled;
			yield return PathNotFilled;
			yield return PathHidden;
			yield return PathHiddenSubElement;

			yield return PolygonFilled;
			yield return PolygonNotFilled;
			yield return PolygonHidden;
			yield return PolygonHiddenSubElement;

			yield return PolylineFilled;
			yield return PolylineNotFilled;
			yield return PolylineHidden;
			yield return PolylineHiddenSubElement;
		}
	}
}
