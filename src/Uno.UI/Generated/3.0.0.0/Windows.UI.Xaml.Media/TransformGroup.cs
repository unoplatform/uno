#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class TransformGroup : global::Windows.UI.Xaml.Media.Transform
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Media.TransformCollection Children
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.TransformCollection)this.GetValue(ChildrenProperty);
			}
			set
			{
				this.SetValue(ChildrenProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Media.Matrix Value
		{
			get
			{
				throw new global::System.NotImplementedException("The member Matrix TransformGroup.Value is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ChildrenProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Children", typeof(global::Windows.UI.Xaml.Media.TransformCollection), 
			typeof(global::Windows.UI.Xaml.Media.TransformGroup), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.TransformCollection)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public TransformGroup() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.TransformGroup", "TransformGroup.TransformGroup()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.TransformGroup.TransformGroup()
		// Forced skipping of method Windows.UI.Xaml.Media.TransformGroup.Children.get
		// Forced skipping of method Windows.UI.Xaml.Media.TransformGroup.Children.set
		// Forced skipping of method Windows.UI.Xaml.Media.TransformGroup.Value.get
		// Forced skipping of method Windows.UI.Xaml.Media.TransformGroup.ChildrenProperty.get
	}
}
