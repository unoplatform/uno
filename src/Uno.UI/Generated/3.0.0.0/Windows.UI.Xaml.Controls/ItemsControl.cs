#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class ItemsControl : global::Windows.UI.Xaml.Controls.Control,global::Windows.UI.Xaml.Controls.IItemContainerMapping
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  object ItemsSource
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
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.ItemsPanelTemplate ItemsPanel
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.ItemsPanelTemplate)this.GetValue(ItemsPanelProperty);
			}
			set
			{
				this.SetValue(ItemsPanelProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.DataTemplateSelector ItemTemplateSelector
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.DataTemplateSelector)this.GetValue(ItemTemplateSelectorProperty);
			}
			set
			{
				this.SetValue(ItemTemplateSelectorProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Media.Animation.TransitionCollection ItemContainerTransitions
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Animation.TransitionCollection)this.GetValue(ItemContainerTransitionsProperty);
			}
			set
			{
				this.SetValue(ItemContainerTransitionsProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Style ItemContainerStyle
		{
			get
			{
				return (global::Windows.UI.Xaml.Style)this.GetValue(ItemContainerStyleProperty);
			}
			set
			{
				this.SetValue(ItemContainerStyleProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.DataTemplate ItemTemplate
		{
			get
			{
				return (global::Windows.UI.Xaml.DataTemplate)this.GetValue(ItemTemplateProperty);
			}
			set
			{
				this.SetValue(ItemTemplateProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.GroupStyleSelector GroupStyleSelector
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.GroupStyleSelector)this.GetValue(GroupStyleSelectorProperty);
			}
			set
			{
				this.SetValue(GroupStyleSelectorProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.StyleSelector ItemContainerStyleSelector
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.StyleSelector)this.GetValue(ItemContainerStyleSelectorProperty);
			}
			set
			{
				this.SetValue(ItemContainerStyleSelectorProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  string DisplayMemberPath
		{
			get
			{
				return (string)this.GetValue(DisplayMemberPathProperty);
			}
			set
			{
				this.SetValue(DisplayMemberPathProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.ItemContainerGenerator ItemContainerGenerator
		{
			get
			{
				throw new global::System.NotImplementedException("The member ItemContainerGenerator ItemsControl.ItemContainerGenerator is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.Collections.IObservableVector<global::Windows.UI.Xaml.Controls.GroupStyle> GroupStyle
		{
			get
			{
				throw new global::System.NotImplementedException("The member IObservableVector<GroupStyle> ItemsControl.GroupStyle is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.ItemCollection Items
		{
			get
			{
				throw new global::System.NotImplementedException("The member ItemCollection ItemsControl.Items is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool IsGrouping
		{
			get
			{
				return (bool)this.GetValue(IsGroupingProperty);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.Panel ItemsPanelRoot
		{
			get
			{
				throw new global::System.NotImplementedException("The member Panel ItemsControl.ItemsPanelRoot is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty DisplayMemberPathProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"DisplayMemberPath", typeof(string), 
			typeof(global::Windows.UI.Xaml.Controls.ItemsControl), 
			new FrameworkPropertyMetadata(default(string)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty GroupStyleSelectorProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"GroupStyleSelector", typeof(global::Windows.UI.Xaml.Controls.GroupStyleSelector), 
			typeof(global::Windows.UI.Xaml.Controls.ItemsControl), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.GroupStyleSelector)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsGroupingProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsGrouping", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.ItemsControl), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ItemContainerStyleProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"ItemContainerStyle", typeof(global::Windows.UI.Xaml.Style), 
			typeof(global::Windows.UI.Xaml.Controls.ItemsControl), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Style)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ItemContainerStyleSelectorProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"ItemContainerStyleSelector", typeof(global::Windows.UI.Xaml.Controls.StyleSelector), 
			typeof(global::Windows.UI.Xaml.Controls.ItemsControl), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.StyleSelector)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ItemContainerTransitionsProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"ItemContainerTransitions", typeof(global::Windows.UI.Xaml.Media.Animation.TransitionCollection), 
			typeof(global::Windows.UI.Xaml.Controls.ItemsControl), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Animation.TransitionCollection)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ItemTemplateProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"ItemTemplate", typeof(global::Windows.UI.Xaml.DataTemplate), 
			typeof(global::Windows.UI.Xaml.Controls.ItemsControl), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.DataTemplate)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ItemTemplateSelectorProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"ItemTemplateSelector", typeof(global::Windows.UI.Xaml.Controls.DataTemplateSelector), 
			typeof(global::Windows.UI.Xaml.Controls.ItemsControl), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.DataTemplateSelector)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ItemsPanelProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"ItemsPanel", typeof(global::Windows.UI.Xaml.Controls.ItemsPanelTemplate), 
			typeof(global::Windows.UI.Xaml.Controls.ItemsControl), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.ItemsPanelTemplate)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ItemsSourceProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"ItemsSource", typeof(object), 
			typeof(global::Windows.UI.Xaml.Controls.ItemsControl), 
			new FrameworkPropertyMetadata(default(object)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public ItemsControl() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ItemsControl", "ItemsControl.ItemsControl()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsControl.ItemsControl()
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsControl.ItemsSource.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsControl.ItemsSource.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsControl.Items.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsControl.ItemTemplate.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsControl.ItemTemplate.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsControl.ItemTemplateSelector.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsControl.ItemTemplateSelector.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsControl.ItemsPanel.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsControl.ItemsPanel.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsControl.DisplayMemberPath.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsControl.DisplayMemberPath.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsControl.ItemContainerStyle.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsControl.ItemContainerStyle.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsControl.ItemContainerStyleSelector.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsControl.ItemContainerStyleSelector.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsControl.ItemContainerGenerator.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsControl.ItemContainerTransitions.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsControl.ItemContainerTransitions.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsControl.GroupStyle.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsControl.GroupStyleSelector.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsControl.GroupStyleSelector.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsControl.IsGrouping.get
		#if false || false || false || false
		[global::Uno.NotImplemented]
		protected virtual bool IsItemItsOwnContainerOverride( object item)
		{
			throw new global::System.NotImplementedException("The member bool ItemsControl.IsItemItsOwnContainerOverride(object item) is not implemented in Uno.");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		protected virtual global::Windows.UI.Xaml.DependencyObject GetContainerForItemOverride()
		{
			throw new global::System.NotImplementedException("The member DependencyObject ItemsControl.GetContainerForItemOverride() is not implemented in Uno.");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		protected virtual void ClearContainerForItemOverride( global::Windows.UI.Xaml.DependencyObject element,  object item)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ItemsControl", "void ItemsControl.ClearContainerForItemOverride(DependencyObject element, object item)");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		protected virtual void PrepareContainerForItemOverride( global::Windows.UI.Xaml.DependencyObject element,  object item)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ItemsControl", "void ItemsControl.PrepareContainerForItemOverride(DependencyObject element, object item)");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		protected virtual void OnItemsChanged( object e)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ItemsControl", "void ItemsControl.OnItemsChanged(object e)");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		protected virtual void OnItemContainerStyleChanged( global::Windows.UI.Xaml.Style oldItemContainerStyle,  global::Windows.UI.Xaml.Style newItemContainerStyle)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ItemsControl", "void ItemsControl.OnItemContainerStyleChanged(Style oldItemContainerStyle, Style newItemContainerStyle)");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		protected virtual void OnItemContainerStyleSelectorChanged( global::Windows.UI.Xaml.Controls.StyleSelector oldItemContainerStyleSelector,  global::Windows.UI.Xaml.Controls.StyleSelector newItemContainerStyleSelector)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ItemsControl", "void ItemsControl.OnItemContainerStyleSelectorChanged(StyleSelector oldItemContainerStyleSelector, StyleSelector newItemContainerStyleSelector)");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		protected virtual void OnItemTemplateChanged( global::Windows.UI.Xaml.DataTemplate oldItemTemplate,  global::Windows.UI.Xaml.DataTemplate newItemTemplate)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ItemsControl", "void ItemsControl.OnItemTemplateChanged(DataTemplate oldItemTemplate, DataTemplate newItemTemplate)");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		protected virtual void OnItemTemplateSelectorChanged( global::Windows.UI.Xaml.Controls.DataTemplateSelector oldItemTemplateSelector,  global::Windows.UI.Xaml.Controls.DataTemplateSelector newItemTemplateSelector)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ItemsControl", "void ItemsControl.OnItemTemplateSelectorChanged(DataTemplateSelector oldItemTemplateSelector, DataTemplateSelector newItemTemplateSelector)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		protected virtual void OnGroupStyleSelectorChanged( global::Windows.UI.Xaml.Controls.GroupStyleSelector oldGroupStyleSelector,  global::Windows.UI.Xaml.Controls.GroupStyleSelector newGroupStyleSelector)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ItemsControl", "void ItemsControl.OnGroupStyleSelectorChanged(GroupStyleSelector oldGroupStyleSelector, GroupStyleSelector newGroupStyleSelector)");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsControl.ItemsPanelRoot.get
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  object ItemFromContainer( global::Windows.UI.Xaml.DependencyObject container)
		{
			throw new global::System.NotImplementedException("The member object ItemsControl.ItemFromContainer(DependencyObject container) is not implemented in Uno.");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.DependencyObject ContainerFromItem( object item)
		{
			throw new global::System.NotImplementedException("The member DependencyObject ItemsControl.ContainerFromItem(object item) is not implemented in Uno.");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  int IndexFromContainer( global::Windows.UI.Xaml.DependencyObject container)
		{
			throw new global::System.NotImplementedException("The member int ItemsControl.IndexFromContainer(DependencyObject container) is not implemented in Uno.");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.DependencyObject ContainerFromIndex( int index)
		{
			throw new global::System.NotImplementedException("The member DependencyObject ItemsControl.ContainerFromIndex(int index) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.DependencyObject GroupHeaderContainerFromItemContainer( global::Windows.UI.Xaml.DependencyObject itemContainer)
		{
			throw new global::System.NotImplementedException("The member DependencyObject ItemsControl.GroupHeaderContainerFromItemContainer(DependencyObject itemContainer) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsControl.ItemsSourceProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsControl.ItemTemplateProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsControl.ItemTemplateSelectorProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsControl.ItemsPanelProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsControl.DisplayMemberPathProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsControl.ItemContainerStyleProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsControl.ItemContainerStyleSelectorProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsControl.ItemContainerTransitionsProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsControl.GroupStyleSelectorProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsControl.IsGroupingProperty.get
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.Controls.ItemsControl GetItemsOwner( global::Windows.UI.Xaml.DependencyObject element)
		{
			throw new global::System.NotImplementedException("The member ItemsControl ItemsControl.GetItemsOwner(DependencyObject element) is not implemented in Uno.");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.Controls.ItemsControl ItemsControlFromItemContainer( global::Windows.UI.Xaml.DependencyObject container)
		{
			throw new global::System.NotImplementedException("The member ItemsControl ItemsControl.ItemsControlFromItemContainer(DependencyObject container) is not implemented in Uno.");
		}
		#endif
		// Processing: Windows.UI.Xaml.Controls.IItemContainerMapping
	}
}
