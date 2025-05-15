using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using Windows.ApplicationModel.Resources;
using Windows.ApplicationModel.Resources.Core;
using Windows.Globalization;

namespace Uno.UI.RuntimeTests.Tests
{
	[TestClass]
	public class Given_ResourceLoader
	{
		private const string DefaultLanguage = "en-US";

		[TestInitialize]
		public void Init()
		{
			CultureInfo.CurrentUICulture = new CultureInfo(DefaultLanguage);
			ApplicationLanguages.PrimaryLanguageOverride = DefaultLanguage;
#if __XAMARIN__ || __WASM__
			ResourceLoader.DefaultLanguage = DefaultLanguage;
#endif
		}

		[TestCleanup]
		public void Cleanup()
		{
			CultureInfo.CurrentUICulture = new CultureInfo(DefaultLanguage);
			ApplicationLanguages.PrimaryLanguageOverride = DefaultLanguage;
#if __XAMARIN__ || __WASM__
			ResourceLoader.DefaultLanguage = DefaultLanguage;
#endif
		}

		[TestMethod]
		public void When_SimpleString()
		{
			var SUT = ResourceLoader.GetForViewIndependentUse();
			Assert.AreEqual("SamplesApp", SUT.GetString("ApplicationName"));
			Assert.AreEqual("My Simple String (en-US)", SUT.GetString("Given_ResourceLoader/When_SimpleString"));
		}

		[TestMethod]
		public void When_UnnamedLoader()
		{
			var SUT = ResourceLoader.GetForViewIndependentUse();
			Assert.AreEqual(@"This is en-US\Resources.resw", SUT.GetString("Given_ResourceLoader/When_NamedLoader"));
		}

		[TestMethod]
		public void When_PrefixUnnamedLoader()
		{
			var SUT = ResourceLoader.GetForViewIndependentUse();
			Assert.AreEqual(@"This is prefixed en\Test01.resw", SUT.GetString("Prefix/Given_ResourceLoader/When_PrefixedNamedLoader"));
		}

		[TestMethod]
		public void When_PrefixNamedLoader()
		{
			var SUT = ResourceLoader.GetForViewIndependentUse("Test01");
			Assert.AreEqual(@"This is prefixed en-US\Test01.resw", SUT.GetString("Prefix/Given_ResourceLoader/When_PrefixedNamedLoader"));
		}

		[TestMethod]
		public void When_NamedLoader_Resources()
		{
			var SUT = ResourceLoader.GetForViewIndependentUse("Resources");
			Assert.AreEqual(@"This is en-US\Resources.resw", SUT.GetString("Given_ResourceLoader/When_NamedLoader"));
		}

		[TestMethod]
		public void When_NamedLoader_Test01()
		{
			var SUT = ResourceLoader.GetForViewIndependentUse("Test01");
			Assert.AreEqual(@"This is en-US\Test01.resw", SUT.GetString("Given_ResourceLoader/When_NamedLoader"));
		}

		[TestMethod]
		public void When_UnnamedLoader_Test01_Only()
		{
			var SUT = ResourceLoader.GetForViewIndependentUse();
			Assert.AreEqual("", SUT.GetString("Given_ResourceLoader/When_NamedLoader_Test01_Only"));
		}

		[TestMethod]
		public void When_NamedLoader_Test01_Only()
		{
			var SUT = ResourceLoader.GetForViewIndependentUse("Test01");
			Assert.AreEqual(@"This is en-US\Test01.resw only", SUT.GetString("Given_ResourceLoader/When_NamedLoader_Test01_Only"));
		}

		[TestMethod]
		public void When_Assembly_NamedLoader_TopLevelNamedRuntimeTests()
		{
			var SUT = ResourceLoader.GetForViewIndependentUse("Uno.UI.RuntimeTests/TopLevelNamedRuntimeTests");
			Assert.AreEqual(@"en-US Value for When_xUid_Explicit in TopLevelNamedRuntimeTests", SUT.GetString("When_xUid_Explicit/Text"));
		}

