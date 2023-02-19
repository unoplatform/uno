#nullable enable
using System;
using System.Globalization;
using System.Text;
using Uno.UI.DataBinding;
using Windows.Foundation.Collections;
using Windows.UI.Xaml.Wasm;

namespace Windows.UI.Xaml.Media
{
	partial class GeometryGroup
	{
		private SvgElement? _svgElement;

		partial void InitPartials()
		{
			_ = this.RegisterDisposablePropertyChangedCallback(OnPropertyChanged);

			Children.VectorChanged += OnGeometriesChanged;
		}

		private void OnPropertyChanged(ManagedWeakReference? instance, DependencyProperty property, DependencyPropertyChangedEventArgs? args)
		{
			if (_svgElement == null)
			{
				return;
			}

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

				if (args?.OldValue is GeometryCollection oldGeometries)
				{
					oldGeometries.VectorChanged -= OnGeometriesChanged;
				}

				if ((args?.NewValue ?? Children) is GeometryCollection newGeometries)
				{
					newGeometries.VectorChanged += OnGeometriesChanged;

					newGeometries.SetParent(this);

					foreach (var child in newGeometries)
					{
						_svgElement.AddChild(child.GetSvgElement());
					}
				}
			}
		}

		internal override void Invalidate()
		{
			var data = ToPathData().ToString(null, CultureInfo.InvariantCulture);
			GetSvgElement().SetAttribute("d", data);
		}

		private void OnGeometriesChanged(IObservableVector<Geometry> sender, IVectorChangedEventArgs e)
		{
			Invalidate();
		}

		internal override SvgElement GetSvgElement()
		{
			if (_svgElement == null)
			{
				_svgElement = new SvgElement("path");
#if DEBUG
				_svgElement.SetAttribute("uno-geometry-type", "GeometryGroup");
#endif

				OnPropertyChanged(null, FillRuleProperty, null);
				OnPropertyChanged(null, ChildrenProperty, null);
			}

			return _svgElement;
		}

		private CompositeFormattable _compositeFormattable;

		internal override IFormattable ToPathData()
		{
			return _compositeFormattable ??= new CompositeFormattable(this);
		}

		private class CompositeFormattable : IFormattable
		{
			private readonly GeometryGroup _owner;

			public CompositeFormattable(GeometryGroup owner)
			{
				_owner = owner;
			}

			public string ToString(string? format, IFormatProvider? formatProvider)
			{
				var sb = new StringBuilder();

				foreach (var child in _owner.Children)
				{
					var childFormattable = child.ToPathData();
					sb.Append(childFormattable.ToString(format, formatProvider));
				}

				return sb.ToString();
			}
		}
	}
}
