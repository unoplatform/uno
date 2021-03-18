#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Perception.Provider
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PerceptionCorrelationGroup 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Devices.Perception.Provider.PerceptionCorrelation> RelativeLocations
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<PerceptionCorrelation> PerceptionCorrelationGroup.RelativeLocations is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public PerceptionCorrelationGroup( global::System.Collections.Generic.IEnumerable<global::Windows.Devices.Perception.Provider.PerceptionCorrelation> relativeLocations) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Perception.Provider.PerceptionCorrelationGroup", "PerceptionCorrelationGroup.PerceptionCorrelationGroup(IEnumerable<PerceptionCorrelation> relativeLocations)");
		}
		#endif
		// Forced skipping of method Windows.Devices.Perception.Provider.PerceptionCorrelationGroup.PerceptionCorrelationGroup(System.Collections.Generic.IEnumerable<Windows.Devices.Perception.Provider.PerceptionCorrelation>)
		// Forced skipping of method Windows.Devices.Perception.Provider.PerceptionCorrelationGroup.RelativeLocations.get
	}
}
