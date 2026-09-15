using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.ApplicationModel.Resources.Core;

namespace Uno.UI.Tests.Resources
{
	[TestClass]
	public class Given_AndroidResourceConverter
	{
		private const string DefaultLanguage = "en";

		[DataRow(@"drawable-nodpi\logo_png.png", @"logo.png", null, null, null)]
		[DataRow(@"drawable-nodpi\Assets_logo_png.png", @"Assets\logo.png", null, null, null)]
		[DataRow(@"drawable-nodpi\Assets_logo_png.png", @"Assets\logo.png", null, "en", null)]
		[DataRow(@"drawable-fr-nodpi\Assets_logo_png.png", @"Assets\logo.png", null, "fr", null)]
		[DataRow(@"drawable-fr-xhdpi\Assets_logo_png.png", @"Assets\logo.png", 200, "fr", null)]
		[DataRow(@"drawable-fr-rCA-xhdpi\Assets_logo_png.png", @"Assets\logo.png", 200, "fr-CA", null)]
		[DataRow(@"drawable-mdpi\Assets_logo_png.png", @"Assets\logo.png", 100, null, null)]
		[DataRow(null, @"Assets\logo.png", 125, null, null)]
		[DataRow(@"drawable-hdpi\Assets_logo_png.png", @"Assets\logo.png", 150, null, null)]
		[DataRow(@"drawable-xhdpi\Assets_logo_png.png", @"Assets\logo.png", 200, null, null)]
		[DataRow(@"drawable-xxhdpi\Assets_logo_png.png", @"Assets\logo.png", 300, null, null)]
		[DataRow(@"drawable-xxxhdpi\Assets_logo_png.png", @"Assets\logo.png", 400, null, null)]
		[DataRow(@"drawable-nodpi\Assets_logo_png.9.png", @"Assets\logo.9.png", null, null, null)]
		[DataRow(@"drawable-nodpi\Assets_logo__png.png", @"Assets\logo-.png", null, null, null)]
		[DataRow(@"drawable-nodpi\Assets_logo__png.png", @"Assets\logo@.png", null, null, null)]
		[DataRow(@"drawable-nodpi\Assets___2logo_png.png", @"Assets\2logo.png", null, null, null)]
		[DataRow(@"drawable-nodpi\Assets___2logo_test_png.png", @"Assets\2logo-test.png", null, null, null)]
		[DataRow(@"drawable-nodpi\Assets_SmallTile_sdk_altform_unplated_targetsize_16_png.png", @"Assets\SmallTile-sdk.altform-unplated_targetsize-16.png", null, null, null)]
		[DataRow(@"drawable-nodpi\Assets_SmallTile_sdk_targetsize_16_png.png", @"Assets\SmallTile-sdk.targetsize-16.png", null, null, null)]
		[DataRow(@"drawable-nodpi\Assets_SmallTile_sdk_targetsize_16_png.9.png", @"Assets\SmallTile-sdk.targetsize-16.9.png", null, null, null)]
		[DataRow(null, @"Assets\logo.png", null, null, "uwp")]
		[DataRow(null, @"Assets\logo.png", null, null, "ios")]
		[DataRow(@"drawable-nodpi\Assets_logo_png.png", @"Assets\logo.png", null, null, "android")]
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
		[DataRow(@"", @"")]
		[DataRow(null, @"")]
		[DataRow(" ", @"_")]
		[DataRow("  ", @"__")]
		public void When_Encode(string input, string expected)
		{
			Assert.AreEqual(expected, AndroidResourceNameEncoder.Encode(input));
		}

		[TestMethod]
		[DataRow(@"logo.png", @"logo.png")]
		[DataRow(@"test/logo.png", @"test/logo.png")]
		[DataRow(@"test/logo-1.png", @"test/logo_1.png")]
		[DataRow(@"test/test2/logo-1.png", @"test/test2/logo_1.png")]
		[DataRow(@"test/test2/test-3/logo-1.png", @"test/test2/test_3/logo_1.png")]
		[DataRow(@"1test/logo-1.png", @"__1test/logo_1.png")]
		[DataRow(@"test/logo.9.png", @"test/logo.9.png")]
		[DataRow(@".png", @".png")]
		[DataRow(@"test space/logo.png", @"test_space/logo.png")]
		[DataRow(@"test space/.png", @"test_space/.png")]
		public void When_EncodeResourcePath(string input, string expected)
		{
			Assert.AreEqual(expected, AndroidResourceNameEncoder.EncodeResourcePath(input));
		}

