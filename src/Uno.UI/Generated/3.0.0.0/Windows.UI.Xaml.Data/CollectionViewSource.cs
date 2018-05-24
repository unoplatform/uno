#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Data
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class CollectionViewSource : global::Windows.UI.Xaml.DependencyObject
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  object Source
		{
			get
			{
				return (object)this.GetValue(SourceProperty);
			}
			set
			{
				this.SetValue(SourceProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.PropertyPath ItemsPath
		{
			get
			{
				return (global::Windows.UI.Xaml.PropertyPath)this.GetValue(ItemsPathProperty);
			}
			set
			{
				this.SetValue(ItemsPathProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool IsSourceGrouped
		{
			get
			{
				return (bool)this.GetValue(IsSourceGroupedProperty);
			}
			set
			{
				this.SetValue(IsSourceGroupedProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Data.ICollectionView View
		{
			get
			{
				return (global::Windows.UI.Xaml.Data.ICollectionView)this.GetValue(ViewProperty);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsSourceGroupedProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsSourceGrouped", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Data.CollectionViewSource), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ItemsPathProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"ItemsPath", typeof(global::Windows.UI.Xaml.PropertyPath), 
			typeof(global::Windows.UI.Xaml.Data.CollectionViewSource), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.PropertyPath)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty SourceProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Source", typeof(object), 
			typeof(global::Windows.UI.Xaml.Data.CollectionViewSource), 
			new FrameworkPropertyMetadata(default(object)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ViewProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"View", typeof(global::Windows.UI.Xaml.Data.ICollectionView), 
			typeof(global::Windows.UI.Xaml.Data.CollectionViewSource), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Data.ICollectionView)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public CollectionViewSource() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Data.CollectionViewSource", "CollectionViewSource.CollectionViewSource()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Data.CollectionViewSource.CollectionViewSource()
		// Forced skipping of method Windows.UI.Xaml.Data.CollectionViewSource.Source.get
		// Forced skipping of method Windows.UI.Xaml.Data.CollectionViewSource.Source.set
		// Forced skipping of method Windows.UI.Xaml.Data.CollectionViewSource.View.get
		// Forced skipping of method Windows.UI.Xaml.Data.CollectionViewSource.IsSourceGrouped.get
		// Forced skipping of method Windows.UI.Xaml.Data.CollectionViewSource.IsSourceGrouped.set
		// Forced skipping of method Windows.UI.Xaml.Data.CollectionViewSource.ItemsPath.get
		// Forced skipping of method Windows.UI.Xaml.Data.CollectionViewSource.ItemsPath.set
		// Forced skipping of method Windows.UI.Xaml.Data.CollectionViewSource.SourceProperty.get
		// Forced skipping of method Windows.UI.Xaml.Data.CollectionViewSource.ViewProperty.get
		// Forced skipping of method Windows.UI.Xaml.Data.CollectionViewSource.IsSourceGroupedProperty.get
		// Forced skipping of method Windows.UI.Xaml.Data.CollectionViewSource.ItemsPathProperty.get
	}
}
