#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Printing.PrintSupport
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PrintSupportSettingsUISession 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string DocumentTitle
		{
			get
			{
				throw new global::System.NotImplementedException("The member string PrintSupportSettingsUISession.DocumentTitle is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20PrintSupportSettingsUISession.DocumentTitle");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Printing.PrintSupport.SettingsLaunchKind LaunchKind
		{
			get
			{
				throw new global::System.NotImplementedException("The member SettingsLaunchKind PrintSupportSettingsUISession.LaunchKind is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=SettingsLaunchKind%20PrintSupportSettingsUISession.LaunchKind");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Printing.PrintSupport.PrintSupportSessionInfo SessionInfo
		{
			get
			{
				throw new global::System.NotImplementedException("The member PrintSupportSessionInfo PrintSupportSettingsUISession.SessionInfo is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=PrintSupportSessionInfo%20PrintSupportSettingsUISession.SessionInfo");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Printing.PrintTicket.WorkflowPrintTicket SessionPrintTicket
		{
			get
			{
				throw new global::System.NotImplementedException("The member WorkflowPrintTicket PrintSupportSettingsUISession.SessionPrintTicket is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=WorkflowPrintTicket%20PrintSupportSettingsUISession.SessionPrintTicket");
			}
		}
		#endif
		// Forced skipping of method Windows.Graphics.Printing.PrintSupport.PrintSupportSettingsUISession.SessionPrintTicket.get
		// Forced skipping of method Windows.Graphics.Printing.PrintSupport.PrintSupportSettingsUISession.DocumentTitle.get
		// Forced skipping of method Windows.Graphics.Printing.PrintSupport.PrintSupportSettingsUISession.LaunchKind.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void UpdatePrintTicket( global::Windows.Graphics.Printing.PrintTicket.WorkflowPrintTicket printTicket)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Printing.PrintSupport.PrintSupportSettingsUISession", "void PrintSupportSettingsUISession.UpdatePrintTicket(WorkflowPrintTicket printTicket)");
		}
		#endif
		// Forced skipping of method Windows.Graphics.Printing.PrintSupport.PrintSupportSettingsUISession.SessionInfo.get
	}
}
