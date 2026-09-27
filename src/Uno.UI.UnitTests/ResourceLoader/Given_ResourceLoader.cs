using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Tests.ResourceLoader.Controls;
using Windows.Globalization;
using _ResourceLoader = Windows.ApplicationModel.Resources.ResourceLoader;

namespace Uno.UI.Tests.ResourceLoaderTests
{
	[TestClass]
	public class Given_ResourceLoader
	{
		private const string DefaultLanguage = "en-US";
		private const string UITestResources = "Uno.UI.UnitTests/Resources";

		// The same strings, localized once under script-named folders (zh-Hans/zh-Hant, the WinUI
		// convention) and once under region-named ones (zh-CN/zh-TW). Both must resolve identically.
		private const string ScriptNamedResources = "Uno.UI.UnitTests/ZhScriptFolders";
		private const string RegionNamedResources = "Uno.UI.UnitTests/ZhRegionFolders";

		// A bare zh folder (Simplified) alongside two Traditional ones, zh-HK and zh-Hant-TW.
		private const string MixedResources = "Uno.UI.UnitTests/ZhMixedFolders";

		[TestInitialize]
		public void Init()
		{
			CultureInfo.CurrentUICulture = new CultureInfo(DefaultLanguage);
			ApplicationLanguages.PrimaryLanguageOverride = DefaultLanguage;
			_ResourceLoader.DefaultLanguage = DefaultLanguage;

			_ResourceLoader.AddLookupAssembly(GetType().Assembly);
		}

		[TestCleanup]
		public void Cleanup()
		{
			CultureInfo.CurrentUICulture = new CultureInfo(DefaultLanguage);
			ApplicationLanguages.PrimaryLanguageOverride = DefaultLanguage;
			_ResourceLoader.DefaultLanguage = DefaultLanguage;
		}

		[TestMethod]
		public void When_ResourceFile_Neutral()
		{
			_ResourceLoader.DefaultLanguage = "en";

			Assert.AreEqual("App70-en", _ResourceLoader.GetForCurrentView(UITestResources).GetString("ApplicationName"));
		}

		[TestMethod]
		public void When_Empty_Resource()
		{
			_ResourceLoader.DefaultLanguage = "en";

			Assert.AreEqual("", _ResourceLoader.GetForCurrentView(UITestResources).GetString("TestEmptyResource"));
		}

		[TestMethod]
		public void When_ResourceFile_Neutral_Both()
		{
			void setResources(string language)
			{
				ApplicationLanguages.PrimaryLanguageOverride = language;
				_ResourceLoader.DefaultLanguage = language;
			}

			setResources("fr");
			Assert.AreEqual("App70-fr", _ResourceLoader.GetForCurrentView(UITestResources).GetString("ApplicationName"));

			setResources("fr-FR");
			Assert.AreEqual("App70-fr", _ResourceLoader.GetForCurrentView(UITestResources).GetString("ApplicationName"));

			setResources("en");
			Assert.AreEqual("App70-en", _ResourceLoader.GetForCurrentView(UITestResources).GetString("ApplicationName"));
		}

		[TestMethod]
		public void When_MissingLocalizedResource_FallbackOnParent()
		{
			var SUT = _ResourceLoader.GetForCurrentView(UITestResources);

			ApplicationLanguages.PrimaryLanguageOverride = "fr-FR";
			Assert.AreEqual(@"Text in 'fr'", SUT.GetString("Given_ResourceLoader/When_LocalizedResource"));
		}

		[TestMethod]
		public void When_MissingLocalizedResource_FallbackOnDefault()
		{
			var SUT = _ResourceLoader.GetForCurrentView(UITestResources);

			ApplicationLanguages.PrimaryLanguageOverride = "de-DE";
			Assert.AreEqual(@"Text in 'en'", SUT.GetString("Given_ResourceLoader/When_LocalizedResource"));
		}

		[TestMethod]
		[DataRow("zh-CN")]
		[DataRow("zh-Hans")]
		[DataRow("zh-Hans-CN")]
		[DataRow("zh-SG")]
		[DataRow("zh")]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24024")]
		public void When_SimplifiedChinese_And_ScriptNamedFolders(string language)
		{
			var SUT = _ResourceLoader.GetForCurrentView(ScriptNamedResources);

			ApplicationLanguages.PrimaryLanguageOverride = language;
			Assert.AreEqual("Simplified", SUT.GetString("Given_ResourceLoader/When_ChineseScript"));
		}

		[TestMethod]
		[DataRow("zh-TW")]
		[DataRow("zh-Hant")]
		[DataRow("zh-Hant-TW")]
		[DataRow("zh-HK")]
		[DataRow("zh-MO")]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24024")]
		public void When_TraditionalChinese_And_ScriptNamedFolders(string language)
		{
			var SUT = _ResourceLoader.GetForCurrentView(ScriptNamedResources);

			ApplicationLanguages.PrimaryLanguageOverride = language;
			Assert.AreEqual("Traditional", SUT.GetString("Given_ResourceLoader/When_ChineseScript"));
		}

		[TestMethod]
		[DataRow("zh-CN")]
		[DataRow("zh-Hans")]
		[DataRow("zh-Hans-CN")]
		[DataRow("zh-SG")]
		[DataRow("zh")]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24024")]
		public void When_SimplifiedChinese_And_RegionNamedFolders(string language)
		{
			var SUT = _ResourceLoader.GetForCurrentView(RegionNamedResources);

			ApplicationLanguages.PrimaryLanguageOverride = language;
			Assert.AreEqual("Simplified", SUT.GetString("Given_ResourceLoader/When_ChineseScript"));
		}

		[TestMethod]
		[DataRow("zh-TW")]
		[DataRow("zh-Hant")]
		[DataRow("zh-Hant-TW")]
		[DataRow("zh-HK")]
		[DataRow("zh-MO")]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24024")]
		public void When_TraditionalChinese_And_RegionNamedFolders(string language)
		{
			var SUT = _ResourceLoader.GetForCurrentView(RegionNamedResources);

			ApplicationLanguages.PrimaryLanguageOverride = language;
			Assert.AreEqual("Traditional", SUT.GetString("Given_ResourceLoader/When_ChineseScript"));
		}

		[TestMethod]
		[DataRow("zh-CN", "Simplified")]
		[DataRow("zh-Hans", "Simplified")]
		[DataRow("zh", "Simplified")]
		[DataRow("zh-TW", "Taiwan")] // zh is a nearer ancestor, but it is Simplified
		[DataRow("zh-Hant", "Taiwan")] // both siblings are Traditional, ordinal order picks TW
		[DataRow("zh-MO", "Taiwan")]
		[DataRow("zh-Hant-HK", "Hong Kong")] // same script, and HK is the matching region
		[DataRow("zh-HK", "Hong Kong")]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24024")]
		public void When_Chinese_And_MixedFolders(string language, string expected)
		{
			var SUT = _ResourceLoader.GetForCurrentView(MixedResources);

			ApplicationLanguages.PrimaryLanguageOverride = language;
			Assert.AreEqual(expected, SUT.GetString("Given_ResourceLoader/When_ChineseMixedFolders"));
		}

		[TestMethod]
		public void When_Collection_And_InlineProperty()
		{
			var SUT = new When_Collection_And_InlineProperty();

			Assert.AreEqual(@"Header in 'en'", SUT.rb.Header);
		}

		[TestMethod]
		public void When_String_Constructor_Used()
		{
			_ResourceLoader.DefaultLanguage = "en";
			var SUT = new _ResourceLoader(UITestResources);

			Assert.AreEqual("App70-en", SUT.GetString("ApplicationName"));
		}
	}
}
