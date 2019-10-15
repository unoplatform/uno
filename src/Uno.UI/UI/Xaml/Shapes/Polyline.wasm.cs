using System;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml.Wasm;
using Uno;
using Uno.Extensions;
using Windows.UI.Xaml.Media;
using System.Collections.Generic;

namespace Windows.UI.Xaml.Shapes
{
	partial class Polyline : ArbitraryShapeBase
	{
		#region Points

		public PointCollection Points
		{
			get { return (PointCollection)GetValue(PointsProperty); }
			set { SetValue(PointsProperty, value); }
		}

		public static global::Windows.UI.Xaml.DependencyProperty PointsProperty { get; } =
			DependencyProperty.Register(
				"Points", typeof(global::Windows.UI.Xaml.Media.PointCollection),
				typeof(global::Windows.UI.Xaml.Shapes.Polyline),
				new FrameworkPropertyMetadata(
					defaultValue: default(global::Windows.UI.Xaml.Media.PointCollection),
					propertyChangedCallback: (s, e) => ((Polyline)s).OnPointsChanged()
				)
			);

		partial void OnPointsChanged();

		#endregion

		private readonly SvgElement _polyline = new SvgElement("polyline");

		public Polyline()
		{
			SvgChildren.Add(_polyline);

			InitCommonShapeProperties();
		}

		protected override SvgElement GetMainSvgElement()
		{
			return _polyline;
		}

		partial void OnPointsChanged()
		{
			var points = string.Join(" ", Points.Select(p => $"{p.X.ToStringInvariant()},{p.Y.ToStringInvariant()}"));
			_polyline.SetAttribute("points", points);
		}

		protected internal override IEnumerable<object> GetShapeParameters()
		{
			yield return Points?.ToArray();

			foreach (var p in base.GetShapeParameters())
			{
				yield return p;
			}
		}
	}
}
