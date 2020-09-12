#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System.Diagnostics.TraceReporting
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PlatformDiagnosticActions 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool IsScenarioEnabled( global::System.Guid scenarioId)
		{
			throw new global::System.NotImplementedException("The member bool PlatformDiagnosticActions.IsScenarioEnabled(Guid scenarioId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool TryEscalateScenario( global::System.Guid scenarioId,  global::Windows.System.Diagnostics.TraceReporting.PlatformDiagnosticEscalationType escalationType,  string outputDirectory,  bool timestampOutputDirectory,  bool forceEscalationUpload,  global::System.Collections.Generic.IReadOnlyDictionary<string, string> triggers)
		{
			throw new global::System.NotImplementedException("The member bool PlatformDiagnosticActions.TryEscalateScenario(Guid scenarioId, PlatformDiagnosticEscalationType escalationType, string outputDirectory, bool timestampOutputDirectory, bool forceEscalationUpload, IReadOnlyDictionary<string, string> triggers) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.System.Diagnostics.TraceReporting.PlatformDiagnosticActionState DownloadLatestSettingsForNamespace( string partner,  string feature,  bool isScenarioNamespace,  bool downloadOverCostedNetwork,  bool downloadOverBattery)
		{
			throw new global::System.NotImplementedException("The member PlatformDiagnosticActionState PlatformDiagnosticActions.DownloadLatestSettingsForNamespace(string partner, string feature, bool isScenarioNamespace, bool downloadOverCostedNetwork, bool downloadOverBattery) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::System.Collections.Generic.IReadOnlyList<global::System.Guid> GetActiveScenarioList()
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<Guid> PlatformDiagnosticActions.GetActiveScenarioList() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.System.Diagnostics.TraceReporting.PlatformDiagnosticActionState ForceUpload( global::Windows.System.Diagnostics.TraceReporting.PlatformDiagnosticEventBufferLatencies latency,  bool uploadOverCostedNetwork,  bool uploadOverBattery)
		{
			throw new global::System.NotImplementedException("The member PlatformDiagnosticActionState PlatformDiagnosticActions.ForceUpload(PlatformDiagnosticEventBufferLatencies latency, bool uploadOverCostedNetwork, bool uploadOverBattery) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.System.Diagnostics.TraceReporting.PlatformDiagnosticTraceSlotState IsTraceRunning( global::Windows.System.Diagnostics.TraceReporting.PlatformDiagnosticTraceSlotType slotType,  global::System.Guid scenarioId,  ulong traceProfileHash)
		{
			throw new global::System.NotImplementedException("The member PlatformDiagnosticTraceSlotState PlatformDiagnosticActions.IsTraceRunning(PlatformDiagnosticTraceSlotType slotType, Guid scenarioId, ulong traceProfileHash) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.System.Diagnostics.TraceReporting.PlatformDiagnosticTraceRuntimeInfo GetActiveTraceRuntime( global::Windows.System.Diagnostics.TraceReporting.PlatformDiagnosticTraceSlotType slotType)
		{
			throw new global::System.NotImplementedException("The member PlatformDiagnosticTraceRuntimeInfo PlatformDiagnosticActions.GetActiveTraceRuntime(PlatformDiagnosticTraceSlotType slotType) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::System.Collections.Generic.IReadOnlyList<global::Windows.System.Diagnostics.TraceReporting.PlatformDiagnosticTraceInfo> GetKnownTraceList( global::Windows.System.Diagnostics.TraceReporting.PlatformDiagnosticTraceSlotType slotType)
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<PlatformDiagnosticTraceInfo> PlatformDiagnosticActions.GetKnownTraceList(PlatformDiagnosticTraceSlotType slotType) is not implemented in Uno.");
		}
		#endif
	}
}
