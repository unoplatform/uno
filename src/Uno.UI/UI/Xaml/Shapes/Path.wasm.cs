#nullable enable
using Windows.Foundation;
using Microsoft.UI.Xaml.Wasm;

namespace Microsoft.UI.Xaml.Shapes
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
	}
}
