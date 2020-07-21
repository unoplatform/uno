#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Background
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SystemCondition : global::Windows.ApplicationModel.Background.IBackgroundCondition
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Background.SystemConditionType ConditionType
		{
			get
			{
				throw new global::System.NotImplementedException("The member SystemConditionType SystemCondition.ConditionType is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public SystemCondition( global::Windows.ApplicationModel.Background.SystemConditionType conditionType) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Background.SystemCondition", "SystemCondition.SystemCondition(SystemConditionType conditionType)");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Background.SystemCondition.SystemCondition(Windows.ApplicationModel.Background.SystemConditionType)
		// Forced skipping of method Windows.ApplicationModel.Background.SystemCondition.ConditionType.get
		// Processing: Windows.ApplicationModel.Background.IBackgroundCondition
	}
}
