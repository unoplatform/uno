#if !WINAPPSDK
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
			Validate_Formatting("test %, 42% test2", "%1!s!, %2!u!%% %3!s!", "test %", 42, "test2");
			Validate_Formatting("test, 42% test2", "%1!s!, %2!u!%% %3!s!", "test", 42, "test2");
		}

		[TestMethod]
		public void When_Literal_Braces()
		{
			Validate_Formatting("{Unsaved} Doc", "{Unsaved} %1!s!", "Doc");
			Validate_Formatting("Doc {42}", "%1!s! {%2!u!}", "Doc", 42);
			Validate_Formatting("{}", "{}");
			Validate_Formatting("{1}", "{1}", "Doc");
			Validate_Formatting("100% {done}", "%1!u!%% {done}", 100);
		}

		[TestMethod]
		public void When_Format_Fails()
		{
			Validate_Formatting("", "%2!s!", "Doc");
		}

		private void Validate_Formatting(string expected, string format, params object[] args)
			=> Assert.AreEqual(expected, StringUtil.FormatString(format, args));
	}
}
#endif
