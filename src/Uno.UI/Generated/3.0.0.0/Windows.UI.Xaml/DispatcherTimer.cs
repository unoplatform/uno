#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class DispatcherTimer 
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::System.TimeSpan Interval
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan DispatcherTimer.Interval is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.DispatcherTimer", "TimeSpan DispatcherTimer.Interval");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool IsEnabled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool DispatcherTimer.IsEnabled is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public DispatcherTimer() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.DispatcherTimer", "DispatcherTimer.DispatcherTimer()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.DispatcherTimer.DispatcherTimer()
		// Forced skipping of method Windows.UI.Xaml.DispatcherTimer.Interval.get
		// Forced skipping of method Windows.UI.Xaml.DispatcherTimer.Interval.set
		// Forced skipping of method Windows.UI.Xaml.DispatcherTimer.IsEnabled.get
		// Forced skipping of method Windows.UI.Xaml.DispatcherTimer.Tick.add
		// Forced skipping of method Windows.UI.Xaml.DispatcherTimer.Tick.remove
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  void Start()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.DispatcherTimer", "void DispatcherTimer.Start()");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  void Stop()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.DispatcherTimer", "void DispatcherTimer.Stop()");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  event global::System.EventHandler<object> Tick
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.DispatcherTimer", "event EventHandler<object> DispatcherTimer.Tick");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.DispatcherTimer", "event EventHandler<object> DispatcherTimer.Tick");
			}
		}
		#endif
	}
}
