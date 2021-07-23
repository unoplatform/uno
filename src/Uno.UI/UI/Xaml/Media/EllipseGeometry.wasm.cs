#nullable enable
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
		}

		private void OnPropertyChanged(ManagedWeakReference instance, DependencyProperty property, DependencyPropertyChangedEventArgs args)
		{
			if(property == CenterProperty)
			{
				var center = Center;

				_svgElement.SetAttribute(
					("cx", center.X.ToStringInvariant()),
					("cy", center.Y.ToStringInvariant()));
			}
			else if (property == RadiusXProperty)
			{
				_svgElement.SetAttribute("rx", RadiusX.ToStringInvariant());
			}
			else if (property == RadiusYProperty)
			{
				_svgElement.SetAttribute("ry", RadiusY.ToStringInvariant());
			}
		}

		internal override SvgElement GetSvgElement() => _svgElement;
	}
}
