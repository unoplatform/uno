#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media.Animation
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ColorAnimation : global::Windows.UI.Xaml.Media.Animation.Timeline
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Color? To
		{
			get
			{
				return (global::Windows.UI.Color?)this.GetValue(ToProperty);
			}
			set
			{
				this.SetValue(ToProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Color? From
		{
			get
			{
				return (global::Windows.UI.Color?)this.GetValue(FromProperty);
			}
			set
			{
				this.SetValue(FromProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool EnableDependentAnimation
		{
			get
			{
				return (bool)this.GetValue(EnableDependentAnimationProperty);
			}
			set
			{
				this.SetValue(EnableDependentAnimationProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Media.Animation.EasingFunctionBase EasingFunction
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Animation.EasingFunctionBase)this.GetValue(EasingFunctionProperty);
			}
			set
			{
				this.SetValue(EasingFunctionProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Color? By
		{
			get
			{
				return (global::Windows.UI.Color?)this.GetValue(ByProperty);
			}
			set
			{
				this.SetValue(ByProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ByProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"By", typeof(global::Windows.UI.Color?), 
			typeof(global::Windows.UI.Xaml.Media.Animation.ColorAnimation), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Color?)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty EasingFunctionProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"EasingFunction", typeof(global::Windows.UI.Xaml.Media.Animation.EasingFunctionBase), 
			typeof(global::Windows.UI.Xaml.Media.Animation.ColorAnimation), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Animation.EasingFunctionBase)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty EnableDependentAnimationProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"EnableDependentAnimation", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Media.Animation.ColorAnimation), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty FromProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"From", typeof(global::Windows.UI.Color?), 
			typeof(global::Windows.UI.Xaml.Media.Animation.ColorAnimation), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Color?)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ToProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"To", typeof(global::Windows.UI.Color?), 
			typeof(global::Windows.UI.Xaml.Media.Animation.ColorAnimation), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Color?)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public ColorAnimation() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Animation.ColorAnimation", "ColorAnimation.ColorAnimation()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.ColorAnimation.ColorAnimation()
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.ColorAnimation.From.get
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.ColorAnimation.From.set
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.ColorAnimation.To.get
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.ColorAnimation.To.set
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.ColorAnimation.By.get
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.ColorAnimation.By.set
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.ColorAnimation.EasingFunction.get
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.ColorAnimation.EasingFunction.set
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.ColorAnimation.EnableDependentAnimation.get
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.ColorAnimation.EnableDependentAnimation.set
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.ColorAnimation.FromProperty.get
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.ColorAnimation.ToProperty.get
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.ColorAnimation.ByProperty.get
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.ColorAnimation.EasingFunctionProperty.get
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.ColorAnimation.EnableDependentAnimationProperty.get
	}
}
