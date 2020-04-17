#if __IOS__ || __ANDROID__ || __WASM__ || __MACOS__
using System;

namespace Windows.ApplicationModel.Activation
{
	public partial interface IProtocolActivatedEventArgs : global::Windows.ApplicationModel.Activation.IActivatedEventArgs
	{
		Uri Uri { get; }
	}
}
#endif
