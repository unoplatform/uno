#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class TreeView : global::Windows.UI.Xaml.Controls.Control
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.TreeViewSelectionMode SelectionMode
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.TreeViewSelectionMode)this.GetValue(SelectionModeProperty);
			}
			set
			{
				this.SetValue(SelectionModeProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.Collections.Generic.IList<global::Windows.UI.Xaml.Controls.TreeViewNode> RootNodes
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<TreeViewNode> TreeView.RootNodes is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.Collections.Generic.IList<global::Windows.UI.Xaml.Controls.TreeViewNode> SelectedNodes
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<TreeViewNode> TreeView.SelectedNodes is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty SelectionModeProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"SelectionMode", typeof(global::Windows.UI.Xaml.Controls.TreeViewSelectionMode), 
			typeof(global::Windows.UI.Xaml.Controls.TreeView), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.TreeViewSelectionMode)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public TreeView() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TreeView", "TreeView.TreeView()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeView.TreeView()
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeView.RootNodes.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeView.SelectionMode.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeView.SelectionMode.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeView.SelectedNodes.get
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void Expand( global::Windows.UI.Xaml.Controls.TreeViewNode value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TreeView", "void TreeView.Expand(TreeViewNode value)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void Collapse( global::Windows.UI.Xaml.Controls.TreeViewNode value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TreeView", "void TreeView.Collapse(TreeViewNode value)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void SelectAll()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TreeView", "void TreeView.SelectAll()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeView.ItemInvoked.add
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeView.ItemInvoked.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeView.Expanding.add
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeView.Expanding.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeView.Collapsed.add
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeView.Collapsed.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeView.SelectionModeProperty.get
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.TreeView, global::Windows.UI.Xaml.Controls.TreeViewCollapsedEventArgs> Collapsed
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TreeView", "event TypedEventHandler<TreeView, TreeViewCollapsedEventArgs> TreeView.Collapsed");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TreeView", "event TypedEventHandler<TreeView, TreeViewCollapsedEventArgs> TreeView.Collapsed");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.TreeView, global::Windows.UI.Xaml.Controls.TreeViewExpandingEventArgs> Expanding
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TreeView", "event TypedEventHandler<TreeView, TreeViewExpandingEventArgs> TreeView.Expanding");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TreeView", "event TypedEventHandler<TreeView, TreeViewExpandingEventArgs> TreeView.Expanding");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.TreeView, global::Windows.UI.Xaml.Controls.TreeViewItemInvokedEventArgs> ItemInvoked
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TreeView", "event TypedEventHandler<TreeView, TreeViewItemInvokedEventArgs> TreeView.ItemInvoked");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TreeView", "event TypedEventHandler<TreeView, TreeViewItemInvokedEventArgs> TreeView.ItemInvoked");
			}
		}
		#endif
	}
}
