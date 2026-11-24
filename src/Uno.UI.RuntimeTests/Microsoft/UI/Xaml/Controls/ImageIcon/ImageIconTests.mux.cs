// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ImageIconTests.cs

using System;
using System.Threading.Tasks;
using Common;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;

#if USING_TAEF
using WEX.TestExecution;
using WEX.TestExecution.Markup;
using WEX.Logging.Interop;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
#endif

namespace Microsoft.UI.Xaml.Tests.MUXControls.ApiTests
{
	[TestClass]
	public class ImageIconTests : MUXApiTestBase
	{
		[TestMethod]
		public async Task ImageIconTest()
		{
			ImageIcon imageIcon = null;
			var uri = new Uri("ms-appx:///Assets/Nuclear_symbol.svg");

			RunOnUIThread.Execute(() =>
			{
				imageIcon = new ImageIcon();

				/*
				#3949 is created to re-enable this part
				This is an unparented ImageIcon, so looking up the default foreground and verifying
				is a bit wierd. The colors are also chaning, so this is going to fail with those changes
				So commenting this check out for now to make the test more resilient.

				var theme = Application.Current.RequestedTheme;
				if (theme == ApplicationTheme.Dark)
				{
					Verify.AreEqual(((SolidColorBrush)imageIcon.Foreground).Color, Colors.White);
				}
				else
				{
					Verify.AreEqual(((SolidColorBrush)imageIcon.Foreground).Color, Colors.Black);
				}
				*/

				Log.Comment("Validate that you can change the properties.");

				imageIcon.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
				imageIcon.Source = new SvgImageSource(uri);
				Content = imageIcon;
				Content.UpdateLayout();
			});
			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Verify.IsTrue(imageIcon.Foreground is SolidColorBrush);
				Verify.AreEqual(Microsoft.UI.Colors.Red, (imageIcon.Foreground as SolidColorBrush).Color);
				Verify.AreEqual(uri, ((SvgImageSource)imageIcon.Source).UriSource);

#if HAS_UNO
				// TODO Uno specific: the MUX test app enables XamlChangeId.IconNoGridOptimization, which Uno doesn't
				// implement, so the Image is still wrapped in the Grid.
				var image = (Image)VisualTreeHelper.GetChild(VisualTreeHelper.GetChild(imageIcon, 0), 0);
#else
				var image = (Image)VisualTreeHelper.GetChild(imageIcon, 0);
#endif
				Verify.IsTrue(image.IsLoaded);
			});
		}
	}
}
