#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class ListViewBase : global::Windows.UI.Xaml.Controls.ISemanticZoomInformation
	{
		// Skipping already declared property SelectionMode
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsSwipeEnabled
		{
			get
			{
				return (bool)this.GetValue(IsSwipeEnabledProperty);
			}
			set
			{
				this.SetValue(IsSwipeEnabledProperty, value);
			}
		}
		#endif
		// Skipping already declared property IsItemClickEnabled
		// Skipping already declared property IncrementalLoadingTrigger
		// Skipping already declared property IncrementalLoadingThreshold
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Media.Animation.TransitionCollection HeaderTransitions
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Animation.TransitionCollection)this.GetValue(HeaderTransitionsProperty);
			}
			set
			{
				this.SetValue(HeaderTransitionsProperty, value);
			}
		}
		#endif
		// Skipping already declared property HeaderTemplate
		// Skipping already declared property Header
		// Skipping already declared property DataFetchSize
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool CanReorderItems
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
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool CanDragItems
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
		#endif
		// Skipping already declared property SelectedItems
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool ShowsScrollingPlaceholders
		{
			get
			{
				return (bool)this.GetValue(ShowsScrollingPlaceholdersProperty);
			}
			set
			{
				this.SetValue(ShowsScrollingPlaceholdersProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Media.Animation.TransitionCollection FooterTransitions
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Animation.TransitionCollection)this.GetValue(FooterTransitionsProperty);
			}
			set
			{
				this.SetValue(FooterTransitionsProperty, value);
			}
		}
		#endif
		// Skipping already declared property FooterTemplate
		// Skipping already declared property Footer
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Controls.ListViewReorderMode ReorderMode
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.ListViewReorderMode)this.GetValue(ReorderModeProperty);
			}
			set
			{
				this.SetValue(ReorderModeProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsMultiSelectCheckBoxEnabled
		{
			get
			{
				return (bool)this.GetValue(IsMultiSelectCheckBoxEnabledProperty);
			}
			set
			{
				this.SetValue(IsMultiSelectCheckBoxEnabledProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.UI.Xaml.Data.ItemIndexRange> SelectedRanges
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<ItemIndexRange> ListViewBase.SelectedRanges is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool SingleSelectionFollowsFocus
		{
			get
			{
				return (bool)this.GetValue(SingleSelectionFollowsFocusProperty);
			}
			set
			{
				this.SetValue(SingleSelectionFollowsFocusProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Controls.SemanticZoom SemanticZoomOwner
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.SemanticZoom)this.GetValue(SemanticZoomOwnerProperty);
			}
			set
			{
				this.SetValue(SemanticZoomOwnerProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsZoomedInView
		{
			get
			{
				return (bool)this.GetValue(IsZoomedInViewProperty);
			}
			set
			{
				this.SetValue(IsZoomedInViewProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsActiveView
		{
			get
			{
				return (bool)this.GetValue(IsActiveViewProperty);
			}
			set
			{
				this.SetValue(IsActiveViewProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty CanDragItemsProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(CanDragItems), typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.ListViewBase), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty CanReorderItemsProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(CanReorderItems), typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.ListViewBase), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		// Skipping already declared property DataFetchSizeProperty
		// Skipping already declared property HeaderProperty
		// Skipping already declared property HeaderTemplateProperty
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty HeaderTransitionsProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(HeaderTransitions), typeof(global::Windows.UI.Xaml.Media.Animation.TransitionCollection), 
			typeof(global::Windows.UI.Xaml.Controls.ListViewBase), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Animation.TransitionCollection)));
		#endif
		// Skipping already declared property IncrementalLoadingThresholdProperty
		// Skipping already declared property IncrementalLoadingTriggerProperty
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty IsActiveViewProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(IsActiveView), typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.ListViewBase), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		// Skipping already declared property IsItemClickEnabledProperty
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty IsSwipeEnabledProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(IsSwipeEnabled), typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.ListViewBase), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty IsZoomedInViewProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(IsZoomedInView), typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.ListViewBase), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		// Skipping already declared property SelectionModeProperty
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty SemanticZoomOwnerProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(SemanticZoomOwner), typeof(global::Windows.UI.Xaml.Controls.SemanticZoom), 
			typeof(global::Windows.UI.Xaml.Controls.ListViewBase), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.SemanticZoom)));
		#endif
		// Skipping already declared property FooterProperty
		// Skipping already declared property FooterTemplateProperty
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty FooterTransitionsProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(FooterTransitions), typeof(global::Windows.UI.Xaml.Media.Animation.TransitionCollection), 
			typeof(global::Windows.UI.Xaml.Controls.ListViewBase), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Animation.TransitionCollection)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty ShowsScrollingPlaceholdersProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(ShowsScrollingPlaceholders), typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.ListViewBase), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty ReorderModeProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(ReorderMode), typeof(global::Windows.UI.Xaml.Controls.ListViewReorderMode), 
			typeof(global::Windows.UI.Xaml.Controls.ListViewBase), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.ListViewReorderMode)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty IsMultiSelectCheckBoxEnabledProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(IsMultiSelectCheckBoxEnabled), typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.ListViewBase), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty SingleSelectionFollowsFocusProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(SingleSelectionFollowsFocus), typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.ListViewBase), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		// Skipping already declared method Windows.UI.Xaml.Controls.ListViewBase.ListViewBase()
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.ListViewBase()
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.SelectedItems.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.SelectionMode.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.SelectionMode.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.IsSwipeEnabled.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.IsSwipeEnabled.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.CanDragItems.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.CanDragItems.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.CanReorderItems.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.CanReorderItems.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.IsItemClickEnabled.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.IsItemClickEnabled.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.DataFetchSize.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.DataFetchSize.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.IncrementalLoadingThreshold.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.IncrementalLoadingThreshold.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.IncrementalLoadingTrigger.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.IncrementalLoadingTrigger.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.ItemClick.add
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.ItemClick.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.DragItemsStarting.add
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.DragItemsStarting.remove
		#if false || false || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void ScrollIntoView( object item)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ListViewBase", "void ListViewBase.ScrollIntoView(object item)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SelectAll()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ListViewBase", "void ListViewBase.SelectAll()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.UI.Xaml.Data.LoadMoreItemsResult> LoadMoreItemsAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<LoadMoreItemsResult> ListViewBase.LoadMoreItemsAsync() is not implemented in Uno.");
		}
		#endif
		#if false || false || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void ScrollIntoView( object item,  global::Windows.UI.Xaml.Controls.ScrollIntoViewAlignment alignment)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ListViewBase", "void ListViewBase.ScrollIntoView(object item, ScrollIntoViewAlignment alignment)");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.Header.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.Header.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.HeaderTemplate.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.HeaderTemplate.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.HeaderTransitions.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.HeaderTransitions.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.ShowsScrollingPlaceholders.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.ShowsScrollingPlaceholders.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.ContainerContentChanging.add
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.ContainerContentChanging.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetDesiredContainerUpdateDuration( global::System.TimeSpan duration)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ListViewBase", "void ListViewBase.SetDesiredContainerUpdateDuration(TimeSpan duration)");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.Footer.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.Footer.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.FooterTemplate.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.FooterTemplate.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.FooterTransitions.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.FooterTransitions.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.ReorderMode.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.ReorderMode.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.SelectedRanges.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.IsMultiSelectCheckBoxEnabled.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.IsMultiSelectCheckBoxEnabled.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.DragItemsCompleted.add
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.DragItemsCompleted.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.ChoosingItemContainer.add
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.ChoosingItemContainer.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.ChoosingGroupHeaderContainer.add
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.ChoosingGroupHeaderContainer.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SelectRange( global::Windows.UI.Xaml.Data.ItemIndexRange itemIndexRange)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ListViewBase", "void ListViewBase.SelectRange(ItemIndexRange itemIndexRange)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void DeselectRange( global::Windows.UI.Xaml.Data.ItemIndexRange itemIndexRange)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ListViewBase", "void ListViewBase.DeselectRange(ItemIndexRange itemIndexRange)");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.SingleSelectionFollowsFocus.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.SingleSelectionFollowsFocus.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsDragSource()
		{
			throw new global::System.NotImplementedException("The member bool ListViewBase.IsDragSource() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> TryStartConnectedAnimationAsync( global::Windows.UI.Xaml.Media.Animation.ConnectedAnimation animation,  object item,  string elementName)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> ListViewBase.TryStartConnectedAnimationAsync(ConnectedAnimation animation, object item, string elementName) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Media.Animation.ConnectedAnimation PrepareConnectedAnimation( string key,  object item,  string elementName)
		{
			throw new global::System.NotImplementedException("The member ConnectedAnimation ListViewBase.PrepareConnectedAnimation(string key, object item, string elementName) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.SemanticZoomOwner.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.SemanticZoomOwner.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.IsActiveView.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.IsActiveView.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.IsZoomedInView.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.IsZoomedInView.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void InitializeViewChange()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ListViewBase", "void ListViewBase.InitializeViewChange()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void CompleteViewChange()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ListViewBase", "void ListViewBase.CompleteViewChange()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void MakeVisible( global::Windows.UI.Xaml.Controls.SemanticZoomLocation item)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ListViewBase", "void ListViewBase.MakeVisible(SemanticZoomLocation item)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void StartViewChangeFrom( global::Windows.UI.Xaml.Controls.SemanticZoomLocation source,  global::Windows.UI.Xaml.Controls.SemanticZoomLocation destination)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ListViewBase", "void ListViewBase.StartViewChangeFrom(SemanticZoomLocation source, SemanticZoomLocation destination)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void StartViewChangeTo( global::Windows.UI.Xaml.Controls.SemanticZoomLocation source,  global::Windows.UI.Xaml.Controls.SemanticZoomLocation destination)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ListViewBase", "void ListViewBase.StartViewChangeTo(SemanticZoomLocation source, SemanticZoomLocation destination)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void CompleteViewChangeFrom( global::Windows.UI.Xaml.Controls.SemanticZoomLocation source,  global::Windows.UI.Xaml.Controls.SemanticZoomLocation destination)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ListViewBase", "void ListViewBase.CompleteViewChangeFrom(SemanticZoomLocation source, SemanticZoomLocation destination)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void CompleteViewChangeTo( global::Windows.UI.Xaml.Controls.SemanticZoomLocation source,  global::Windows.UI.Xaml.Controls.SemanticZoomLocation destination)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ListViewBase", "void ListViewBase.CompleteViewChangeTo(SemanticZoomLocation source, SemanticZoomLocation destination)");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.SingleSelectionFollowsFocusProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.IsMultiSelectCheckBoxEnabledProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.ReorderModeProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.ShowsScrollingPlaceholdersProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.FooterProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.FooterTemplateProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.FooterTransitionsProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.SelectionModeProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.IsSwipeEnabledProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.CanDragItemsProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.CanReorderItemsProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.IsItemClickEnabledProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.DataFetchSizeProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.IncrementalLoadingThresholdProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.IncrementalLoadingTriggerProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.SemanticZoomOwnerProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.IsActiveViewProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.IsZoomedInViewProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.HeaderProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.HeaderTemplateProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ListViewBase.HeaderTransitionsProperty.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.UI.Xaml.Controls.DragItemsStartingEventHandler DragItemsStarting
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ListViewBase", "event DragItemsStartingEventHandler ListViewBase.DragItemsStarting");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ListViewBase", "event DragItemsStartingEventHandler ListViewBase.DragItemsStarting");
			}
		}
		#endif
		// Skipping already declared event Windows.UI.Xaml.Controls.ListViewBase.ItemClick
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.ListViewBase, global::Windows.UI.Xaml.Controls.ContainerContentChangingEventArgs> ContainerContentChanging
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ListViewBase", "event TypedEventHandler<ListViewBase, ContainerContentChangingEventArgs> ListViewBase.ContainerContentChanging");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ListViewBase", "event TypedEventHandler<ListViewBase, ContainerContentChangingEventArgs> ListViewBase.ContainerContentChanging");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.ListViewBase, global::Windows.UI.Xaml.Controls.ChoosingGroupHeaderContainerEventArgs> ChoosingGroupHeaderContainer
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ListViewBase", "event TypedEventHandler<ListViewBase, ChoosingGroupHeaderContainerEventArgs> ListViewBase.ChoosingGroupHeaderContainer");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ListViewBase", "event TypedEventHandler<ListViewBase, ChoosingGroupHeaderContainerEventArgs> ListViewBase.ChoosingGroupHeaderContainer");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.ListViewBase, global::Windows.UI.Xaml.Controls.ChoosingItemContainerEventArgs> ChoosingItemContainer
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ListViewBase", "event TypedEventHandler<ListViewBase, ChoosingItemContainerEventArgs> ListViewBase.ChoosingItemContainer");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ListViewBase", "event TypedEventHandler<ListViewBase, ChoosingItemContainerEventArgs> ListViewBase.ChoosingItemContainer");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.ListViewBase, global::Windows.UI.Xaml.Controls.DragItemsCompletedEventArgs> DragItemsCompleted
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ListViewBase", "event TypedEventHandler<ListViewBase, DragItemsCompletedEventArgs> ListViewBase.DragItemsCompleted");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ListViewBase", "event TypedEventHandler<ListViewBase, DragItemsCompletedEventArgs> ListViewBase.DragItemsCompleted");
			}
		}
		#endif
		// Processing: Windows.UI.Xaml.Controls.ISemanticZoomInformation
	}
}
