#nullable enable
using System;
using Uno.UI.DataBinding;
using Windows.UI.Xaml.Wasm;

namespace Windows.UI.Xaml.Media
{
	partial class GeometryGroup
	{
		private SvgElement _svgElement = new SvgElement("ellipse");

		partial void InitPartials()
		{
			this.RegisterDisposablePropertyChangedCallback(OnPropertyChanged);

			_svgElement.SetAttribute("fill-rule", "evenodd");
		}

		private void OnPropertyChanged(ManagedWeakReference instance, DependencyProperty property, DependencyPropertyChangedEventArgs args)
		{
			if (property == FillRuleProperty)
			{
				var rule = FillRule switch
				{
					FillRule.EvenOdd => "evenodd",
					FillRule.Nonzero => "nonzero",
					_ => "evenodd"
				};
				_svgElement.SetAttribute("fill-rule", rule);
			}
			else if (property == ChildrenProperty)
			{
				// TODO!!
			}
		}

		internal override SvgElement GetSvgElement() => _svgElement;
	}
}
