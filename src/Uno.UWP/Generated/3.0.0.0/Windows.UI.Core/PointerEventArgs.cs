#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PointerEventArgs : global::Windows.UI.Core.ICoreWindowEventArgs
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool Handled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool PointerEventArgs.Handled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.PointerEventArgs", "bool PointerEventArgs.Handled");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Input.PointerPoint CurrentPoint
		{
			get
			{
				throw new global::System.NotImplementedException("The member PointerPoint PointerEventArgs.CurrentPoint is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.System.VirtualKeyModifiers KeyModifiers
		{
			get
			{
				throw new global::System.NotImplementedException("The member VirtualKeyModifiers PointerEventArgs.KeyModifiers is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Core.PointerEventArgs.CurrentPoint.get
		// Forced skipping of method Windows.UI.Core.PointerEventArgs.KeyModifiers.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.Collections.Generic.IList<global::Windows.UI.Input.PointerPoint> GetIntermediatePoints()
		{
			throw new global::System.NotImplementedException("The member IList<PointerPoint> PointerEventArgs.GetIntermediatePoints() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.Core.PointerEventArgs.Handled.get
		// Forced skipping of method Windows.UI.Core.PointerEventArgs.Handled.set
		// Processing: Windows.UI.Core.ICoreWindowEventArgs
	}
}
