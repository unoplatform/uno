#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.StartScreen
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class JumpList 
	{
		#if false || false || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.StartScreen.JumpListSystemGroupKind SystemGroupKind
		{
			get
			{
				throw new global::System.NotImplementedException("The member JumpListSystemGroupKind JumpList.SystemGroupKind is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=JumpListSystemGroupKind%20JumpList.SystemGroupKind");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.StartScreen.JumpList", "JumpListSystemGroupKind JumpList.SystemGroupKind");
			}
		}
		#endif
		#if false || false || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<global::Windows.UI.StartScreen.JumpListItem> Items
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<JumpListItem> JumpList.Items is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IList%3CJumpListItem%3E%20JumpList.Items");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.StartScreen.JumpList.Items.get
		// Forced skipping of method Windows.UI.StartScreen.JumpList.SystemGroupKind.get
		// Forced skipping of method Windows.UI.StartScreen.JumpList.SystemGroupKind.set
		#if false || false || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction SaveAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction JumpList.SaveAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncAction%20JumpList.SaveAsync%28%29");
		}
		#endif
		#if false || false || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.UI.StartScreen.JumpList> LoadCurrentAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<JumpList> JumpList.LoadCurrentAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CJumpList%3E%20JumpList.LoadCurrentAsync%28%29");
		}
		#endif
		// Skipping already declared method Windows.UI.StartScreen.JumpList.IsSupported()
	}
}
