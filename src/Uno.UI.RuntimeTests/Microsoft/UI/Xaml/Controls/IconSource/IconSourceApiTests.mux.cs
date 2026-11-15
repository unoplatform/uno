// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference IconSourceApiTests.cs

using System;

using MUXControlsTestApp.Utilities;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI.Text;
using Windows.Foundation.Metadata;
using Common;

#if USING_TAEF
using WEX.TestExecution;
using WEX.TestExecution.Markup;
using WEX.Logging.Interop;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
#endif

using SymbolIconSource = Microsoft.UI.Xaml.Controls.SymbolIconSource;
using FontIconSource = Microsoft.UI.Xaml.Controls.FontIconSource;
using BitmapIconSource = Microsoft.UI.Xaml.Controls.BitmapIconSource;
using ImageIconSource = Microsoft.UI.Xaml.Controls.ImageIconSource;
using PathIconSource = Microsoft.UI.Xaml.Controls.PathIconSource;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Controls.AnimatedVisuals;
using Private.Infrastructure;
using System.Threading.Tasks;

#if !HAS_UNO_WINUI
using Microsoft.UI.Xaml.Controls;
#endif

#if HAS_UNO
using Uno.UI;
#endif

namespace Microsoft.UI.Xaml.Tests.MUXControls.ApiTests
{
	[TestClass]
	public class IconSourceApiTests : MUXApiTestBase
	{
		[TestMethod]
		public async Task ImageIconSourceTest()
		{
			ImageIconSource iconSource = null;
			ImageIcon imageIcon = null;
			var uri = new Uri("ms-appx:///Assets/Nuclear_symbol.svg");

			RunOnUIThread.Execute(() =>
			{
				iconSource = new ImageIconSource();
				imageIcon = iconSource.CreateIconElement() as ImageIcon;

				// IconSource.Foreground should be null to allow foreground inheritance from
				// the parent to work.
				Verify.AreEqual(iconSource.Foreground, null);
				//Verify.AreEqual(imageIcon.Foreground, null);

				Log.Comment("Validate the defaults match BitmapIcon.");

				var icon = new ImageIcon();
				Verify.AreEqual(icon.Source, iconSource.ImageSource);
				Verify.AreEqual(imageIcon.Source, iconSource.ImageSource);

				Log.Comment("Validate that you can change the properties.");

				iconSource.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
				iconSource.ImageSource = new SvgImageSource(uri);
			});
			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Verify.IsTrue(iconSource.Foreground is SolidColorBrush);
				Verify.IsTrue(imageIcon.Foreground is SolidColorBrush);
				Verify.AreEqual(Microsoft.UI.Colors.Red, (iconSource.Foreground as SolidColorBrush).Color);
				Verify.AreEqual(Microsoft.UI.Colors.Red, (imageIcon.Foreground as SolidColorBrush).Color);
				Verify.AreEqual(uri, ((SvgImageSource)iconSource.ImageSource).UriSource);
				Verify.AreEqual(uri, ((SvgImageSource)imageIcon.Source).UriSource);
			});
		}

		[TestMethod]
		public async Task AnimatedIconSourceTest()
		{
			AnimatedIconSource iconSource = null;
			IAnimatedVisualSource2 source = null;
			AnimatedIcon animatedIcon = null;

			RunOnUIThread.Execute(() =>
			{
				iconSource = new AnimatedIconSource();
				source = new AnimatedChevronDownSmallVisualSource();
				animatedIcon = iconSource.CreateIconElement() as AnimatedIcon;

				// IconSource.Foreground should be null to allow foreground inheritance from
				// the parent to work.
				Verify.AreEqual(iconSource.Foreground, null);
				//Verify.AreEqual(animatedIcon.Foreground, null);
				Verify.AreEqual(iconSource.MirroredWhenRightToLeft, false);
				Verify.AreEqual(animatedIcon.MirroredWhenRightToLeft, false);

				Log.Comment("Validate the defaults match BitmapIcon.");

				var icon = new AnimatedIcon();
				Verify.AreEqual(icon.Source, iconSource.Source);
				Verify.AreEqual(animatedIcon.Source, iconSource.Source);
				Verify.AreEqual(icon.MirroredWhenRightToLeft, iconSource.MirroredWhenRightToLeft);

				Log.Comment("Validate that you can change the properties.");

				iconSource.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
				iconSource.Source = source;
				iconSource.MirroredWhenRightToLeft = true;
			});
			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Verify.IsTrue(iconSource.Foreground is SolidColorBrush);
				Verify.IsTrue(animatedIcon.Foreground is SolidColorBrush);
				Verify.AreEqual(Microsoft.UI.Colors.Red, (iconSource.Foreground as SolidColorBrush).Color);
				Verify.AreEqual(Microsoft.UI.Colors.Red, (animatedIcon.Foreground as SolidColorBrush).Color);
				Verify.AreEqual(source, iconSource.Source);
				Verify.AreEqual(source, animatedIcon.Source);
				Verify.IsTrue(iconSource.MirroredWhenRightToLeft);
				Verify.IsTrue(animatedIcon.MirroredWhenRightToLeft);
			});
		}

		[TestMethod]
		public void CreateIconElementReturnsCorrectTypeTest()
		{
			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Verify BitmapIconSource creates BitmapIcon");
				var bitmapIconSource = new BitmapIconSource();
				var bitmapIcon = bitmapIconSource.CreateIconElement();
				Verify.IsNotNull(bitmapIcon);
				Verify.IsTrue(bitmapIcon is BitmapIcon);

				Log.Comment("Verify FontIconSource creates FontIcon");
				var fontIconSource = new FontIconSource();
				var fontIcon = fontIconSource.CreateIconElement();
				Verify.IsNotNull(fontIcon);
				Verify.IsTrue(fontIcon is FontIcon);

				Log.Comment("Verify SymbolIconSource creates SymbolIcon");
				var symbolIconSource = new SymbolIconSource();
				var symbolIcon = symbolIconSource.CreateIconElement();
				Verify.IsNotNull(symbolIcon);
				Verify.IsTrue(symbolIcon is SymbolIcon);

				Log.Comment("Verify PathIconSource creates PathIcon");
				var pathIconSource = new PathIconSource();
				var pathIcon = pathIconSource.CreateIconElement();
				Verify.IsNotNull(pathIcon);
				Verify.IsTrue(pathIcon is PathIcon);
			});
		}

		[TestMethod]
		public async Task CreateIconElementForegroundTest()
		{
			FontIconSource iconSource1 = null;
			FontIconSource iconSource2 = null;
			FontIcon icon1 = null;
			FontIcon icon2 = null;

			RunOnUIThread.Execute(() =>
			{
				iconSource1 = new FontIconSource()
				{
					Foreground = new SolidColorBrush(Microsoft.UI.Colors.Blue)
				};

				iconSource2 = new FontIconSource();

				Log.Comment("Create first icon element with foreground already set");
				icon1 = iconSource1.CreateIconElement() as FontIcon;
				Verify.IsNotNull(icon1);

				Log.Comment("Create second icon element with foreground not set");
				icon2 = iconSource2.CreateIconElement() as FontIcon;
				Verify.IsNotNull(icon2);
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Verify foreground is applied to both icon elements");
				Verify.IsTrue(icon1.Foreground is SolidColorBrush);
				Verify.IsTrue(icon2.Foreground is SolidColorBrush);
				Verify.AreEqual(Microsoft.UI.Colors.Blue, (icon1.Foreground as SolidColorBrush).Color);
			});
		}

		[TestMethod]
		public async Task PropertyChangePropagationToCreatedElements()
		{
			FontIconSource iconSource = null;
			FontIcon icon = null;

			RunOnUIThread.Execute(() =>
			{
				iconSource = new FontIconSource();

				Log.Comment("Create icon element before setting properties");
				icon = iconSource.CreateIconElement() as FontIcon;

				Verify.IsNotNull(icon);
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Verify foreground is not null before setting");
				Verify.IsNotNull(icon.Foreground);

				Log.Comment("Change foreground on IconSource");
				iconSource.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Verify foreground propagates to created element");
				Verify.IsTrue(icon.Foreground is SolidColorBrush);
				Verify.AreEqual(Microsoft.UI.Colors.Red, (icon.Foreground as SolidColorBrush).Color);
			});
		}

		[TestMethod]
		public async Task CreateIconElementPreservesIconSourceProperties()
		{
			FontIconSource fontIconSource = null;
			FontIcon fontIcon = null;

			RunOnUIThread.Execute(() =>
			{
				fontIconSource = new FontIconSource();
				fontIconSource.Glyph = "";
				fontIconSource.FontSize = 24;
				fontIconSource.FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe UI Symbol");
				fontIconSource.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Purple);

				Log.Comment("Create icon element from configured source");
				fontIcon = fontIconSource.CreateIconElement() as FontIcon;
				Verify.IsNotNull(fontIcon);
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Verify all properties are transferred to the icon element");
				Verify.AreEqual("", fontIcon.Glyph);
				Verify.AreEqual(24.0, fontIcon.FontSize);
#if !HAS_UNO
				Verify.AreEqual("Segoe UI Symbol", fontIcon.FontFamily.Source);
#else
				// In case of Uno the Source is equal to FeatureConfiguration.Font.SymbolsFont
				Verify.AreEqual(FeatureConfiguration.Font.SymbolsFont, fontIcon.FontFamily.Source);
#endif
				Verify.AreEqual(Microsoft.UI.Colors.Purple, (fontIcon.Foreground as SolidColorBrush).Color);
			});
		}

		// Uno Doc: tests from here onwards are only present in the WinUI2 source
		[TestMethod]
		public async Task SymbolIconSourceTest()
		{
			SymbolIconSource iconSource = null;
			SymbolIcon symbolIcon = null;

			RunOnUIThread.Execute(() =>
			{
				iconSource = new SymbolIconSource();
				symbolIcon = iconSource.CreateIconElement() as SymbolIcon;

				// IconSource.Foreground should be null to allow foreground inheritance from
				// the parent to work.
				Verify.AreEqual(iconSource.Foreground, null);
				//Verify.AreEqual(symbolIcon.Foreground, null);

				Log.Comment("Validate the defaults match SymbolIcon.");

				var icon = new SymbolIcon();
				Verify.AreEqual(icon.Symbol, iconSource.Symbol);
				Verify.AreEqual(symbolIcon.Symbol, iconSource.Symbol);

				Log.Comment("Validate that you can change the properties.");

				iconSource.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
				iconSource.Symbol = Symbol.HangUp;
			});
			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Verify.IsTrue(iconSource.Foreground is SolidColorBrush);
				Verify.AreEqual(Microsoft.UI.Colors.Red, (iconSource.Foreground as SolidColorBrush).Color);
				Verify.AreEqual(Microsoft.UI.Colors.Red, (symbolIcon.Foreground as SolidColorBrush).Color);
				Verify.AreEqual(Symbol.HangUp, iconSource.Symbol);
				Verify.AreEqual(Symbol.HangUp, symbolIcon.Symbol);
			});
		}

		[TestMethod]
		public async Task FontIconSourceTest()
		{
			FontIconSource iconSource = null;
			FontIcon fontIcon = null;

			RunOnUIThread.Execute(() =>
			{
				iconSource = new FontIconSource();
				fontIcon = iconSource.CreateIconElement() as FontIcon;

				// IconSource.Foreground should be null to allow foreground inheritance from
				// the parent to work.
				Verify.AreEqual(iconSource.Foreground, null);
				//Verify.AreEqual(fontIcon.Foreground, null);

				Log.Comment("Validate the defaults match FontIcon.");

				var icon = new FontIcon();
				Verify.AreEqual(icon.Glyph, iconSource.Glyph);
				Verify.AreEqual(fontIcon.Glyph, iconSource.Glyph);
				Verify.AreEqual(icon.FontSize, iconSource.FontSize);
				Verify.AreEqual(fontIcon.FontSize, iconSource.FontSize);
				Verify.AreEqual(icon.FontStyle, iconSource.FontStyle);
				Verify.AreEqual(fontIcon.FontStyle, iconSource.FontStyle);
				Verify.AreEqual(icon.FontWeight.Weight, iconSource.FontWeight.Weight);
				Verify.AreEqual(fontIcon.FontWeight.Weight, iconSource.FontWeight.Weight);
				Verify.AreEqual(icon.FontFamily.Source, iconSource.FontFamily.Source);
				Verify.AreEqual(fontIcon.FontFamily.Source, iconSource.FontFamily.Source);
				Verify.AreEqual(icon.IsTextScaleFactorEnabled, iconSource.IsTextScaleFactorEnabled);
				Verify.AreEqual(fontIcon.IsTextScaleFactorEnabled, iconSource.IsTextScaleFactorEnabled);
				Verify.AreEqual(icon.MirroredWhenRightToLeft, iconSource.MirroredWhenRightToLeft);
				Verify.AreEqual(fontIcon.MirroredWhenRightToLeft, iconSource.MirroredWhenRightToLeft);

				Log.Comment("Validate that you can change the properties.");

				iconSource.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
				iconSource.Glyph = "&#xE114;";
				iconSource.FontSize = 25;
				iconSource.FontStyle = FontStyle.Oblique;
				iconSource.FontWeight = new FontWeight() { Weight = 250 };
				iconSource.FontFamily = new FontFamily("Segoe UI Symbol");
				iconSource.IsTextScaleFactorEnabled = true;
				iconSource.MirroredWhenRightToLeft = true;
			});
			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Verify.IsTrue(iconSource.Foreground is SolidColorBrush);
				Verify.IsTrue(fontIcon.Foreground is SolidColorBrush);
				Verify.AreEqual(Microsoft.UI.Colors.Red, (iconSource.Foreground as SolidColorBrush).Color);
				Verify.AreEqual(Microsoft.UI.Colors.Red, (fontIcon.Foreground as SolidColorBrush).Color);
				Verify.AreEqual("&#xE114;", iconSource.Glyph);
				Verify.AreEqual("&#xE114;", fontIcon.Glyph);
				Verify.AreEqual(25, iconSource.FontSize);
				Verify.AreEqual(25, fontIcon.FontSize);
				Verify.AreEqual(FontStyle.Oblique, iconSource.FontStyle);
				Verify.AreEqual(FontStyle.Oblique, fontIcon.FontStyle);
				Verify.AreEqual(250, iconSource.FontWeight.Weight);
				Verify.AreEqual(250, fontIcon.FontWeight.Weight);
#if !HAS_UNO
				Verify.AreEqual("Segoe UI Symbol", iconSource.FontFamily.Source);
				Verify.AreEqual("Segoe UI Symbol", fontIcon.FontFamily.Source);
#else
				// In case of Uno the Source is equal to FeatureConfiguration.Font.SymbolsFont
				Verify.AreEqual(FeatureConfiguration.Font.SymbolsFont, iconSource.FontFamily.Source);
				Verify.AreEqual(FeatureConfiguration.Font.SymbolsFont, fontIcon.FontFamily.Source);
#endif
				Verify.AreEqual(true, iconSource.IsTextScaleFactorEnabled);
				Verify.AreEqual(true, fontIcon.IsTextScaleFactorEnabled);
				Verify.AreEqual(true, iconSource.MirroredWhenRightToLeft);
				Verify.AreEqual(true, fontIcon.MirroredWhenRightToLeft);
			});
		}

		[TestMethod]
		public async Task BitmapIconSourceTest()
		{
			BitmapIconSource iconSource = null;
			BitmapIcon bitmapIcon = null;
			var uri = new Uri("ms-appx:///Assets/ingredient1.png");

			RunOnUIThread.Execute(() =>
			{
				iconSource = new BitmapIconSource();
				bitmapIcon = iconSource.CreateIconElement() as BitmapIcon;

				// IconSource.Foreground should be null to allow foreground inheritance from
				// the parent to work.
				Verify.AreEqual(iconSource.Foreground, null);
				//Verify.AreEqual(bitmapIcon.Foreground, null);

				Log.Comment("Validate the defaults match BitmapIcon.");

				var icon = new BitmapIcon();
				Verify.AreEqual(icon.UriSource, iconSource.UriSource);
				Verify.AreEqual(bitmapIcon.UriSource, iconSource.UriSource);

				if (ApiInformation.IsPropertyPresent("Microsoft.UI.Xaml.Controls.BitmapIcon, Uno.UI", "ShowAsMonochrome"))
				{
					Verify.AreEqual(icon.ShowAsMonochrome, iconSource.ShowAsMonochrome);
					Verify.AreEqual(bitmapIcon.ShowAsMonochrome, iconSource.ShowAsMonochrome);
				}

				Log.Comment("Validate that you can change the properties.");

				iconSource.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
				iconSource.UriSource = uri;
				iconSource.ShowAsMonochrome = false;
			});
			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Verify.IsTrue(iconSource.Foreground is SolidColorBrush);
				Verify.IsTrue(bitmapIcon.Foreground is SolidColorBrush);
				Verify.AreEqual(Microsoft.UI.Colors.Red, (iconSource.Foreground as SolidColorBrush).Color);
				Verify.AreEqual(Microsoft.UI.Colors.Red, (bitmapIcon.Foreground as SolidColorBrush).Color);
				Verify.AreEqual(uri, iconSource.UriSource);
				Verify.AreEqual(uri, bitmapIcon.UriSource);
				Verify.AreEqual(false, iconSource.ShowAsMonochrome);
				Verify.AreEqual(false, bitmapIcon.ShowAsMonochrome);
			});
		}

		[TestMethod]
		public async Task PathIconSourceTest()
		{
			PathIconSource iconSource = null;
			RectangleGeometry rectGeometry = null;
			PathIcon pathIcon = null;

			RunOnUIThread.Execute(() =>
			{
				iconSource = new PathIconSource();
				pathIcon = iconSource.CreateIconElement() as PathIcon;

				// IconSource.Foreground should be null to allow foreground inheritance from
				// the parent to work.
				Verify.AreEqual(iconSource.Foreground, null);
				//Verify.AreEqual(pathIcon.Foreground, null);

				Log.Comment("Validate the defaults match PathIcon.");

				var icon = new PathIcon();
				Verify.AreEqual(icon.Data, iconSource.Data);
				Verify.AreEqual(pathIcon.Data, iconSource.Data);

				Log.Comment("Validate that you can change the properties.");

				iconSource.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
				iconSource.Data = rectGeometry = new RectangleGeometry();
			});
			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Verify.IsTrue(iconSource.Foreground is SolidColorBrush);
				Verify.IsTrue(pathIcon.Foreground is SolidColorBrush);
				Verify.AreEqual(Microsoft.UI.Colors.Red, (iconSource.Foreground as SolidColorBrush).Color);
				Verify.AreEqual(Microsoft.UI.Colors.Red, (pathIcon.Foreground as SolidColorBrush).Color);
				Verify.AreEqual(rectGeometry, iconSource.Data);
				Verify.AreEqual(rectGeometry, pathIcon.Data);
			});
		}

		// XamlControlsXamlMetaDataProvider does not exist in the OS repo,
		// so we can't execute this test as authored there.
		//[TestMethod]
		//public void VerifyFontWeightPropertyMetadata()
		//{
		//    RunOnUIThread.Execute(() =>
		//    {
		//        XamlControlsXamlMetaDataProvider provider = new XamlControlsXamlMetaDataProvider();
		//        var FontIconSourceType = provider.GetXamlType(typeof(FontIconSource).FullName);
		//        var fontWeightMember = FontIconSourceType.GetMember("FontWeight");
		//        Verify.IsNotNull(fontWeightMember);
		//        var memberType = fontWeightMember.Type;
		//        Verify.AreEqual(memberType.BaseType.FullName, "ValueType");
		//    });
		//}
	}
}
