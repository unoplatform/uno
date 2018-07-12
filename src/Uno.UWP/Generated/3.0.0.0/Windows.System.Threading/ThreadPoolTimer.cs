#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System.Threading
{
#if false
	[global::Uno.NotImplemented]
#endif
	public partial class ThreadPoolTimer 
	{
#if false
		[global::Uno.NotImplemented]
		public  global::System.TimeSpan Delay
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan ThreadPoolTimer.Delay is not implemented in Uno.");
			}
		}
#endif
#if false
		[global::Uno.NotImplemented]
		public  global::System.TimeSpan Period
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan ThreadPoolTimer.Period is not implemented in Uno.");
			}
		}
#endif
		// Forced skipping of method Windows.System.Threading.ThreadPoolTimer.Period.get
		// Forced skipping of method Windows.System.Threading.ThreadPoolTimer.Delay.get
#if false
		[global::Uno.NotImplemented]
		public  void Cancel()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.Threading.ThreadPoolTimer", "void ThreadPoolTimer.Cancel()");
		}
#endif
#if false
		[global::Uno.NotImplemented]
		public static global::Windows.System.Threading.ThreadPoolTimer CreatePeriodicTimer( global::Windows.System.Threading.TimerElapsedHandler handler,  global::System.TimeSpan period)
		{
			throw new global::System.NotImplementedException("The member ThreadPoolTimer ThreadPoolTimer.CreatePeriodicTimer(TimerElapsedHandler handler, TimeSpan period) is not implemented in Uno.");
		}
#endif
#if false
		[global::Uno.NotImplemented]
		public static global::Windows.System.Threading.ThreadPoolTimer CreateTimer( global::Windows.System.Threading.TimerElapsedHandler handler,  global::System.TimeSpan delay)
		{
			throw new global::System.NotImplementedException("The member ThreadPoolTimer ThreadPoolTimer.CreateTimer(TimerElapsedHandler handler, TimeSpan delay) is not implemented in Uno.");
		}
#endif
#if false
		[global::Uno.NotImplemented]
		public static global::Windows.System.Threading.ThreadPoolTimer CreatePeriodicTimer( global::Windows.System.Threading.TimerElapsedHandler handler,  global::System.TimeSpan period,  global::Windows.System.Threading.TimerDestroyedHandler destroyed)
		{
			throw new global::System.NotImplementedException("The member ThreadPoolTimer ThreadPoolTimer.CreatePeriodicTimer(TimerElapsedHandler handler, TimeSpan period, TimerDestroyedHandler destroyed) is not implemented in Uno.");
		}
#endif
#if false
		[global::Uno.NotImplemented]
		public static global::Windows.System.Threading.ThreadPoolTimer CreateTimer( global::Windows.System.Threading.TimerElapsedHandler handler,  global::System.TimeSpan delay,  global::Windows.System.Threading.TimerDestroyedHandler destroyed)
		{
			throw new global::System.NotImplementedException("The member ThreadPoolTimer ThreadPoolTimer.CreateTimer(TimerElapsedHandler handler, TimeSpan delay, TimerDestroyedHandler destroyed) is not implemented in Uno.");
		}
#endif
	}
}
