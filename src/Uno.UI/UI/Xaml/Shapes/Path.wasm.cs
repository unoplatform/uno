#nullable enable
using Windows.Foundation;
using Windows.UI.Xaml.Wasm;
using Uno.UI.Xaml;

namespace Windows.UI.Xaml.Shapes
{
	partial class Path
	{
		public Path() : base("g")
		{
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			Data?.Invalidate();
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

			var property = args.Property;

			if (property == DataProperty)
			{
				_mainSvgElement.ClearChildren();

				if (Data is { } data)
				{
					_mainSvgElement.AddChild(data.GetSvgElement());
				}
			}
		}

		internal override bool HitTest(Point relativePosition)
		{
			// ContainsPoint acts on SVGGeometryElement, and "g" HTML element is not SVGGeometryElement.
			// So, we override ContainsPoint for Path specifically to operate on the inner child.
			var considerFill = Fill != null;

			// TODO: Verify if this should also consider StrokeThickness (likely it should)
			var considerStroke = Stroke != null;

			if (!considerFill && !considerStroke)
			{
				return false;
			}

			if (_mainSvgElement._children.Count == 0)
			{
				return false;
			}

			if (_mainSvgElement._children.Count == 1)
			{
				return WindowManagerInterop.ContainsPoint(_mainSvgElement._children[0].HtmlId, relativePosition.X, relativePosition.Y, considerFill, considerStroke);
			}

			foreach (var child in _mainSvgElement._children)
			{
				// Unexpected to be hit, but just in case.
				if (WindowManagerInterop.ContainsPoint(child.HtmlId, relativePosition.X, relativePosition.Y, considerFill, considerStroke))
				{
					return true;
				}
			}

			return false;
		}
	}
}
