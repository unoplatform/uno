#if __WASM__
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_FontFamilyLoader
	{
		[TestMethod]
		[DataRow("ms-appx:///Assets/Fonts/Even Badder Mofo.ttf#EvenBadderMofo")]
		[DataRow("ms-appx:///Assets/Fonts/FamilyGuy-4grW.ttf")]
		[DataRow("ms-appx:///Assets/Fonts/Nillambari-K7y1W.ttf")]
		[DataRow("Assets/Fonts/Nillambari-K7y1W.ttf")]
		[DataRow("/Assets/Fonts/Nillambari-K7y1W.ttf")]
		[DataRow("Assets/Fonts/Even Badder Mofo.ttf")]
		[DataRow("Assets/Fonts/Even Badder Mofo.ttf#EvenBadderMofo")]
		[DataRow("/Assets/Fonts/Even Badder Mofo.ttf#EvenBadderMofo")]
		[DataRow("https://raw.githubusercontent.com/unoplatform/uno/8751b7af65f5426d3a1f91274b8663465452411c/src/SamplesApp/UITests.Shared/Assets/RemoteFonts/antikythera.woff")]
		[DataRow("https://raw.githubusercontent.com/unoplatform/uno/8751b7af65f5426d3a1f91274b8663465452411c/src/SamplesApp/UITests.Shared/Assets/RemoteFonts/antikytheraoutlineital.woff")]
		[DataRow("https://raw.githubusercontent.com/unoplatform/uno/8751b7af65f5426d3a1f91274b8663465452411c/src/SamplesApp/UITests.Shared/Assets/RemoteFonts/antikytheraoutline.woff")]
		[DataRow("https://raw.githubusercontent.com/unoplatform/uno/8751b7af65f5426d3a1f91274b8663465452411c/src/SamplesApp/UITests.Shared/Assets/RemoteFonts/GALACTIC%20VANGUARDIAN%20NCV.woff")]
		public async Task With_FontPath(string fontPath)
		{
			var SUT = new TextBlock()
			{
				Text = "Hellow Uno!",
				FontFamily = new(fontPath)
			};

			var loader = FontFamilyLoader.GetLoaderForFontFamily(SUT.FontFamily);
			Assert.IsNotNull(loader, "loader");
			Assert.IsFalse(loader.IsLoading, "IsLoading");

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			var sw = Stopwatch.StartNew();
			while ((!loader.IsLoaded || loader.IsLoading) && sw.Elapsed < TimeSpan.FromSeconds(10))
			{
				await Task.Delay(100);
				await WindowHelper.WaitForIdle();
			}

			Assert.IsFalse(loader.IsLoading, "IsLoading");
			Assert.IsTrue(loader.IsLoaded, "IsLoaded");
			Assert.IsTrue(await loader.LoadFontAsync());
		}

		[TestMethod]
		public async Task When_FailedLoading()
		{
			var family = new FontFamily("https://raw.githubusercontent.com/unoplatform/uno/8751b7af65f5426d3a1f91274b8663465452411c/src/SamplesApp/UITests.Shared/Assets/RemoteFonts/INVALIDFONT.woff");
			var loader = FontFamilyLoader.GetLoaderForFontFamily(family);
			Assert.IsFalse(await loader.LoadFontAsync());
		}
	}
}
#endif
