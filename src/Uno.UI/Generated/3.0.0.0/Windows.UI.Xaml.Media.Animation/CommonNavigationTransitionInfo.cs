#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media.Animation
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class CommonNavigationTransitionInfo : global::Windows.UI.Xaml.Media.Animation.NavigationTransitionInfo
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool IsStaggeringEnabled
		{
			get
			{
				return (bool)this.GetValue(IsStaggeringEnabledProperty);
			}
			set
			{
				this.SetValue(IsStaggeringEnabledProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsStaggerElementProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"IsStaggerElement", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Media.Animation.CommonNavigationTransitionInfo), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsStaggeringEnabledProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsStaggeringEnabled", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Media.Animation.CommonNavigationTransitionInfo), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public CommonNavigationTransitionInfo() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Animation.CommonNavigationTransitionInfo", "CommonNavigationTransitionInfo.CommonNavigationTransitionInfo()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.CommonNavigationTransitionInfo.CommonNavigationTransitionInfo()
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.CommonNavigationTransitionInfo.IsStaggeringEnabled.get
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.CommonNavigationTransitionInfo.IsStaggeringEnabled.set
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.CommonNavigationTransitionInfo.IsStaggeringEnabledProperty.get
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.CommonNavigationTransitionInfo.IsStaggerElementProperty.get
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static bool GetIsStaggerElement( global::Windows.UI.Xaml.UIElement element)
		{
			return (bool)element.GetValue(IsStaggerElementProperty);
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static void SetIsStaggerElement( global::Windows.UI.Xaml.UIElement element,  bool value)
		{
			element.SetValue(IsStaggerElementProperty, value);
		}
		#endif
	}
}
