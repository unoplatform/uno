#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DurationHelper 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.Duration Automatic
		{
			get
			{
				throw new global::System.NotImplementedException("The member Duration DurationHelper.Automatic is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.Duration Forever
		{
			get
			{
				throw new global::System.NotImplementedException("The member Duration DurationHelper.Forever is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.DurationHelper.Automatic.get
		// Forced skipping of method Windows.UI.Xaml.DurationHelper.Forever.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static int Compare( global::Windows.UI.Xaml.Duration duration1,  global::Windows.UI.Xaml.Duration duration2)
		{
			throw new global::System.NotImplementedException("The member int DurationHelper.Compare(Duration duration1, Duration duration2) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.Duration FromTimeSpan( global::System.TimeSpan timeSpan)
		{
			throw new global::System.NotImplementedException("The member Duration DurationHelper.FromTimeSpan(TimeSpan timeSpan) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static bool GetHasTimeSpan( global::Windows.UI.Xaml.Duration target)
		{
			throw new global::System.NotImplementedException("The member bool DurationHelper.GetHasTimeSpan(Duration target) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.Duration Add( global::Windows.UI.Xaml.Duration target,  global::Windows.UI.Xaml.Duration duration)
		{
			throw new global::System.NotImplementedException("The member Duration DurationHelper.Add(Duration target, Duration duration) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static bool Equals( global::Windows.UI.Xaml.Duration target,  global::Windows.UI.Xaml.Duration value)
		{
			throw new global::System.NotImplementedException("The member bool DurationHelper.Equals(Duration target, Duration value) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.Duration Subtract( global::Windows.UI.Xaml.Duration target,  global::Windows.UI.Xaml.Duration duration)
		{
			throw new global::System.NotImplementedException("The member Duration DurationHelper.Subtract(Duration target, Duration duration) is not implemented in Uno.");
		}
		#endif
	}
}
