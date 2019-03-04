#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Data
{
	#if false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class CollectionViewSource : global::Windows.UI.Xaml.DependencyObject
	{
		// Skipping already declared property Source
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
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
		// Skipping already declared property IsSourceGrouped
		// Skipping already declared property View
		// Skipping already declared property IsSourceGroupedProperty
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ItemsPathProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"ItemsPath", typeof(global::Windows.UI.Xaml.PropertyPath), 
			typeof(global::Windows.UI.Xaml.Data.CollectionViewSource), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.PropertyPath)));
		#endif
		// Skipping already declared property SourceProperty
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ViewProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"View", typeof(global::Windows.UI.Xaml.Data.ICollectionView), 
			typeof(global::Windows.UI.Xaml.Data.CollectionViewSource), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Data.ICollectionView)));
		#endif
		// Skipping already declared method Windows.UI.Xaml.Data.CollectionViewSource.CollectionViewSource()
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
