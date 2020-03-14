using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class TreeView
	{
		public bool CanDragItems
		{
			get { return (bool)GetValue(CanDragItemsProperty); }
			set { SetValue(CanDragItemsProperty, value); }
		}

		public bool CanReorderItems
		{
			get { return (bool)GetValue(CanReorderItemsProperty); }
			set { SetValue(CanReorderItemsProperty, value); }
		}

		public Style ItemContainerStyle
		{
			get { return (Style)GetValue(ItemContainerStyleProperty); }
			set { SetValue(ItemContainerStyleProperty, value); }
		}

		public StyleSelector ItemContainerStyleSelector
		{
			get { return (StyleSelector)GetValue(ItemContainerStyleSelectorProperty); }
			set { SetValue(ItemContainerStyleSelectorProperty, value); }
		}

		public TransitionCollection ItemContainerTransitions
		{
			get { return (TransitionCollection)GetValue(ItemContainerTransitionsProperty); }
			set { SetValue(ItemContainerTransitionsProperty, value); }
		}

		public object ItemsSource
		{
			get { return (object)GetValue(ItemsSourceProperty); }
			set { SetValue(ItemsSourceProperty, value); }
		}

		public DataTemplate ItemTemplate
		{
			get { return (DataTemplate)GetValue(ItemTemplateProperty); }
			set { SetValue(ItemTemplateProperty, value); }
		}

		public DataTemplateSelector ItemTemplateSelector
		{
			get { return (DataTemplateSelector)GetValue(ItemTemplateSelectorProperty); }
			set { SetValue(ItemTemplateSelectorProperty, value); }
		}

		public TreeViewSelectionMode SelectionMode
		{
			get { return (TreeViewSelectionMode)GetValue(SelectionModeProperty); }
			set { SetValue(SelectionModeProperty, value); }
		}

		public static readonly DependencyProperty CanDragItemsProperty =
			DependencyProperty.Register(nameof(CanDragItems), typeof(bool), typeof(TreeView), new PropertyMetadata(true));

		public static readonly DependencyProperty CanReorderItemsProperty =
			DependencyProperty.Register(nameof(CanReorderItems), typeof(bool), typeof(TreeView), new PropertyMetadata(true));

		public static readonly DependencyProperty ItemContainerStyleProperty =
			DependencyProperty.Register(nameof(ItemContainerStyle), typeof(Style), typeof(TreeView), new PropertyMetadata(null));

		public static readonly DependencyProperty ItemContainerStyleSelectorProperty =
			DependencyProperty.Register(nameof(ItemContainerStyleSelector), typeof(StyleSelector), typeof(TreeView), new PropertyMetadata(null));

		public static readonly DependencyProperty ItemContainerTransitionsProperty =
			DependencyProperty.Register(nameof(ItemContainerTransitions), typeof(TransitionCollection), typeof(TreeView), new PropertyMetadata(null));

		public static readonly DependencyProperty ItemsSourceProperty =
			DependencyProperty.Register(nameof(ItemsSource), typeof(object), typeof(TreeView), new PropertyMetadata(null));

		public static readonly DependencyProperty ItemTemplateProperty =
			DependencyProperty.Register(nameof(ItemTemplate), typeof(DataTemplate), typeof(TreeView), new PropertyMetadata(null));

		public static readonly DependencyProperty ItemTemplateSelectorProperty =
			DependencyProperty.Register(nameof(ItemTemplateSelector), typeof(DataTemplateSelector), typeof(TreeView), new PropertyMetadata(null));

		public static readonly DependencyProperty SelectionModeProperty =
			DependencyProperty.Register(nameof(SelectionMode), typeof(TreeViewSelectionMode), typeof(TreeView), new PropertyMetadata(TreeViewSelectionMode.Single));
	}
}
