#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media.Animation
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class ObjectKeyFrame : global::Windows.UI.Xaml.DependencyObject
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  object Value
		{
			get
			{
				return (object)this.GetValue(ValueProperty);
			}
			set
			{
				this.SetValue(ValueProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Media.Animation.KeyTime KeyTime
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Animation.KeyTime)this.GetValue(KeyTimeProperty);
			}
			set
			{
				this.SetValue(KeyTimeProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty KeyTimeProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"KeyTime", typeof(global::Windows.UI.Xaml.Media.Animation.KeyTime), 
			typeof(global::Windows.UI.Xaml.Media.Animation.ObjectKeyFrame), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Animation.KeyTime)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ValueProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Value", typeof(object), 
			typeof(global::Windows.UI.Xaml.Media.Animation.ObjectKeyFrame), 
			new FrameworkPropertyMetadata(default(object)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		protected ObjectKeyFrame() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Animation.ObjectKeyFrame", "ObjectKeyFrame.ObjectKeyFrame()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.ObjectKeyFrame.ObjectKeyFrame()
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.ObjectKeyFrame.Value.get
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.ObjectKeyFrame.Value.set
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.ObjectKeyFrame.KeyTime.get
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.ObjectKeyFrame.KeyTime.set
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.ObjectKeyFrame.ValueProperty.get
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.ObjectKeyFrame.KeyTimeProperty.get
	}
}
