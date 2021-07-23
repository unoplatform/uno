#nullable enable
using Uno.Extensions;
using Uno.UI.DataBinding;
using Windows.UI.Xaml.Wasm;

namespace Windows.UI.Xaml.Media
{
	partial class LineGeometry
	{
		private readonly SvgElement _svgElement = new SvgElement("line");

		partial void InitPartials()
		{
			this.RegisterDisposablePropertyChangedCallback(OnPropertyChanged);
		}

		private void OnPropertyChanged(ManagedWeakReference instance, DependencyProperty property, DependencyPropertyChangedEventArgs args)
		{
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

		internal override SvgElement GetSvgElement() => _svgElement;
	}
}
