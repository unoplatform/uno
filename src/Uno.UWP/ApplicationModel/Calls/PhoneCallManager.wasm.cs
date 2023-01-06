#if __WASM__
using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.ApplicationModel.Calls
{
	public partial class PhoneCallManager
	{
		private static void ShowPhoneCallUIImpl(string phoneNumber, string displayName)
		{
			var uri = new Uri($"tel:{phoneNumber}");
			var command = $"Uno.UI.WindowManager.current.open(\"{uri.AbsoluteUri}\");";
			Uno.Foundation.WebAssemblyRuntime.InvokeJS(command);
		}
	}
}
#endif
