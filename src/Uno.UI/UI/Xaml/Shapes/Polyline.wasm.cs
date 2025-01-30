using System;
using System.Linq;
using Windows.Foundation;
using Microsoft.UI.Xaml.Wasm;
using Uno;
using Uno.Extensions;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;
using Uno.UI.Xaml;

namespace Microsoft.UI.Xaml.Shapes
{
	partial class Polyline
	{
		protected override Size MeasureOverride(Size availableSize)
		{
			WindowManagerInterop.SetSvgPolyPoints(_mainSvgElement.HtmlId, Points?.Flatten());

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
				? ("polyline:" + string.Join(',', points.Flatten()))
				: null;
	}
}
