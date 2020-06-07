using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.ApplicationModel.Resources.Core;

namespace Uno.UI.Tests.Resources
{
	[TestClass]
    public class Given_AndroidResourceConverter
    {
		private const string DefaultLanguage = "en";

		[DataRow(@"drawable-mdpi\logo.png", @"logo.png", null, null, null)]
		[DataRow(@"drawable-mdpi\logo.png", @"Assets\logo.png", null, null, null)]
		[DataRow(@"drawable-mdpi\logo.png", @"Assets\logo.png", null, "en", null)]
		[DataRow(@"drawable-fr-mdpi\logo.png", @"Assets\logo.png", null, "fr", null)]
		[DataRow(@"drawable-fr-xhdpi\logo.png", @"Assets\logo.png", 200, "fr", null)]
		[DataRow(@"drawable-fr-rCA-xhdpi\logo.png", @"Assets\logo.png", 200, "fr-CA", null)]
		[DataRow(@"drawable-mdpi\logo.png", @"Assets\logo.png", 100, null, null)]
		[DataRow(null, @"Assets\logo.png", 125, null, null)]
		[DataRow(@"drawable-hdpi\logo.png", @"Assets\logo.png", 150, null, null)]
		[DataRow(@"drawable-xhdpi\logo.png", @"Assets\logo.png", 200, null, null)]
		[DataRow(@"drawable-xxhdpi\logo.png", @"Assets\logo.png", 300, null, null)]
		[DataRow(@"drawable-xxxhdpi\logo.png", @"Assets\logo.png", 400, null, null)]
		[DataRow(@"drawable-mdpi\logo.9.png", @"Assets\logo.9.png", null, null, null)]
		[DataRow(@"drawable-mdpi\logo_.png", @"Assets\logo-.png", null, null, null)]
		[DataRow(@"drawable-mdpi\logo_.png", @"Assets\logo@.png", null, null, null)]
		[DataRow(@"drawable-mdpi\__2logo.png", @"Assets\2logo.png", null, null, null)]
		[DataRow(@"drawable-mdpi\__2logo_test.png", @"Assets\2logo-test.png", null, null, null)]
		[DataRow(@"drawable-mdpi\SmallTile_sdk_altform_unplated_targetsize_16.png", @"Assets\SmallTile-sdk.altform-unplated_targetsize-16.png", null, null, null)]
		[DataRow(@"drawable-mdpi\SmallTile_sdk_targetsize_16.png", @"Assets\SmallTile-sdk.targetsize-16.png", null, null, null)]
		[DataRow(@"drawable-mdpi\SmallTile_sdk_targetsize_16.9.png", @"Assets\SmallTile-sdk.targetsize-16.9.png", null, null, null)]
		[DataRow(null, @"Assets\logo.png", null, null, "uwp")]
		[DataRow(null, @"Assets\logo.png", null, null, "ios")]
		[DataRow(@"drawable-mdpi\logo.png", @"Assets\logo.png", null, null, "android")]
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
			var actualPath = AndroidResourceConverter.Convert(resourceCandidate, DefaultLanguage);
			Assert.AreEqual(expectedPath, actualPath);
		}
    }
}
