#nullable enable
using System;
using System.Collections.Generic;
using Uno.Extensions;
using Uno.UI.DataBinding;
using Windows.UI.Xaml.Wasm;

namespace Windows.UI.Xaml.Media
{
	partial class LineGeometry
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

			if (property == StartPointProperty)
			{
				var point = StartPoint;
				_svgElement.SetAttribute(
					("x1", point.X.ToStringInvariant()),
					("y1", point.Y.ToStringInvariant()));
			}
			else if (property == EndPointProperty)
			{
				var point = EndPoint;
				_svgElement.SetAttribute(
					("x2", point.X.ToStringInvariant()),
					("y2", point.Y.ToStringInvariant()));
			}
		}

		internal override SvgElement GetSvgElement()
		{
			if (_svgElement == null)
			{
				_svgElement = new SvgElement("line");
#if DEBUG
				_svgElement.SetAttribute("uno-geometry-type", "LineGeometry");
#endif

				OnPropertyChanged(null, StartPointProperty, null);
				OnPropertyChanged(null, EndPointProperty, null);
			}

			return _svgElement;
		}

		internal override IFormattable ToPathData()
		{
			var p1 = StartPoint;
			var p2 = EndPoint;

			return $"M {p1.X},{p1.Y} L {p2.X}, {p2.Y} Z";
		}
	}
}
