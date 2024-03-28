using Uno.UI;
using Windows.UI.Xaml.Markup;
using Windows.Foundation;

using Rect = Windows.Foundation.Rect;

namespace Windows.UI.Xaml.Media
{
	[ContentProperty(Name = nameof(Children))]
	public partial class GeometryGroup : Geometry
	{
		public GeometryGroup()
		{
			Children = new GeometryCollection();

			InitPartials();
		}

		partial void InitPartials();

		#region FillRule

		public FillRule FillRule
		{
			get => (FillRule)this.GetValue(FillRuleProperty);
			set => this.SetValue(FillRuleProperty, value);
		}

		public static DependencyProperty FillRuleProperty { get; } =
			DependencyProperty.Register(
				nameof(FillRule),
				typeof(FillRule),
				typeof(GeometryGroup),
				new FrameworkPropertyMetadata(
					defaultValue: FillRule.EvenOdd,
					options: FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure,
					propertyChangedCallback: (o, v) => OnPropertyChanged(o, v)
				)
			);

		#endregion

		static partial void OnPropertyChanged(DependencyObject dependencyObject, object newValue);

		#region Children

		public GeometryCollection Children
		{
			get => (GeometryCollection)this.GetValue(ChildrenProperty);
			set => this.SetValue(ChildrenProperty, value);
		}

		public static DependencyProperty ChildrenProperty { get; } =
			DependencyProperty.Register(
				nameof(Children),
				typeof(GeometryCollection),
				typeof(GeometryGroup),
				new FrameworkPropertyMetadata(
					defaultValue: null,
					options: FrameworkPropertyMetadataOptions.ValueInheritsDataContext | FrameworkPropertyMetadataOptions.LogicalChild | FrameworkPropertyMetadataOptions.AffectsMeasure
				)
			);

		#endregion

		private protected override Rect ComputeBounds()
		{
			Rect? bounds = default;

			foreach (var geometry in Children)
			{
				if (bounds is { } b)
				{
					bounds = b.UnionWith(geometry.Bounds);
				}
				else
				{
					bounds = geometry.Bounds;
				}
			}

			if (bounds is { } result)
			{
				if (Transform is { } t)
				{
					return t.TransformBounds(result);
				}

				return result;
			}

			return default;
		}
	}
}
