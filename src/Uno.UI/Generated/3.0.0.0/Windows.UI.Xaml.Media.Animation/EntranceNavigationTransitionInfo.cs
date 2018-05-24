#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media.Animation
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class EntranceNavigationTransitionInfo : global::Windows.UI.Xaml.Media.Animation.NavigationTransitionInfo
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsTargetElementProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"IsTargetElement", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Media.Animation.EntranceNavigationTransitionInfo), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public EntranceNavigationTransitionInfo() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Animation.EntranceNavigationTransitionInfo", "EntranceNavigationTransitionInfo.EntranceNavigationTransitionInfo()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.EntranceNavigationTransitionInfo.EntranceNavigationTransitionInfo()
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.EntranceNavigationTransitionInfo.IsTargetElementProperty.get
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static bool GetIsTargetElement( global::Windows.UI.Xaml.UIElement element)
		{
			return (bool)element.GetValue(IsTargetElementProperty);
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static void SetIsTargetElement( global::Windows.UI.Xaml.UIElement element,  bool value)
		{
			element.SetValue(IsTargetElementProperty, value);
		}
		#endif
	}
}
