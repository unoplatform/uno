using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Windows.Foundation;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Wasm;
using static System.FormattableString;

namespace Windows.UI.Xaml.Shapes
{
	partial class Path
	{
		private readonly SvgElement _root = new SvgElement("g");

		public Path()
		{
			SvgChildren.Add(_root);

			InitCommonShapeProperties();
		}

		protected override SvgElement GetMainSvgElement()
		{
			return _root;
		}

		partial void OnDataChanged() => InvalidateMeasure();

		internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
		{
			var property = args.Property;

			if(property == DataProperty)
			{
				_root.ClearChildren();
				_root.AddChild(Data.GetSvgElement());
			}
		}

		protected override void InvalidateShape()
		{
			Data?.Invalidate();
		}
	}
}
