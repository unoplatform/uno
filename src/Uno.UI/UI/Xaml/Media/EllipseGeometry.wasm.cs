#nullable enable
using System;
using Uno.Extensions;
using Uno.UI.DataBinding;
using Windows.UI.Xaml.Wasm;

namespace Windows.UI.Xaml.Media
{
	partial class EllipseGeometry
	{
		private readonly SvgElement _svgElement = new SvgElement("ellipse");

		partial void InitPartials()
		{
			this.RegisterDisposablePropertyChangedCallback(OnPropertyChanged);
#if DEBUG
			_svgElement.SetAttribute("uno-geometry-type", "EllipseGeometry");
#endif
		}

		private void OnPropertyChanged(ManagedWeakReference instance, DependencyProperty property, DependencyPropertyChangedEventArgs args)
		{
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

		internal override SvgElement GetSvgElement() => _svgElement;
	}
}
