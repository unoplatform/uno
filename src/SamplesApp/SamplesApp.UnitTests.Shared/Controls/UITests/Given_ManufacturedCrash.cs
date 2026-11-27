#nullable enable

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SamplesApp.Tests;

// TEMP (do not merge): crashes the app on purpose to exercise the WinUI runtime tests crash diagnostics.
[TestClass]
public class Given_ManufacturedCrash
{
	[TestMethod]
	public void When_App_Crashes_Natively()
	{
		// Writing to an invalid address raises an AccessViolation, which kills the process (0xC0000005).
		Marshal.WriteByte(new IntPtr(0x10), 0);
	}
}
