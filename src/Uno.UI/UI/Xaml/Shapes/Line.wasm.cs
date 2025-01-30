using System;
using Uno.Extensions;
using Windows.Foundation;
using Microsoft.UI.Xaml.Wasm;
using Uno.UI.Xaml;

namespace Microsoft.UI.Xaml.Shapes
{
	partial class Line
	{
		public Line() : base("line")
		{
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			WindowManagerInterop.SetSvgLineAttributes(_mainSvgElement.HtmlId, X1, X2, Y1, Y2);

			return MeasureAbsoluteShape(availableSize, this);
		}

		/// <inheritdoc />
		protected override Size ArrangeOverride(Size finalSize)
		{
			UpdateRender();
			return ArrangeAbsoluteShape(finalSize, this);
		}

		internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
		{
			base.OnPropertyChanged2(args);

			// invalidate cache key if dp of interests changed
			if (_bboxCacheKey != null && (
				args.Property == X1Property ||
				args.Property == Y1Property ||
				args.Property == X2Property ||
				args.Property == Y2Property
			))
			{
				_bboxCacheKey = null;
			}
		}

		private protected override string GetBBoxCacheKeyImpl() => string.Join(',',
			"line",
			X1.ToStringInvariant(), Y1.ToStringInvariant(),
			X2.ToStringInvariant(), Y2.ToStringInvariant()
		);
	}
}
