namespace Windows.UI.Xaml.Controls
{
	public partial class TreeViewItemTemplateSettings : DependencyObject
	{
		public Visibility CollapsedGlyphVisibility
		{
			get { return (Visibility)GetValue(CollapsedGlyphVisibilityProperty); }
			set { SetValue(CollapsedGlyphVisibilityProperty, value); }
		}

		public int DragItemsCount
		{
			get { return (int)GetValue(DragItemsCountProperty); }
			set { SetValue(DragItemsCountProperty, value); }
		}

		public Visibility ExpandedGlyphVisibility
		{
			get { return (Visibility)GetValue(ExpandedGlyphVisibilityProperty); }
			set { SetValue(ExpandedGlyphVisibilityProperty, value); }
		}

		public Thickness Indentation
		{
			get { return (Thickness)GetValue(IndentationProperty); }
			set { SetValue(IndentationProperty, value); }
		}

		public static readonly DependencyProperty CollapsedGlyphVisibilityProperty =
			DependencyProperty.Register(nameof(CollapsedGlyphVisibility), typeof(Visibility), typeof(TreeViewItemTemplateSettings), new PropertyMetadata(Visibility.Collapsed));

		public static readonly DependencyProperty DragItemsCountProperty =
			DependencyProperty.Register(nameof(DragItemsCount), typeof(int), typeof(TreeViewItemTemplateSettings), new PropertyMetadata(0));

		public static readonly DependencyProperty ExpandedGlyphVisibilityProperty =
			DependencyProperty.Register(nameof(ExpandedGlyphVisibility), typeof(Visibility), typeof(TreeViewItemTemplateSettings), new PropertyMetadata(Visibility.Collapsed));

		public static readonly DependencyProperty IndentationProperty =
			DependencyProperty.Register(nameof(Indentation), typeof(Thickness), typeof(TreeViewItemTemplateSettings), new PropertyMetadata(default(Thickness)));
	}
}
