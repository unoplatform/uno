#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Navigation
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class PageStackEntry : global::Windows.UI.Xaml.DependencyObject
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Media.Animation.NavigationTransitionInfo NavigationTransitionInfo
		{
			get
			{
				throw new global::System.NotImplementedException("The member NavigationTransitionInfo PageStackEntry.NavigationTransitionInfo is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  object Parameter
		{
			get
			{
				throw new global::System.NotImplementedException("The member object PageStackEntry.Parameter is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::System.Type SourcePageType
		{
			get
			{
				return (global::System.Type)this.GetValue(SourcePageTypeProperty);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty SourcePageTypeProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"SourcePageType", typeof(global::System.Type), 
			typeof(global::Windows.UI.Xaml.Navigation.PageStackEntry), 
			new FrameworkPropertyMetadata(default(global::System.Type)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public PageStackEntry( global::System.Type sourcePageType,  object parameter,  global::Windows.UI.Xaml.Media.Animation.NavigationTransitionInfo navigationTransitionInfo) : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Navigation.PageStackEntry", "PageStackEntry.PageStackEntry(Type sourcePageType, object parameter, NavigationTransitionInfo navigationTransitionInfo)");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Navigation.PageStackEntry.PageStackEntry(System.Type, object, Windows.UI.Xaml.Media.Animation.NavigationTransitionInfo)
		// Forced skipping of method Windows.UI.Xaml.Navigation.PageStackEntry.SourcePageType.get
		// Forced skipping of method Windows.UI.Xaml.Navigation.PageStackEntry.Parameter.get
		// Forced skipping of method Windows.UI.Xaml.Navigation.PageStackEntry.NavigationTransitionInfo.get
		// Forced skipping of method Windows.UI.Xaml.Navigation.PageStackEntry.SourcePageTypeProperty.get
	}
}
