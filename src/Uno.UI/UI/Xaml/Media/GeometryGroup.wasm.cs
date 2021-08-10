#nullable enable
using System;
using Uno.UI.DataBinding;
using Windows.Foundation.Collections;
using Windows.UI.Xaml.Wasm;

namespace Windows.UI.Xaml.Media
{
	partial class GeometryGroup
	{
		private SvgElement _svgElement = new SvgElement("g");

		partial void InitPartials()
		{
			this.RegisterDisposablePropertyChangedCallback(OnPropertyChanged);

			Children.VectorChanged += OnGeometriesChanged;

			_svgElement.SetAttribute("fill-rule", "evenodd");
#if DEBUG
			_svgElement.SetAttribute("uno-geometry-type", "GeometryGroup");
#endif
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
				_svgElement.ClearChildren();

				if(args.OldValue is GeometryCollection oldGeometries)
				{
					oldGeometries.VectorChanged -= OnGeometriesChanged;
				}

				if(args.NewValue is GeometryCollection newGeometries)
				{
					newGeometries.VectorChanged += OnGeometriesChanged;

					newGeometries.SetParent(this);

					foreach (var child in Children)
					{
						_svgElement.AddChild(child.GetSvgElement());
					}
				}
			}
		}

		private void OnGeometriesChanged(IObservableVector<Geometry> sender, IVectorChangedEventArgs @event)
		{
			_svgElement.ClearChildren();

			foreach (var child in Children)
			{
				_svgElement.AddChild(child.GetSvgElement());
			}
		}

		internal override SvgElement GetSvgElement() => _svgElement;
	}
}
