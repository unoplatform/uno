#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input.Inking
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class InkModelerAttributes 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  float ScalingFactor
		{
			get
			{
				throw new global::System.NotImplementedException("The member float InkModelerAttributes.ScalingFactor is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.Inking.InkModelerAttributes", "float InkModelerAttributes.ScalingFactor");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan PredictionTime
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan InkModelerAttributes.PredictionTime is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.Inking.InkModelerAttributes", "TimeSpan InkModelerAttributes.PredictionTime");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool UseVelocityBasedPressure
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool InkModelerAttributes.UseVelocityBasedPressure is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.Inking.InkModelerAttributes", "bool InkModelerAttributes.UseVelocityBasedPressure");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Input.Inking.InkModelerAttributes.PredictionTime.get
		// Forced skipping of method Windows.UI.Input.Inking.InkModelerAttributes.PredictionTime.set
		// Forced skipping of method Windows.UI.Input.Inking.InkModelerAttributes.ScalingFactor.get
		// Forced skipping of method Windows.UI.Input.Inking.InkModelerAttributes.ScalingFactor.set
		// Forced skipping of method Windows.UI.Input.Inking.InkModelerAttributes.UseVelocityBasedPressure.get
		// Forced skipping of method Windows.UI.Input.Inking.InkModelerAttributes.UseVelocityBasedPressure.set
	}
}
