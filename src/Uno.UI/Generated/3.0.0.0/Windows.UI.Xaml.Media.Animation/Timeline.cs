#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media.Animation
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class Timeline : global::Windows.UI.Xaml.DependencyObject
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  double SpeedRatio
		{
			get
			{
				return (double)this.GetValue(SpeedRatioProperty);
			}
			set
			{
				this.SetValue(SpeedRatioProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Media.Animation.RepeatBehavior RepeatBehavior
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Animation.RepeatBehavior)this.GetValue(RepeatBehaviorProperty);
			}
			set
			{
				this.SetValue(RepeatBehaviorProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Media.Animation.FillBehavior FillBehavior
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Animation.FillBehavior)this.GetValue(FillBehaviorProperty);
			}
			set
			{
				this.SetValue(FillBehaviorProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Duration Duration
		{
			get
			{
				return (global::Windows.UI.Xaml.Duration)this.GetValue(DurationProperty);
			}
			set
			{
				this.SetValue(DurationProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::System.TimeSpan? BeginTime
		{
			get
			{
				return (global::System.TimeSpan?)this.GetValue(BeginTimeProperty);
			}
			set
			{
				this.SetValue(BeginTimeProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  bool AutoReverse
		{
			get
			{
				return (bool)this.GetValue(AutoReverseProperty);
			}
			set
			{
				this.SetValue(AutoReverseProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static bool AllowDependentAnimations
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool Timeline.AllowDependentAnimations is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Animation.Timeline", "bool Timeline.AllowDependentAnimations");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty AutoReverseProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"AutoReverse", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Media.Animation.Timeline), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty BeginTimeProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"BeginTime", typeof(global::System.TimeSpan?), 
			typeof(global::Windows.UI.Xaml.Media.Animation.Timeline), 
			new FrameworkPropertyMetadata(default(global::System.TimeSpan?)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty DurationProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Duration", typeof(global::Windows.UI.Xaml.Duration), 
			typeof(global::Windows.UI.Xaml.Media.Animation.Timeline), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Duration)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty FillBehaviorProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"FillBehavior", typeof(global::Windows.UI.Xaml.Media.Animation.FillBehavior), 
			typeof(global::Windows.UI.Xaml.Media.Animation.Timeline), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Animation.FillBehavior)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty RepeatBehaviorProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"RepeatBehavior", typeof(global::Windows.UI.Xaml.Media.Animation.RepeatBehavior), 
			typeof(global::Windows.UI.Xaml.Media.Animation.Timeline), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Animation.RepeatBehavior)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty SpeedRatioProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"SpeedRatio", typeof(double), 
			typeof(global::Windows.UI.Xaml.Media.Animation.Timeline), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		protected Timeline() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Animation.Timeline", "Timeline.Timeline()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.Timeline.Timeline()
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.Timeline.AutoReverse.get
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.Timeline.AutoReverse.set
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.Timeline.BeginTime.get
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.Timeline.BeginTime.set
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.Timeline.Duration.get
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.Timeline.Duration.set
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.Timeline.SpeedRatio.get
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.Timeline.SpeedRatio.set
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.Timeline.FillBehavior.get
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.Timeline.FillBehavior.set
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.Timeline.RepeatBehavior.get
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.Timeline.RepeatBehavior.set
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.Timeline.Completed.add
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.Timeline.Completed.remove
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.Timeline.AllowDependentAnimations.get
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.Timeline.AllowDependentAnimations.set
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.Timeline.AutoReverseProperty.get
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.Timeline.BeginTimeProperty.get
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.Timeline.DurationProperty.get
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.Timeline.SpeedRatioProperty.get
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.Timeline.FillBehaviorProperty.get
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.Timeline.RepeatBehaviorProperty.get
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  event global::System.EventHandler<object> Completed
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Animation.Timeline", "event EventHandler<object> Timeline.Completed");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Animation.Timeline", "event EventHandler<object> Timeline.Completed");
			}
		}
		#endif
	}
}