		[TestMethod]
		public void When_Assembly_NamedLoader_Resources()
		{
			var SUT = ResourceLoader.GetForViewIndependentUse("Uno.UI.RuntimeTests/Resources");
			Assert.AreEqual(@"RuntimeTest Additional Resource", SUT.GetString("RuntimeTests_AdditionalResource"));
		}

		[TestMethod]
		public void When_Assembly_NamedLoader_Resources_With_Prefix()
		{
			var SUT = ResourceLoader.GetForViewIndependentUse("Uno.UI.RuntimeTests/Resources");
			Assert.AreEqual(@"en-US Value for SomePrefix/When_xUid_With_Prefix", SUT.GetString("SomePrefix/When_xUid_With_Prefix/Text"));
		}

		[TestMethod]
		public void When_UnnamedLoader_UnknownResource()
		{
			var SUT = ResourceLoader.GetForViewIndependentUse();
			Assert.AreEqual(@"", SUT.GetString("Given_ResourceLoader/INVALID_RESOURCE_NAME"));
		}

		[TestMethod]
		public void When_NamedLoader_UnknownResource()
		{
			var SUT = ResourceLoader.GetForViewIndependentUse("Test01");
			Assert.AreEqual(@"", SUT.GetString("Given_ResourceLoader/INVALID_RESOURCE_NAME"));
		}

		[TestMethod]
		public void When_LocalizedResource()
		{
			var SUT = ResourceLoader.GetForViewIndependentUse();
			var languages = new[] { "fr-CA", "fr", "en-US", "en", "sr" };

			foreach (var language in languages)
			{
				ApplicationLanguages.PrimaryLanguageOverride = language;
				Assert.AreEqual($@"Text in '{language}'", SUT.GetString("Given_ResourceLoader/When_LocalizedResource"));
			}
		}

		[TestMethod]
		public void When_MissingLocalizedResource_FallbackOnParent()
		{
			var SUT = ResourceLoader.GetForViewIndependentUse();

			ApplicationLanguages.PrimaryLanguageOverride = "fr-FR";
			Assert.AreEqual(@"Text in 'fr'", SUT.GetString("Given_ResourceLoader/When_LocalizedResource"));
		}

		[TestMethod]
		public void When_MissingLocalizedResource_FallbackOnRegional()
		{
			var SUT = ResourceLoader.GetForViewIndependentUse();

			ApplicationLanguages.PrimaryLanguageOverride = "es";
			Assert.AreEqual(@"Text in 'es-MX'", SUT.GetString("Given_ResourceLoader/When_LocalizedResource"));
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaIOS)] // Unstable test due to device language settings: NSLocale.PreferredLanguages can be 'en' or 'en-US'.
#if __APPLE_UIKIT__
		[Ignore("Unstable test due to device language settings: NSLocale.PreferredLanguages can be 'en' or 'en-US'.")]
#endif
		public void When_MissingLocalizedResource_FallbackOnDefault()
		{
			var SUT = ResourceLoader.GetForViewIndependentUse();

			ApplicationLanguages.PrimaryLanguageOverride = "de-DE";
			Assert.AreEqual(@"Text in 'en-US'", SUT.GetString("Given_ResourceLoader/When_LocalizedResource"));
		}

		[TestMethod]
		public void When_FileAndStringNameFormat()
		{
			var SUT = ResourceLoader.GetForViewIndependentUse();

			Assert.AreEqual(@"Uniq text from Resources", SUT.GetString("pkResUniqResources"));
			Assert.AreEqual(@"Shared text from Resources", SUT.GetString("pkResShared"));
			Assert.AreEqual(@"Shared text from Test01", SUT.GetString("/Test01/pkResShared"));
			Assert.AreEqual(@"Uniq text from Test01", SUT.GetString("/Test01/pkResUniqTest01"));
			Assert.AreEqual(@"", SUT.GetString("/this-does-not-exist"));
			Assert.AreEqual(@"", SUT.GetString("//this-does-not-exist"));
		}
	}
}
