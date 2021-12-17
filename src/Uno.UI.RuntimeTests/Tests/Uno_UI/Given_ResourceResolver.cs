using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Uno_UI
{
	public class MyCustomClass
	{
		public Duration Duration { get; set; }
	}

	[TestClass]
	public class Given_ResourceResolver
	{
		[TestMethod]
		[RunsOnUIThread]
		public void When_Resolving_String_Resources_Should_Produce_Target_Type()
		{
			var SUT = new Resolve_String_Resources();
			WindowHelper.WindowContent = SUT;
			var expected = new Duration(TimeSpan.Parse("00:00:00.250"));

			Assert.AreEqual(expected, SUT.ReferencingStringDurationFromDP.Duration);
			Assert.AreEqual(expected, SUT.ReferenceStringDurationFromProperty.Duration);
		}
	}
}
