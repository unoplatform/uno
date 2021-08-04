#nullable enable
using Uno.Extensions;
using Uno.UI.DataBinding;
using Windows.UI.Xaml.Wasm;

namespace Windows.UI.Xaml.Media
{
	partial class RectangleGeometry
	{
		private SvgElement? _svgElement;

		partial void InitPartials()
		{
			this.RegisterDisposablePropertyChangedCallback(OnPropertyChanged);
		}

		private void OnPropertyChanged(ManagedWeakReference instance, DependencyProperty property, DependencyPropertyChangedEventArgs args)
		{
			if (property == RectProperty)
			{
				UpdateSvg();
			}
		}

		private void UpdateSvg()
		{
			var rect = Rect;

			_svgElement?.SetAttribute(
				("x", rect.X.ToStringInvariant()),
				("y", rect.Y.ToStringInvariant()),
				("width", rect.Width.ToStringInvariant()),
				("height", rect.Height.ToStringInvariant()));
		}

		internal override SvgElement GetSvgElement()
		{
			if (_svgElement == null)
			{
				_svgElement = new SvgElement("rect");
#if DEBUG
				_svgElement.SetAttribute("uno-geometry-type", "RectangleGeometry");
#endif
				UpdateSvg();
			}
			return _svgElement;
		}
	}
}
