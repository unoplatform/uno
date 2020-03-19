using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class TreeViewItem
	{
		public string CollapsedGlyph
		{
			get { return (string)GetValue(CollapsedGlyphProperty); }
			set { SetValue(CollapsedGlyphProperty, value); }
		}

		public string ExpandedGlyph
		{
			get { return (string)GetValue(ExpandedGlyphProperty); }
			set { SetValue(ExpandedGlyphProperty, value); }
		}

		public Brush GlyphBrush
		{
			get { return (Brush)GetValue(GlyphBrushProperty); }
			set { SetValue(GlyphBrushProperty, value); }
		}

		public double GlyphOpacity
		{
			get { return (double)GetValue(GlyphOpacityProperty); }
			set { SetValue(GlyphOpacityProperty, value); }
		}

		public double GlyphSize
		{
			get { return (double)GetValue(GlyphSizeProperty); }
			set { SetValue(GlyphSizeProperty, value); }
		}

		public bool HasUnrealizedChildren
		{
			get { return (bool)GetValue(HasUnrealizedChildrenProperty); }
			set { SetValue(HasUnrealizedChildrenProperty, value); }
		}

		public bool IsExpanded
		{
			get { return (bool)GetValue(IsExpandedProperty); }
			set { SetValue(IsExpandedProperty, value); }
		}

		public object ItemsSource
		{
			get { return (object)GetValue(ItemsSourceProperty); }
			set { SetValue(ItemsSourceProperty, value); }
		}

		public TreeViewItemTemplateSettings TreeViewItemTemplateSettings
		{
			get { return (TreeViewItemTemplateSettings)GetValue(TreeViewItemTemplateSettingsProperty); }
			set { SetValue(TreeViewItemTemplateSettingsProperty, value); }
		}

		public static readonly DependencyProperty CollapsedGlyphProperty =
			DependencyProperty.Register(nameof(CollapsedGlyph), typeof(string), typeof(TreeViewItem), new PropertyMetadata("\uE76C"));

		public static readonly DependencyProperty ExpandedGlyphProperty =
			DependencyProperty.Register(nameof(ExpandedGlyph), typeof(string), typeof(TreeViewItem), new PropertyMetadata("\uE70D"));

		public static readonly DependencyProperty GlyphBrushProperty =
			DependencyProperty.Register(nameof(GlyphBrush), typeof(Brush), typeof(TreeViewItem), new PropertyMetadata(null));

		public static readonly DependencyProperty GlyphOpacityProperty =
			DependencyProperty.Register(nameof(GlyphOpacity), typeof(double), typeof(TreeViewItem), new PropertyMetadata(1.0));

		public static readonly DependencyProperty GlyphSizeProperty =
			DependencyProperty.Register(nameof(GlyphSize), typeof(double), typeof(TreeViewItem), new PropertyMetadata(0));

		public static readonly DependencyProperty HasUnrealizedChildrenProperty =
			DependencyProperty.Register(nameof(HasUnrealizedChildren), typeof(bool), typeof(TreeViewItem), new PropertyMetadata(false, OnHasUnrealizedChildrenPropertyChanged));

		public static readonly DependencyProperty IsExpandedProperty =
			DependencyProperty.Register(nameof(IsExpanded), typeof(bool), typeof(TreeViewItem), new PropertyMetadata(false, OnIsExpandedPropertyChanged));

		public static readonly DependencyProperty ItemsSourceProperty =
			DependencyProperty.Register(nameof(ItemsSource), typeof(object), typeof(TreeViewItem), new PropertyMetadata(null, OnItemsSourcePropertyChanged));

		public static readonly DependencyProperty TreeViewItemTemplateSettingsProperty =
			DependencyProperty.Register(nameof(TreeViewItemTemplateSettings), typeof(TreeViewItemTemplateSettings), typeof(TreeViewItem), new PropertyMetadata(null));

		private static void OnHasUnrealizedChildrenPropertyChanged(
			DependencyObject sender,
			DependencyPropertyChangedEventArgs args)
		{
			var owner = (TreeViewItem)sender;
			owner.OnPropertyChanged(args);
		}

		private static void OnIsExpandedPropertyChanged(
			DependencyObject sender,
			DependencyPropertyChangedEventArgs args)
		{
			var owner = (TreeViewItem)sender;
			owner.OnPropertyChanged(args);
		}

		private static void OnItemsSourcePropertyChanged(
			DependencyObject sender,
			DependencyPropertyChangedEventArgs args)
		{
			var owner = (TreeViewItem)sender;
			owner.OnPropertyChanged(args);
		}
	}
}
