#nullable disable

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.System.Profile.Internal;

namespace Uno.UI.Tests.Windows_System.Profile.Internal
{
	[TestClass]
	public class VersionHelpersTests
	{
		[TestMethod]
		public void When_Windows_Insider_Version_ToLong()
		{
			var version = new Version("10.0.22523.1000");
			var longVersion = VersionHelpers.ToLong(version);
			Assert.AreEqual(2814751243174888L, longVersion);
		}
	}
}
