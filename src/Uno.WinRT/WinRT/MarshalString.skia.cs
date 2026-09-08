using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;

namespace Windows.WinRT;

internal class MarshalString
{
	public unsafe static string FromAbi(IntPtr value)
	{
		if (value == IntPtr.Zero)
		{
			return "";
		}
		uint length = default(uint);
		char* value2 = Platform.WindowsGetStringRawBuffer(value, &length);
		return new string(value2, 0, (int)length);
	}

	public static void DisposeAbi(IntPtr hstring)
	{
		if (hstring != IntPtr.Zero)
		{
			_ = Platform.WindowsDeleteString(hstring);
		}
	}

	private static class Platform
	{
		[DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
		internal unsafe static extern char* WindowsGetStringRawBuffer(IntPtr hstring, uint* length);

		[DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
		internal static extern int WindowsDeleteString(IntPtr hstring);
	}
}
