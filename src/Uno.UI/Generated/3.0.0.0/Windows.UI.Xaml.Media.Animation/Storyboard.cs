#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media.Animation
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class Storyboard : global::Windows.UI.Xaml.Media.Animation.Timeline
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Media.Animation.TimelineCollection Children
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimelineCollection Storyboard.Children is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty TargetNameProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"TargetName", typeof(string), 
			typeof(global::Windows.UI.Xaml.Media.Animation.Storyboard), 
			new FrameworkPropertyMetadata(default(string)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty TargetPropertyProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"TargetProperty", typeof(string), 
			typeof(global::Windows.UI.Xaml.Media.Animation.Storyboard), 
			new FrameworkPropertyMetadata(default(string)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public Storyboard() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Animation.Storyboard", "Storyboard.Storyboard()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.Storyboard.Storyboard()
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.Storyboard.Children.get
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  void Seek( global::System.TimeSpan offset)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Animation.Storyboard", "void Storyboard.Seek(TimeSpan offset)");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  void Stop()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Animation.Storyboard", "void Storyboard.Stop()");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  void Begin()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Animation.Storyboard", "void Storyboard.Begin()");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  void Pause()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Animation.Storyboard", "void Storyboard.Pause()");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  void Resume()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Animation.Storyboard", "void Storyboard.Resume()");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Media.Animation.ClockState GetCurrentState()
		{
			throw new global::System.NotImplementedException("The member ClockState Storyboard.GetCurrentState() is not implemented in Uno.");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::System.TimeSpan GetCurrentTime()
		{
			throw new global::System.NotImplementedException("The member TimeSpan Storyboard.GetCurrentTime() is not implemented in Uno.");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  void SeekAlignedToLastTick( global::System.TimeSpan offset)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Animation.Storyboard", "void Storyboard.SeekAlignedToLastTick(TimeSpan offset)");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  void SkipToFill()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Animation.Storyboard", "void Storyboard.SkipToFill()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.Storyboard.TargetPropertyProperty.get
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static string GetTargetProperty( global::Windows.UI.Xaml.Media.Animation.Timeline element)
		{
			return (string)element.GetValue(TargetPropertyProperty);
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static void SetTargetProperty( global::Windows.UI.Xaml.Media.Animation.Timeline element,  string path)
		{
			element.SetValue(TargetPropertyProperty, path);
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.Storyboard.TargetNameProperty.get
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static string GetTargetName( global::Windows.UI.Xaml.Media.Animation.Timeline element)
		{
			return (string)element.GetValue(TargetNameProperty);
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static void SetTargetName( global::Windows.UI.Xaml.Media.Animation.Timeline element,  string name)
		{
			element.SetValue(TargetNameProperty, name);
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static void SetTarget( global::Windows.UI.Xaml.Media.Animation.Timeline timeline,  global::Windows.UI.Xaml.DependencyObject target)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Animation.Storyboard", "void Storyboard.SetTarget(Timeline timeline, DependencyObject target)");
		}
		#endif
	}
}