		[TestMethod]
		[DataRow(@"logo.png", @"Assets\logo.png")]
		[DataRow(@"test\logo.png", @"Assets\test\logo.png")]
		[DataRow(@"test\logo-1.png", @"Assets\test\logo_1.png")]
		[DataRow(@"test\test2\logo-1.png", @"Assets\test\test2\logo_1.png")]
		[DataRow(@"test\test2\test-3\logo-1.png", @"Assets\test\test2\test_3\logo_1.png")]
		[DataRow(@"1test\logo-1.png", @"Assets\__1test\logo_1.png")]
		[DataRow(@"test\logo.9.png", @"Assets\test\logo.9.png")]
		[DataRow(@".png", @"Assets\.png")]
		[DataRow(@"test\.png", @"Assets\test\.png")]
		[DataRow(@"test with spaces\.png", @"Assets\test_with_spaces\.png")]
		public void When_EncodeFileSystemPath(string input, string expected)
		{
			Assert.AreEqual(expected, AndroidResourceNameEncoder.EncodeFileSystemPath(input));
		}

		[TestMethod]
		[DataRow(@"logo.png", @"logo_png.png")]
		[DataRow(@"test/logo.png", @"test_logo_png.png")]
		[DataRow(@"test/logo-1.png", @"test_logo_1_png.png")]
		[DataRow(@"test/test2/logo-1.png", @"test_test2_logo_1_png.png")]
		[DataRow(@"test/test2/test-3/logo-1.png", @"test_test2_test_3_logo_1_png.png")]
		[DataRow(@"test/logo.jpg", @"test_logo_jpg.jpg")]
		[DataRow(@"test/logo.9.png", @"test_logo_png.9.png")]
		[DataRow(@"test/logo.9.PNG", @"test_logo_PNG.9.PNG")]
		[DataRow(@"test/SmallTile-sdk.targetsize-16.9.png", @"test_SmallTile_sdk_targetsize_16_png.9.png")]
		[DataRow(@"v1.9/logo.png", @"v1_9_logo_png.png")]
		[DataRow(@"v1.9/logo.9.png", @"v1_9_logo_png.9.png")]
		[DataRow(@".png", @"_png.png")]
		[DataRow(@"test/.png", @"test__png.png")]
		[DataRow(@"test with spaces/.png", @"test_with_spaces__png.png")]
		public void When_EncodeDrawablePath(string input, string expected)
		{
			Assert.AreEqual(expected, AndroidResourceNameEncoder.EncodeDrawablePath(input));
		}

		[TestMethod]
		[DataRow(@"logo.png", @"logo_png")]
		[DataRow(@"Assets/logo.png", @"Assets_logo_png")]
		[DataRow(@"Assets/logo.jpg", @"Assets_logo_jpg")]
		[DataRow(@"Assets/sub/logo-1.png", @"Assets_sub_logo_1_png")]
		[DataRow(@"Assets/logo.9.png", @"Assets_logo_png")]
		[DataRow(@"Assets/2logo.png", @"Assets___2logo_png")]
		[DataRow(@"Assets/v1.9/logo.png", @"Assets_v1_9_logo_png")]
		// DrawableHelper.FindResourceId re-encodes the name it gets from FindResourceIdFromPath
		[DataRow(@"Assets_logo_png", @"Assets_logo_png")]
		public void When_EncodeDrawableResourceName(string input, string expected)
		{
			Assert.AreEqual(expected, AndroidResourceNameEncoder.EncodeDrawableResourceName(input));
		}

		[TestMethod]
		[DataRow(@"Assets/logo.png")]
		[DataRow(@"Assets/logo.jpg")]
		[DataRow(@"Assets/logo.9.png")]
		[DataRow(@"Assets/SmallTile-sdk.targetsize-16.9.png")]
		[DataRow(@"Assets/2logo.png")]
		[DataRow(@"Assets/v1.9/logo.png")]
		[DataRow(@"Assets/v1.9/logo.9.png")]
		public void When_EncodeDrawablePath_Then_ResourceNameMatches(string input)
		{
			// Android names a drawable after its file name up to the first dot, so the name
			// used to look the resource up at runtime has to match that exactly.
			var expected = AndroidResourceNameEncoder.EncodeDrawablePath(input).Split('.')[0];

			Assert.AreEqual(expected, AndroidResourceNameEncoder.EncodeDrawableResourceName(input));
		}
	}
}
