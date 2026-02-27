using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.ApplicationModel.Resources.Core;

namespace Uno.UI.Tests.Resources
{
	[TestClass]
	public class Given_ResourceCandidate
	{
		[DataRow(@"logo.png", @"logo.png", null, null, null)]
		[DataRow(@"Assets\logo.png", @"Assets\logo.png", null, null, null)]
		[DataRow(@"Assets\en\logo.png", @"Assets\logo.png", null, "en", null)]
		[DataRow(@"Assets\lang-en\logo.png", @"Assets\logo.png", null, "en", null)]
		[DataRow(@"Assets\language-en\logo.png", @"Assets\logo.png", null, "en", null)]
		[DataRow(@"Assets\logo.lang-en.png", @"Assets\logo.png", null, "en", null)]
		[DataRow(@"Assets\logo.language-en.png", @"Assets\logo.png", null, "en", null)]
		[DataRow(@"Assets\fr\logo.png", @"Assets\logo.png", null, "fr", null)]
		[DataRow(@"Assets\lang-fr\logo.png", @"Assets\logo.png", null, "fr", null)]
		[DataRow(@"Assets\language-fr\logo.png", @"Assets\logo.png", null, "fr", null)]
		[DataRow(@"Assets\logo.lang-fr.png", @"Assets\logo.png", null, "fr", null)]
		[DataRow(@"Assets\logo.language-fr.png", @"Assets\logo.png", null, "fr", null)]
		[DataRow(@"Assets\lang-fr_scale-200\logo.png", @"Assets\logo.png", "200", "fr", null)]
		[DataRow(@"Assets\logo.lang-fr_scale-200.png", @"Assets\logo.png", "200", "fr", null)]
		[DataRow(@"Assets\fr\scale-200\logo.png", @"Assets\logo.png", "200", "fr", null)]
		[DataRow(@"Assets\lang-fr\scale-200\logo.png", @"Assets\logo.png", "200", "fr", null)]
		[DataRow(@"Assets\language-fr\scale-200\logo.png", @"Assets\logo.png", "200", "fr", null)]
		[DataRow(@"Assets\language-fr_scale-200\logo.png", @"Assets\logo.png", "200", "fr", null)]
		[DataRow(@"Assets\logo.scale-100.png", @"Assets\logo.png", "100", null, null)]
		[DataRow(@"Assets\logo.scale-125.png", @"Assets\logo.png", "125", null, null)]
		[DataRow(@"Assets\logo.scale-150.png", @"Assets\logo.png", "150", null, null)]
		[DataRow(@"Assets\logo.scale-200.png", @"Assets\logo.png", "200", null, null)]
		[DataRow(@"Assets\logo.scale-300.png", @"Assets\logo.png", "300", null, null)]
		[DataRow(@"Assets\logo.scale-400.png", @"Assets\logo.png", "400", null, null)]
		[DataRow(@"Assets\scale-100\logo.png", @"Assets\logo.png", "100", null, null)]
		[DataRow(@"Assets\scale-200\logo.png", @"Assets\logo.png", "200", null, null)]
		[DataRow(@"Assets\scale-400\logo.png", @"Assets\logo.png", "400", null, null)]
		[DataRow(@"Assets\SCALE-400\logo.png", @"Assets\logo.png", "400", null, null)]
		[DataRow(@"Assets\scale-400\logo-2.png", @"Assets\logo-2.png", "400", null, null)]
		[DataRow(@"Assets\scale-400\logo.9.png", @"Assets\logo.9.png", "400", null, null)]
		[DataRow(@"Assets\logo.scale-400.9.png", @"Assets\logo.9.png", "400", null, null)]
		[DataRow(@"Assets\folder\scale-400\logo.png", @"Assets\folder\logo.png", "400", null, null)]
		[DataRow(@"Assets\scale-400\folder\logo.png", @"Assets\folder\logo.png", "400", null, null)]
		[DataRow(@"Assets\logo.custom-test.png", @"Assets\logo.png", null, null, "test")]
		[DataRow(@"Assets\custom-test\logo.png", @"Assets\logo.png", null, null, "test")]
		[DataRow(@"Assets\custom-TEST\logo.png", @"Assets\logo.png", null, null, "TEST")]
		[DataRow(@"Assets\custom-1\logo.png", @"Assets\logo.png", null, null, "1")]
		[DataRow(@"Assets\custom-test\logo.scale-200.png", @"Assets\logo.png", "200", null, "test")]
		[DataRow(@"Assets\custom-test\scale-200\logo.png", @"Assets\logo.png", "200", null, "test")]
		[DataRow(@"Assets\custom-test_scale-200\logo.png", @"Assets\logo.png", "200", null, "test")]
		[DataRow(@"Assets\en-CA\logo.png", @"Assets\logo.png", null, "en-CA", null)]
		[DataRow(@"Assets\fr-CA\logo.png", @"Assets\logo.png", null, "fr-CA", null)]
		[DataRow(@"Assets\language-fr-CA\logo.png", @"Assets\logo.png", null, "fr-CA", null)]
		[DataRow(@"Assets\lang-fr-CA\logo.png", @"Assets\logo.png", null, "fr-CA", null)]
		[DataRow(@"Assets\lang-fr-CA\scale-200\logo.png", @"Assets\logo.png", "200", "fr-CA", null)]
		[DataRow(@"Assets\lang-fr-CA_scale-200\logo.png", @"Assets\logo.png", "200", "fr-CA", null)]
		[DataRow(@"Assets\logo.lang-fr-CA.png", @"Assets\logo.png", null, "fr-CA", null)]
		[DataRow(@"Assets\logo.lang-fr-CA.scale-200.png", @"Assets\logo.png", "200", "fr-CA", null)]
		[DataRow(@"Assets\logo.scale-200.lang-fr-CA.png", @"Assets\logo.png", "200", "fr-CA", null)]
		[DataRow(@"Assets\sr-Cyrl\logo.png", @"Assets\logo.png", null, "sr-Cyrl", null)]
		[DataRow(@"fr\Assets\sr-Cyrl\logo.png", @"Assets\logo.png", null, "sr-Cyrl", null)]
		// Locales that triggered UNOB0003 due to language detection failures
		[DataRow(@"Strings\quz-PE\Resources.resw", @"Strings\Resources.resw", null, "quz-PE", null)]
		[DataRow(@"Strings\zh-CN\Resources.resw", @"Strings\Resources.resw", null, "zh-CN", null)]
		[DataRow(@"Strings\zh-TW\Resources.resw", @"Strings\Resources.resw", null, "zh-TW", null)]
		[DataRow(@"Strings\ca-Es-VALENCIA\Resources.resw", @"Strings\Resources.resw", null, "ca-Es-VALENCIA", null)]
		[DataRow(@"Strings\pa-IN\Resources.resw", @"Strings\Resources.resw", null, "pa-IN", null)]
		[DataRow(@"Strings\zh-Hans\Resources.resw", @"Strings\Resources.resw", null, "zh-Hans", null)]
		[DataRow(@"Strings\zh-Hant\Resources.resw", @"Strings\Resources.resw", null, "zh-Hant", null)]
		[TestMethod]
		public void When_Parse(string relativePath, string logicalPath, string scale, string language, string custom)
		{
			var resourceCandidate = ResourceCandidate.Parse(null, relativePath);
			Assert.AreEqual(logicalPath, resourceCandidate.LogicalPath);
			Assert.AreEqual(scale, resourceCandidate.GetQualifierValue("scale"));
			Assert.AreEqual(language, resourceCandidate.GetQualifierValue("language"));
			Assert.AreEqual(custom, resourceCandidate.GetQualifierValue("custom"));
		}
	}
}
