#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SceneAnalysisEffect : global::Windows.Media.IMediaExtension
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan DesiredAnalysisInterval
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan SceneAnalysisEffect.DesiredAnalysisInterval is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.SceneAnalysisEffect", "TimeSpan SceneAnalysisEffect.DesiredAnalysisInterval");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Core.HighDynamicRangeControl HighDynamicRangeAnalyzer
		{
			get
			{
				throw new global::System.NotImplementedException("The member HighDynamicRangeControl SceneAnalysisEffect.HighDynamicRangeAnalyzer is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Core.SceneAnalysisEffect.HighDynamicRangeAnalyzer.get
		// Forced skipping of method Windows.Media.Core.SceneAnalysisEffect.DesiredAnalysisInterval.set
		// Forced skipping of method Windows.Media.Core.SceneAnalysisEffect.DesiredAnalysisInterval.get
		// Forced skipping of method Windows.Media.Core.SceneAnalysisEffect.SceneAnalyzed.add
		// Forced skipping of method Windows.Media.Core.SceneAnalysisEffect.SceneAnalyzed.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetProperties( global::Windows.Foundation.Collections.IPropertySet configuration)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.SceneAnalysisEffect", "void SceneAnalysisEffect.SetProperties(IPropertySet configuration)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Core.SceneAnalysisEffect, global::Windows.Media.Core.SceneAnalyzedEventArgs> SceneAnalyzed
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.SceneAnalysisEffect", "event TypedEventHandler<SceneAnalysisEffect, SceneAnalyzedEventArgs> SceneAnalysisEffect.SceneAnalyzed");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.SceneAnalysisEffect", "event TypedEventHandler<SceneAnalysisEffect, SceneAnalyzedEventArgs> SceneAnalysisEffect.SceneAnalyzed");
			}
		}
		#endif
		// Processing: Windows.Media.IMediaExtension
	}
}
