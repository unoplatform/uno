#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using System;
using System.Collections.Generic;
using Windows.UI.Xaml.Media.Animation;
using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Windows.UI.Xaml.Controls
{

	[Uno.NotImplemented]
	public partial class TreeView : Control
	{

		[Uno.NotImplemented]
		public TreeViewSelectionMode SelectionMode
		{
			get
			{
				return (TreeViewSelectionMode)this.GetValue(SelectionModeProperty);
			}
			set
			{
				this.SetValue(SelectionModeProperty, value);
			}
		}


		[Uno.NotImplemented]
		public IList<TreeViewNode> RootNodes
		{
			get
			{
				throw new NotImplementedException("The member IList<TreeViewNode> TreeView.RootNodes is not implemented in Uno.");
			}
		}


		[Uno.NotImplemented]
		public IList<TreeViewNode> SelectedNodes
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<TreeViewNode> TreeView.SelectedNodes is not implemented in Uno.");
			}
		}


		[Uno.NotImplemented]
		public object ItemsSource
		{
			get
			{
				return (object)this.GetValue(ItemsSourceProperty);
			}
			set
			{
				this.SetValue(ItemsSourceProperty, value);
			}
		}


		[Uno.NotImplemented]
		public DataTemplateSelector ItemTemplateSelector
		{
			get
			{
				return (DataTemplateSelector)this.GetValue(ItemTemplateSelectorProperty);
			}
			set
			{
				this.SetValue(ItemTemplateSelectorProperty, value);
			}
		}


		[Uno.NotImplemented]
		public DataTemplate ItemTemplate
		{
			get
			{
				return (DataTemplate)this.GetValue(ItemTemplateProperty);
			}
			set
			{
				this.SetValue(ItemTemplateProperty, value);
			}
		}


		[Uno.NotImplemented]
		public TransitionCollection ItemContainerTransitions
		{
			get
			{
				return (TransitionCollection)this.GetValue(ItemContainerTransitionsProperty);
			}
			set
			{
				this.SetValue(ItemContainerTransitionsProperty, value);
			}
		}


		[Uno.NotImplemented]
		public StyleSelector ItemContainerStyleSelector
		{
			get
			{
				return (StyleSelector)this.GetValue(ItemContainerStyleSelectorProperty);
			}
			set
			{
				this.SetValue(ItemContainerStyleSelectorProperty, value);
			}
		}


		[Uno.NotImplemented]
		public Style ItemContainerStyle
		{
			get
			{
				return (Style)this.GetValue(ItemContainerStyleProperty);
			}
			set
			{
				this.SetValue(ItemContainerStyleProperty, value);
			}
		}


		[Uno.NotImplemented]
		public bool CanReorderItems
		{
			get
			{
				return (bool)this.GetValue(CanReorderItemsProperty);
			}
			set
			{
				this.SetValue(CanReorderItemsProperty, value);
			}
		}


		[Uno.NotImplemented]
		public bool CanDragItems
		{
			get
			{
				return (bool)this.GetValue(CanDragItemsProperty);
			}
			set
			{
				this.SetValue(CanDragItemsProperty, value);
			}
		}


		[Uno.NotImplemented]
		public static DependencyProperty SelectionModeProperty { get; } =
		DependencyProperty.Register(
			"SelectionMode", typeof(TreeViewSelectionMode),
			typeof(TreeView),
			new FrameworkPropertyMetadata(default(TreeViewSelectionMode)));


		[Uno.NotImplemented]
		public static DependencyProperty CanDragItemsProperty { get; } =
		DependencyProperty.Register(
			"CanDragItems", typeof(bool),
			typeof(global::Windows.UI.Xaml.Controls.TreeView),
			new FrameworkPropertyMetadata(default(bool)));


		[Uno.NotImplemented]
		public static DependencyProperty CanReorderItemsProperty { get; } =
		DependencyProperty.Register(
			"CanReorderItems", typeof(bool),
			typeof(global::Windows.UI.Xaml.Controls.TreeView),
			new FrameworkPropertyMetadata(default(bool)));


		[Uno.NotImplemented]
		public static DependencyProperty ItemContainerStyleProperty { get; } =
		DependencyProperty.Register(
			"ItemContainerStyle", typeof(Style),
			typeof(TreeView),
			new FrameworkPropertyMetadata(default(Style)));


		[Uno.NotImplemented]
		public static DependencyProperty ItemContainerStyleSelectorProperty { get; } =
		DependencyProperty.Register(
			"ItemContainerStyleSelector", typeof(StyleSelector),
			typeof(TreeView),
			new FrameworkPropertyMetadata(default(StyleSelector)));


		[Uno.NotImplemented]
		public static DependencyProperty ItemContainerTransitionsProperty { get; } =
		DependencyProperty.Register(
			"ItemContainerTransitions", typeof(TransitionCollection),
			typeof(TreeView),
			new FrameworkPropertyMetadata(default(TransitionCollection)));


		[Uno.NotImplemented]
		public static DependencyProperty ItemTemplateProperty { get; } =
		DependencyProperty.Register(
			"ItemTemplate", typeof(DataTemplate),
			typeof(TreeView),
			new FrameworkPropertyMetadata(default(DataTemplate)));


		[Uno.NotImplemented]
		public static DependencyProperty ItemTemplateSelectorProperty { get; } =
		DependencyProperty.Register(
			"ItemTemplateSelector", typeof(DataTemplateSelector),
			typeof(TreeView),
			new FrameworkPropertyMetadata(default(DataTemplateSelector)));


		[Uno.NotImplemented]
		public static DependencyProperty ItemsSourceProperty { get; } =
		DependencyProperty.Register(
			"ItemsSource", typeof(object),
			typeof(TreeView),
			new FrameworkPropertyMetadata(default(object)));


		[Uno.NotImplemented]
		public TreeView() : base()
		{
			ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TreeView", "TreeView.TreeView()");
		}

		// Forced skipping of method Windows.UI.Xaml.Controls.TreeView.TreeView()
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeView.RootNodes.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeView.SelectionMode.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeView.SelectionMode.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeView.SelectedNodes.get

		[Uno.NotImplemented]
		public void Expand(TreeViewNode value)
		{
			ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TreeView", "void TreeView.Expand(TreeViewNode value)");
		}


		[Uno.NotImplemented]
		public void Collapse(TreeViewNode value)
		{
			ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TreeView", "void TreeView.Collapse(TreeViewNode value)");
		}


		[Uno.NotImplemented]
		public void SelectAll()
		{
			ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TreeView", "void TreeView.SelectAll()");
		}

		// Forced skipping of method Windows.UI.Xaml.Controls.TreeView.ItemInvoked.add
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeView.ItemInvoked.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeView.Expanding.add
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeView.Expanding.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeView.Collapsed.add
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeView.Collapsed.remove

		[Uno.NotImplemented]
		public TreeViewNode NodeFromContainer(global::Windows.UI.Xaml.DependencyObject container)
		{
			throw new NotImplementedException("The member TreeViewNode TreeView.NodeFromContainer(DependencyObject container) is not implemented in Uno.");
		}


		[Uno.NotImplemented]
		public DependencyObject ContainerFromNode(global::Windows.UI.Xaml.Controls.TreeViewNode node)
		{
			throw new NotImplementedException("The member DependencyObject TreeView.ContainerFromNode(TreeViewNode node) is not implemented in Uno.");
		}


		[Uno.NotImplemented]
		public object ItemFromContainer(global::Windows.UI.Xaml.DependencyObject container)
		{
			throw new NotImplementedException("The member object TreeView.ItemFromContainer(DependencyObject container) is not implemented in Uno.");
		}


		[Uno.NotImplemented]
		public DependencyObject ContainerFromItem(object item)
		{
			throw new NotImplementedException("The member DependencyObject TreeView.ContainerFromItem(object item) is not implemented in Uno.");
		}

		// Forced skipping of method Windows.UI.Xaml.Controls.TreeView.CanDragItems.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeView.CanDragItems.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeView.CanReorderItems.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeView.CanReorderItems.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeView.ItemTemplate.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeView.ItemTemplate.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeView.ItemTemplateSelector.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeView.ItemTemplateSelector.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeView.ItemContainerStyle.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeView.ItemContainerStyle.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeView.ItemContainerStyleSelector.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeView.ItemContainerStyleSelector.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeView.ItemContainerTransitions.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeView.ItemContainerTransitions.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeView.ItemsSource.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeView.ItemsSource.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeView.DragItemsStarting.add
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeView.DragItemsStarting.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeView.DragItemsCompleted.add
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeView.DragItemsCompleted.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeView.CanDragItemsProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeView.CanReorderItemsProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeView.ItemTemplateProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeView.ItemTemplateSelectorProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeView.ItemContainerStyleProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeView.ItemContainerStyleSelectorProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeView.ItemContainerTransitionsProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeView.ItemsSourceProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeView.SelectionModeProperty.get

		[Uno.NotImplemented]
		public event TypedEventHandler<TreeView, TreeViewCollapsedEventArgs> Collapsed
		{
			[Uno.NotImplemented]
			add
			{
				ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TreeView", "event TypedEventHandler<TreeView, TreeViewCollapsedEventArgs> TreeView.Collapsed");
			}
			[Uno.NotImplemented]
			remove
			{
				ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TreeView", "event TypedEventHandler<TreeView, TreeViewCollapsedEventArgs> TreeView.Collapsed");
			}
		}


		[Uno.NotImplemented]
		public event TypedEventHandler<TreeView, TreeViewExpandingEventArgs> Expanding
		{
			[Uno.NotImplemented]
			add
			{
				ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TreeView", "event TypedEventHandler<TreeView, TreeViewExpandingEventArgs> TreeView.Expanding");
			}
			[Uno.NotImplemented]
			remove
			{
				ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TreeView", "event TypedEventHandler<TreeView, TreeViewExpandingEventArgs> TreeView.Expanding");
			}
		}


		[Uno.NotImplemented]
		public event TypedEventHandler<TreeView, TreeViewItemInvokedEventArgs> ItemInvoked
		{
			[Uno.NotImplemented]
			add
			{
				ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TreeView", "event TypedEventHandler<TreeView, TreeViewItemInvokedEventArgs> TreeView.ItemInvoked");
			}
			[Uno.NotImplemented]
			remove
			{
				ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TreeView", "event TypedEventHandler<TreeView, TreeViewItemInvokedEventArgs> TreeView.ItemInvoked");
			}
		}


		[Uno.NotImplemented]
		public event TypedEventHandler<TreeView, TreeViewDragItemsCompletedEventArgs> DragItemsCompleted
		{
			[Uno.NotImplemented]
			add
			{
				ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TreeView", "event TypedEventHandler<TreeView, TreeViewDragItemsCompletedEventArgs> TreeView.DragItemsCompleted");
			}
			[Uno.NotImplemented]
			remove
			{
				ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TreeView", "event TypedEventHandler<TreeView, TreeViewDragItemsCompletedEventArgs> TreeView.DragItemsCompleted");
			}
		}

		[Uno.NotImplemented]
		public event TypedEventHandler<TreeView, TreeViewDragItemsStartingEventArgs> DragItemsStarting
		{
			[Uno.NotImplemented]
			add
			{
				ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TreeView", "event TypedEventHandler<TreeView, TreeViewDragItemsStartingEventArgs> TreeView.DragItemsStarting");
			}
			[Uno.NotImplemented]
			remove
			{
				ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TreeView", "event TypedEventHandler<TreeView, TreeViewDragItemsStartingEventArgs> TreeView.DragItemsStarting");
			}
		}

	}
}
