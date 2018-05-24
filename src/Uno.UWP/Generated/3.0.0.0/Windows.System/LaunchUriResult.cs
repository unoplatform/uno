#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class LaunchUriResult 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.Collections.ValueSet Result
		{
			get
			{
				throw new global::System.NotImplementedException("The member ValueSet LaunchUriResult.Result is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.System.LaunchUriStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member LaunchUriStatus LaunchUriResult.Status is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.System.LaunchUriResult.Status.get
		// Forced skipping of method Windows.System.LaunchUriResult.Result.get
	}
}
