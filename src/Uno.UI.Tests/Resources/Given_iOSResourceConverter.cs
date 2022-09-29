#nullable disable

using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.ApplicationModel.Resources.Core;

namespace Uno.UI.Tests.Resources
{
	[TestClass]
    public class Given_iOSResourceConverter
    {
		private const string DefaultLanguage = "en";

		[DataRow(@"logo.png", @"logo.png", null, null, null)]
		[DataRow(@"Assets\logo.png", @"Assets\logo.png", null, null, null)]
		[DataRow(@"en.lproj\logo.png", @"logo.png", null, "en", null)]
		[DataRow(@"fr.lproj\logo.png", @"logo.png", null, "fr", null)]
		[DataRow(@"fr.lproj\logo@2x.png", @"logo.png", 200, "fr", null)]
		[DataRow(@"fr_CA.lproj\logo@2x.png", @"logo.png", 200, "fr-CA", null)]
		[DataRow(@"logo.png", @"logo.png", 100, null, null)]
		[DataRow(null, @"logo.png", 125, null, null)]
		[DataRow(null, @"logo.png", 150, null, null)]
		[DataRow(@"logo@2x.png", @"logo.png", 200, null, null)]
		[DataRow(@"logo@3x.png", @"logo.png", 300, null, null)]
		[DataRow(null, @"logo.png", 400, null, null)]
		[DataRow(@"logo.9.png", @"logo.9.png", null, null, null)]
		[DataRow(null, @"logo.png", null, null, "uwp")]
		[DataRow(null, @"logo.png", null, null, "android")]
		[DataRow(@"logo.png", @"logo.png", null, null, "ios")]
		[TestMethod]
		public void When_Convert(string expectedPath, string logicalPath, int? scale, string language, string custom)
		{
			var qualifiers = new List<ResourceQualifier>
			{
				new ResourceQualifier("scale", scale?.ToString()),
				new ResourceQualifier("language", language),
				new ResourceQualifier("custom", custom)
			}
			.AsReadOnly();

			var resourceCandidate = new ResourceCandidate(qualifiers, null, logicalPath);
			var actualPath = iOSResourceConverter.Convert(resourceCandidate, DefaultLanguage);
			Assert.AreEqual(expectedPath, actualPath);
		}
    }
}
