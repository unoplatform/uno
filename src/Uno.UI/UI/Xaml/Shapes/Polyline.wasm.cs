using System;
using System.Linq;
using Windows.Foundation;
using Microsoft.UI.Xaml.Wasm;
using Uno;
using Uno.Extensions;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Shapes
{
	partial class Polyline
	{
		protected override Size MeasureOverride(Size availableSize)
		{
			var points = Points;
			if (points == null)
			{
				_mainSvgElement.RemoveAttribute("points");
			}
			else
			{
				_mainSvgElement.SetAttribute("points", points.ToCssString());
			}

			return MeasureAbsoluteShape(availableSize, this);
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			UpdateRender();
			return ArrangeAbsoluteShape(finalSize, this);
		}

		internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
		{
			base.OnPropertyChanged2(args);

			if (_bboxCacheKey != null && (
				args.Property == PointsProperty
			))
			{
				_bboxCacheKey = null;
			}
		}

		private protected override string GetBBoxCacheKeyImpl() =>
			Points is { } points
				? ("polygone:" + points.ToCssString())
				: null;
	}
}
