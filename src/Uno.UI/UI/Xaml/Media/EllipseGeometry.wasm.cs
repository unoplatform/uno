#nullable enable
using System;
using System.Collections.Generic;
using Uno.Extensions;
using Uno.UI.DataBinding;
using Windows.UI.Xaml.Wasm;

namespace Windows.UI.Xaml.Media
{
	partial class EllipseGeometry
	{
		private SvgElement? _svgElement;

		partial void InitPartials()
		{
			this.RegisterDisposablePropertyChangedCallback(OnPropertyChanged);
		}

		private void OnPropertyChanged(ManagedWeakReference? instance, DependencyProperty property, DependencyPropertyChangedEventArgs? args)
		{
			if (_svgElement == null)
			{
				return;
			}

			if (property == CenterProperty)
			{
				var center = Center;

				_svgElement.SetAttribute(
					("cx", center.X.ToStringInvariant()),
					("cy", center.Y.ToStringInvariant()));
				_svgElement.InvalidateMeasure();
			}
			else if (property == RadiusXProperty)
			{
				_svgElement.SetAttribute("rx", RadiusX.ToStringInvariant());
				_svgElement.InvalidateMeasure();
			}
			else if (property == RadiusYProperty)
			{
				_svgElement.SetAttribute("ry", RadiusY.ToStringInvariant());
				_svgElement.InvalidateMeasure();
			}
		}

		internal override SvgElement GetSvgElement()
		{
			if (_svgElement == null)
			{
				_svgElement = new SvgElement("ellipse");
#if DEBUG
				_svgElement.SetAttribute("uno-geometry-type", "EllipseGeometry");
#endif

				OnPropertyChanged(null, CenterProperty, null);
				OnPropertyChanged(null, RadiusXProperty, null);
				OnPropertyChanged(null, RadiusYProperty, null);
			}

			return _svgElement;
		}

		internal override IFormattable ToPathData()
		{
			var cx = Center.X;
			var cy = Center.Y;
			var rx = RadiusX;
			var ry = RadiusY;

			return $"M{cx},{cy - ry} A{rx},{ry} 0 0 0 {cx},{cy + ry} A{rx},{ry} 0 0 0 {cx},{cy - ry} Z";
		}
	}
}
