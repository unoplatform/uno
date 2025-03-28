#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Extensions;
using Uno.UI.DataBinding;
using Windows.Foundation.Collections;
using Windows.UI.Xaml.Wasm;

namespace Windows.UI.Xaml.Media
{
	partial class PathGeometry
	{
		private readonly SvgElement _svgElement = new SvgElement("path");

		partial void InitPartials()
		{
			this.RegisterDisposablePropertyChangedCallback(OnPropertyChanged);

			_svgElement.SetAttribute("fill-rule", "evenodd");
#if DEBUG
			_svgElement.SetAttribute("uno-geometry-type", "PathGeometry");
#endif
		}

		private void OnPropertyChanged(ManagedWeakReference instance, DependencyProperty property, DependencyPropertyChangedEventArgs args)
		{
			if (property == FiguresProperty)
			{
				if (args.OldValue is PathFigureCollection oldFigures)
				{
					oldFigures.VectorChanged -= OnFiguresVectorChanged;
				}

				if (args.NewValue is PathFigureCollection newFigures)
				{
					newFigures.VectorChanged += OnFiguresVectorChanged;
				}

				_svgElement.InvalidateMeasure();
			}
			else if (property == FillRuleProperty)
			{
				var rule = FillRule switch
				{
					FillRule.EvenOdd => "evenodd",
					FillRule.Nonzero => "nonzero",
					_ => "evenodd"
				};
				_svgElement.SetAttribute("fill-rule", rule);
			}
		}

		internal override void Invalidate() => ComputeAndSetPathData();

		private void OnFiguresVectorChanged(IObservableVector<PathFigure> sender, IVectorChangedEventArgs? args)
		{
			_svgElement.InvalidateMeasure();
		}

		private void ComputeAndSetPathData()
		{
			var data = ToStreamGeometry(Figures);
			_svgElement.SetAttribute("d", data);
		}

		/// <summary>
		/// Transform the figures collection into a SVG Path according to :
		/// https://developer.mozilla.org/en-US/docs/Web/SVG/Attribute/d
		/// </summary>
		private static string ToStreamGeometry(PathFigureCollection figures)
		{
			if (figures == null)
			{
				return "";
			}

			IEnumerable<IFormattable> GenerateDataParts()
			{
				foreach (var figure in figures)
				{
					// https://developer.mozilla.org/en-US/docs/Web/SVG/Attribute/d#moveto_path_commands
					yield return $"M {figure.StartPoint.X},{figure.StartPoint.Y}";

					foreach (var segment in figure.Segments)
					{
						foreach (var p in segment.ToDataStream())
						{
							yield return p;
						}
					}

					if (figure.IsClosed)
					{
						// https://developer.mozilla.org/en-US/docs/Web/SVG/Attribute/d#closepath
						yield return $"Z";
					}
				}
			}

			return string.Join(" ", GenerateDataParts().Select(FormattableExtensions.ToStringInvariant));
		}

		internal override SvgElement GetSvgElement() => _svgElement;

		internal override IFormattable ToPathData() => $"{ToStreamGeometry(Figures)}";
	}
}
