using System;
using System.Globalization;
using System.Linq;
using Windows.Globalization;
using Windows.Storage;

namespace Uno.UI.RuntimeTests.Tests.Windows_Globalization;

[TestClass]
public class Given_ApplicationLanguages
{
	[TestCleanup]
	public void CleanUp()
	{
		ApplicationLanguages.PrimaryLanguageOverride = string.Empty;
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void Test_Chinese_With_Script_Subtag()
	{
		ApplicationLanguages.PrimaryLanguageOverride = "zh-Hans-CN";
		ApplicationLanguages.Languages[0].Should().Be("zh-Hans-CN");
		ApplicationData.Current.LocalSettings.Values["__Uno.PrimaryLanguageOverride"].Should().Be("zh-Hans-CN");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void Test_French_With_Script_Subtag()
	{
		ApplicationLanguages.PrimaryLanguageOverride = "fr-Latn-CA";
		ApplicationLanguages.Languages[0].Should().Be("fr-Latn-CA");
		ApplicationData.Current.LocalSettings.Values["__Uno.PrimaryLanguageOverride"].Should().Be("fr-Latn-CA");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/13704")]
	public void When_PrimaryLanguageOverride_Then_Culture_Unchanged()
	{
		var culture = CultureInfo.CurrentCulture;
		var uiCulture = CultureInfo.CurrentUICulture;
		var threadCulture = CultureInfo.DefaultThreadCurrentCulture;
		var threadUICulture = CultureInfo.DefaultThreadCurrentUICulture;

		// Picked relative to the ambient culture so the assertion below cannot pass by coincidence.
		var language = culture.Name.StartsWith("fr", StringComparison.OrdinalIgnoreCase) ? "de-DE" : "fr-CA";

		try
		{
			ApplicationLanguages.PrimaryLanguageOverride = language;

			// The override still drives resource resolution immediately...
			ApplicationLanguages.Languages[0].Should().Be(language);

			// ...but the culture only follows on the next app start, as on WinUI.
			CultureInfo.CurrentCulture.Name.Should().Be(culture.Name);
			CultureInfo.CurrentUICulture.Name.Should().Be(uiCulture.Name);
		}
		finally
		{
			ApplicationLanguages.PrimaryLanguageOverride = string.Empty;
			CultureInfo.CurrentCulture = culture;
			CultureInfo.CurrentUICulture = uiCulture;
			CultureInfo.DefaultThreadCurrentCulture = threadCulture;
			CultureInfo.DefaultThreadCurrentUICulture = threadUICulture;
		}
	}

#if HAS_UNO // ApplyCulture is what the Uno Application constructor calls.
	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24761")]
	[DataRow(false)]
	[DataRow(true)]
	public void When_ApplyCulture_Then_Only_UICulture_Follows_Languages(bool usePrimaryLanguageOverride)
	{
		var culture = CultureInfo.CurrentCulture;
		var uiCulture = CultureInfo.CurrentUICulture;
		var threadCulture = CultureInfo.DefaultThreadCurrentCulture;
		var threadUICulture = CultureInfo.DefaultThreadCurrentUICulture;

		try
		{
			var language = ApplicationLanguages.Languages[0];
			if (usePrimaryLanguageOverride)
			{
				language = language.StartsWith("fr", StringComparison.OrdinalIgnoreCase) ? "de-DE" : "fr-CA";
				ApplicationLanguages.PrimaryLanguageOverride = language;
			}

			// Stands for the culture the runtime read from the regional settings, which differs from the language.
			var formatCulture = new[] { "es-MX", "it-IT", "pt-BR" }
				.Select(name => new CultureInfo(name))
				.First(c => !language.StartsWith(c.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase) && c.Name != CultureInfo.InstalledUICulture.Name);
			CultureInfo.CurrentCulture = formatCulture;
			CultureInfo.DefaultThreadCurrentCulture = formatCulture;

			ApplicationLanguages.ApplyCulture();

			var expectedUICulture = new CultureInfo(language).Name;
			CultureInfo.CurrentUICulture.Name.Should().Be(expectedUICulture);
			(CultureInfo.DefaultThreadCurrentUICulture?.Name).Should().Be(expectedUICulture);
			CultureInfo.CurrentCulture.Should().BeSameAs(formatCulture);
			CultureInfo.DefaultThreadCurrentCulture.Should().BeSameAs(formatCulture);
		}
		finally
		{
			ApplicationLanguages.PrimaryLanguageOverride = string.Empty;
			CultureInfo.CurrentCulture = culture;
			CultureInfo.CurrentUICulture = uiCulture;
			CultureInfo.DefaultThreadCurrentCulture = threadCulture;
			CultureInfo.DefaultThreadCurrentUICulture = threadUICulture;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24761")]
	public void When_ApplyCulture_With_Runtime_Culture_Then_Culture_Follows_System_Locale_On_Android_Only()
	{
		var culture = CultureInfo.CurrentCulture;
		var uiCulture = CultureInfo.CurrentUICulture;
		var threadCulture = CultureInfo.DefaultThreadCurrentCulture;
		var threadUICulture = CultureInfo.DefaultThreadCurrentUICulture;

		try
		{
			if (!OperatingSystem.IsAndroid() && CultureInfo.InstalledUICulture.Name.Length == 0)
			{
				Assert.Inconclusive("The runtime found no user locale; When_ApplyCulture_Without_User_Locale_Then_Culture_Follows_Language covers it.");
			}

			// The culture the runtime picked at startup, before the app set one of its own.
			var runtimeCulture = CultureInfo.InstalledUICulture;
			CultureInfo.CurrentCulture = runtimeCulture;
			CultureInfo.DefaultThreadCurrentCulture = null;

			ApplicationLanguages.ApplyCulture();

			if (OperatingSystem.IsAndroid())
			{
				// The system locale keeps the -u- extensions (regional preferences) that the runtime's culture drops.
				var systemCulture = new CultureInfo(ApplicationLanguages.ManifestLanguages[0]).Name;
				CultureInfo.CurrentCulture.Name.Should().Be(systemCulture);
				(CultureInfo.DefaultThreadCurrentCulture?.Name).Should().Be(systemCulture);
			}
			else
			{
				CultureInfo.CurrentCulture.Should().BeSameAs(runtimeCulture);
				CultureInfo.DefaultThreadCurrentCulture.Should().BeNull();
			}
		}
		finally
		{
			CultureInfo.CurrentCulture = culture;
			CultureInfo.CurrentUICulture = uiCulture;
			CultureInfo.DefaultThreadCurrentCulture = threadCulture;
			CultureInfo.DefaultThreadCurrentUICulture = threadUICulture;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI | RuntimeTestPlatforms.SkiaAndroid)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24761")]
	public void When_ApplyCulture_Without_User_Locale_Then_Culture_Follows_Language()
	{
		if (ApplicationLanguages.InvariantCulture)
		{
			Assert.Inconclusive("Invariant globalization mode has no culture to fall back to.");
		}

		// What the runtime holds when it found no user locale: with LANG unset or C on Linux, a read-only
		// invariant culture of its own.
		var runtimeCulture = CultureInfo.ReadOnly(new CultureInfo(""));
		var uiCulture = new CultureInfo("fr-CA");

		ApplicationLanguages.GetFormatCulture(uiCulture, runtimeCulture, runtimeCulture).Should().BeSameAs(uiCulture);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24761")]
	[DataRow(false)]
	[DataRow(true)]
	public void When_App_Customized_The_Runtime_Culture_Then_Culture_Kept(bool runtimeFoundNoLocale)
	{
		var culture = CultureInfo.CurrentCulture;
		var uiCulture = CultureInfo.CurrentUICulture;
		var threadCulture = CultureInfo.DefaultThreadCurrentCulture;
		var threadUICulture = CultureInfo.DefaultThreadCurrentUICulture;

		try
		{
			var runtimeCulture = runtimeFoundNoLocale ? CultureInfo.ReadOnly(new CultureInfo("")) : CultureInfo.InstalledUICulture;

			// Same name as the runtime's culture, customized by the app before the Application constructor.
			var appCulture = (CultureInfo)runtimeCulture.Clone();
			appCulture.DateTimeFormat.ShortDatePattern = "yyyy'|'MM'|'dd";

			ApplicationLanguages.GetFormatCulture(new CultureInfo("fr-CA"), appCulture, runtimeCulture).Should().BeNull();

			if (!runtimeFoundNoLocale)
			{
				CultureInfo.CurrentCulture = appCulture;

				ApplicationLanguages.ApplyCulture();

				CultureInfo.CurrentCulture.Should().BeSameAs(appCulture);
			}
		}
		finally
		{
			CultureInfo.CurrentCulture = culture;
			CultureInfo.CurrentUICulture = uiCulture;
			CultureInfo.DefaultThreadCurrentCulture = threadCulture;
			CultureInfo.DefaultThreadCurrentUICulture = threadUICulture;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24761")]
	public void When_App_Set_Invariant_Culture_Then_Culture_Kept()
	{
		if (ReferenceEquals(CultureInfo.InstalledUICulture, CultureInfo.InvariantCulture))
		{
			// Invariant mode, or a locale the runtime couldn't read or create.
			Assert.Inconclusive("The runtime's own culture is the invariant singleton, so an app setting it can't be told apart.");
		}

		var culture = CultureInfo.CurrentCulture;
		var uiCulture = CultureInfo.CurrentUICulture;
		var threadCulture = CultureInfo.DefaultThreadCurrentCulture;
		var threadUICulture = CultureInfo.DefaultThreadCurrentUICulture;

		try
		{
			CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

			ApplicationLanguages.ApplyCulture();

			CultureInfo.CurrentCulture.Should().BeSameAs(CultureInfo.InvariantCulture);
		}
		finally
		{
			CultureInfo.CurrentCulture = culture;
			CultureInfo.CurrentUICulture = uiCulture;
			CultureInfo.DefaultThreadCurrentCulture = threadCulture;
			CultureInfo.DefaultThreadCurrentUICulture = threadUICulture;
		}
	}
#endif
}
