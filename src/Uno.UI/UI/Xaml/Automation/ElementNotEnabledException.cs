using System;

namespace Microsoft.UI.Xaml.Automation;

public partial class ElementNotEnabledException : Exception
{
	// UIA_E_ELEMENTNOTENABLED — the HRESULT WinUI/UIA returns when a pattern method (Invoke,
	// Toggle, Select, ...) is invoked on a disabled element. Assigning it here means every peer
	// that throws this exception surfaces the correct error to UIA clients through the Win32 COM
	// interop layer (Marshal.GetHRForException reads Exception.HResult).
	private const int UIA_E_ELEMENTNOTENABLED = unchecked((int)0x80040200);

	public ElementNotEnabledException() : base()
	{
		HResult = UIA_E_ELEMENTNOTENABLED;
	}

	public ElementNotEnabledException(string message) : base(message)
	{
		HResult = UIA_E_ELEMENTNOTENABLED;
	}

	public ElementNotEnabledException(string message, Exception innerException) : base(message, innerException)
	{
		HResult = UIA_E_ELEMENTNOTENABLED;
	}
}
