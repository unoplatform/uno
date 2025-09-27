using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
using SamplesApp.UITests;
using Uno.UI.RuntimeTests.Helpers;
using PersonPicture = Microsoft.UI.Xaml.Controls.PersonPicture;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public partial class Given_PersonPicture
{
	private partial class MyPersonPicture : PersonPicture
	{
		public TextBlock InitialsTextBlock { get; private set; }

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			InitialsTextBlock = (TextBlock)GetTemplateChild("InitialsTextBlock");
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/16006")]
	public async Task TestInitialsTextBlockFontFamily()
	{
		var personPicture = new MyPersonPicture();
		await UITestHelper.Load(personPicture);

#if WINAPPSDK
		string symbolsFontName = "Segoe MDL2 Assets";
#else
		string symbolsFontName = "ms-appx:///Uno.Fonts.Fluent/Fonts/uno-fluentui-assets.ttf";
#endif

		var fontFamilyLight = personPicture.InitialsTextBlock.FontFamily.Source;
		Assert.AreEqual(symbolsFontName, fontFamilyLight);

		using (ThemeHelper.UseDarkTheme())
		{
			Assert.AreEqual(fontFamilyLight, personPicture.InitialsTextBlock.FontFamily.Source);
			await TestServices.WindowHelper.WaitForIdle();
			Assert.AreEqual(fontFamilyLight, personPicture.InitialsTextBlock.FontFamily.Source);
		}

		Assert.AreEqual(fontFamilyLight, personPicture.InitialsTextBlock.FontFamily.Source);
	}
}
