using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.ApplicationModel.Resources.Core;

namespace Uno.UI.Tests.Resources
{
	[TestClass]
    public class Given_AndroidResourceConverter
    {
		private const string DefaultLanguage = "en";

		[DataRow(@"drawable-nodpi\logo.png", @"logo.png", null, null, null)]
		[DataRow(@"drawable-nodpi\logo.png", @"Assets\logo.png", null, null, null)]
		[DataRow(@"drawable-nodpi\logo.png", @"Assets\logo.png", null, "en", null)]
		[DataRow(@"drawable-fr-nodpi\logo.png", @"Assets\logo.png", null, "fr", null)]
		[DataRow(@"drawable-fr-xhdpi\logo.png", @"Assets\logo.png", 200, "fr", null)]
		[DataRow(@"drawable-fr-rCA-xhdpi\logo.png", @"Assets\logo.png", 200, "fr-CA", null)]
		[DataRow(@"drawable-mdpi\logo.png", @"Assets\logo.png", 100, null, null)]
		[DataRow(null, @"Assets\logo.png", 125, null, null)]
		[DataRow(@"drawable-hdpi\logo.png", @"Assets\logo.png", 150, null, null)]
		[DataRow(@"drawable-xhdpi\logo.png", @"Assets\logo.png", 200, null, null)]
		[DataRow(@"drawable-xxhdpi\logo.png", @"Assets\logo.png", 300, null, null)]
		[DataRow(@"drawable-xxxhdpi\logo.png", @"Assets\logo.png", 400, null, null)]
		[DataRow(@"drawable-nodpi\logo.9.png", @"Assets\logo.9.png", null, null, null)]
		[DataRow(@"drawable-nodpi\logo_.png", @"Assets\logo-.png", null, null, null)]
		[DataRow(@"drawable-nodpi\logo_.png", @"Assets\logo@.png", null, null, null)]
		[DataRow(@"drawable-nodpi\__2logo.png", @"Assets\2logo.png", null, null, null)]
		[DataRow(@"drawable-nodpi\__2logo_test.png", @"Assets\2logo-test.png", null, null, null)]
		[DataRow(@"drawable-nodpi\SmallTile_sdk_altform_unplated_targetsize_16.png", @"Assets\SmallTile-sdk.altform-unplated_targetsize-16.png", null, null, null)]
		[DataRow(@"drawable-nodpi\SmallTile_sdk_targetsize_16.png", @"Assets\SmallTile-sdk.targetsize-16.png", null, null, null)]
		[DataRow(@"drawable-nodpi\SmallTile_sdk_targetsize_16.9.png", @"Assets\SmallTile-sdk.targetsize-16.9.png", null, null, null)]
		[DataRow(null, @"Assets\logo.png", null, null, "uwp")]
		[DataRow(null, @"Assets\logo.png", null, null, "ios")]
		[DataRow(@"drawable-nodpi\logo.png", @"Assets\logo.png", null, null, "android")]
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

	[TestClass]
	public class Given_AndroidResourceNameEncoder
	{
		[TestMethod]
		[DataRow(@"logo", @"logo")]
		public void When_Encode(string input, string expected)
		{
			Assert.AreEqual(expected, AndroidResourceNameEncoder.Encode(input));
		}

		[TestMethod]

		[DataRow(@"logo.png", @"logo.png")]
		[DataRow(@"test/logo.png", @"test/logo.png")]
		[DataRow(@"test/logo-1.png", @"test/logo_1.png")]
		[DataRow(@"test/logo-1.png", @"test/logo_1.png")]
		[DataRow(@"test/test2/logo-1.png", @"test/test2/logo_1.png")]
		[DataRow(@"test/test2/test-3/logo-1.png", @"test/test2/test_3/logo_1.png")]
		[DataRow(@"1test/logo-1.png", @"__1test/logo_1.png")]
		public void When_EncodeResourcePath(string input, string expected)
		{
			Assert.AreEqual(expected, AndroidResourceNameEncoder.EncodeResourcePath(input));
		}

		[TestMethod]

		[DataRow(@"logo.png", @"Assets\logo.png")]
		[DataRow(@"test\logo.png", @"Assets\test\logo.png")]
		[DataRow(@"test\logo-1.png", @"Assets\test\logo_1.png")]
		[DataRow(@"test\logo-1.png", @"Assets\test\logo_1.png")]
		[DataRow(@"test\test2\logo-1.png", @"Assets\test\test2\logo_1.png")]
		[DataRow(@"test\test2\test-3\logo-1.png", @"Assets\test\test2\test_3\logo_1.png")]
		[DataRow(@"1test\logo-1.png", @"Assets\__1test\logo_1.png")]
		public void When_EncodeFileSystemPath(string input, string expected)
		{
			Assert.AreEqual(expected, AndroidResourceNameEncoder.EncodeFileSystemPath(input));
		}
	}
}
