#if !NETFX_CORE
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Helpers.WinUI;

namespace Uno.UI.RuntimeTests.MUX.Helpers
{
	[TestClass]
	public class StringUtilTests
	{
		[TestMethod]
		public void When_StringUtil()
		{
			Validate_Formatting("test, icon", "%1!s!, icon", "test");
			Validate_Formatting("test, 42 items", "%1!s!, %2!u! items", "test", 42);
			Validate_Formatting("test, 42 test2", "%1!s!, %2!u! %3!s!", "test", 42, "test2");
		}

		private void Validate_Formatting(string expected, string format, params object[] args)
			=> Assert.AreEqual(expected, StringUtil.FormatString(format, args));
	}
}
#endif
