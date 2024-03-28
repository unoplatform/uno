using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Windows.UI.Xaml.Media.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.Foundation;
using Windows.UI.Xaml.Media;

namespace Uno.UI.RuntimeTests.Helpers
{
	/// <summary>
	/// Class that make it possible to find a specific child position in a parent FrameworkElement
	/// </summary>
	public class RelativeCoords// renamed to RelativeCoords
	{
		private readonly GeneralTransform _transform;
		private readonly double _width, _height;

		private RelativeCoords(GeneralTransform transform, double width, double height)
		{
			this._transform = transform;
			this._width = width;
			this._height = height;
		}

		public static RelativeCoords From(FrameworkElement parent, FrameworkElement child)
		{
			return new RelativeCoords(child.TransformToVisual(parent), child.Width, child.Height);
		}

		// note: the transformed (0, 0) position is based the top-left.
		public float X => (float)_transform.TransformPoint(new Windows.Foundation.Point(0, 0)).X;

		public float Y => (float)_transform.TransformPoint(new Windows.Foundation.Point(0, 0)).Y;

		public float Left => X;

		public float Top => Y;

		public float Right => (float)_transform.TransformPoint(new Windows.Foundation.Point(_width, 0)).X;

		public float Bottom => (float)_transform.TransformPoint(new Windows.Foundation.Point(0, _height)).Y;

		public float CenterX => (float)_transform.TransformPoint(new Windows.Foundation.Point(_width / 2, 0)).X;

		public float CenterY => (float)_transform.TransformPoint(new Windows.Foundation.Point(0, _height / 2)).Y;

		public float Width => Right - Left;

		public float Height => Bottom - Top;
	}
}
