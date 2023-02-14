#nullable enable
using Microsoft.UI.Xaml.Wasm;

namespace Microsoft.UI.Xaml.Shapes
{
	partial class Path
	{
		private readonly SvgElement _root = new SvgElement("g");

		public Path()
		{
			SvgChildren.Add(_root);

			InitCommonShapeProperties();
		}

		protected override SvgElement GetMainSvgElement() => _root;

		partial void OnDataChanged() => InvalidateMeasure();

		internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
		{
			base.OnPropertyChanged2(args);

			var property = args.Property;

			if (property == DataProperty)
			{
				_root.ClearChildren();

				if (Data is { } data)
				{
					_root.AddChild(data.GetSvgElement());
				}
			}
		}

		protected override void InvalidateShape() => Data?.Invalidate();
	}
}
