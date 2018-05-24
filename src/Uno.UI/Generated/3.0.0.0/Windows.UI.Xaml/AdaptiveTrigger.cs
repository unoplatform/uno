#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class AdaptiveTrigger : global::Windows.UI.Xaml.StateTriggerBase
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  double MinWindowWidth
		{
			get
			{
				return (double)this.GetValue(MinWindowWidthProperty);
			}
			set
			{
				this.SetValue(MinWindowWidthProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  double MinWindowHeight
		{
			get
			{
				return (double)this.GetValue(MinWindowHeightProperty);
			}
			set
			{
				this.SetValue(MinWindowHeightProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty MinWindowHeightProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"MinWindowHeight", typeof(double), 
			typeof(global::Windows.UI.Xaml.AdaptiveTrigger), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty MinWindowWidthProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"MinWindowWidth", typeof(double), 
			typeof(global::Windows.UI.Xaml.AdaptiveTrigger), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public AdaptiveTrigger() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.AdaptiveTrigger", "AdaptiveTrigger.AdaptiveTrigger()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.AdaptiveTrigger.AdaptiveTrigger()
		// Forced skipping of method Windows.UI.Xaml.AdaptiveTrigger.MinWindowWidth.get
		// Forced skipping of method Windows.UI.Xaml.AdaptiveTrigger.MinWindowWidth.set
		// Forced skipping of method Windows.UI.Xaml.AdaptiveTrigger.MinWindowHeight.get
		// Forced skipping of method Windows.UI.Xaml.AdaptiveTrigger.MinWindowHeight.set
		// Forced skipping of method Windows.UI.Xaml.AdaptiveTrigger.MinWindowWidthProperty.get
		// Forced skipping of method Windows.UI.Xaml.AdaptiveTrigger.MinWindowHeightProperty.get
	}
}
