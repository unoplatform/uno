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
		// A write to an unmapped address outside the null page is a real access violation, which kills the
		// process (0xC0000005). Addresses in the null page are turned into a catchable exception instead.
		Marshal.WriteByte(new IntPtr(0x0000123400000000), 0);
	}
}
