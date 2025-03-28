// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RatingControlTests.h, commit e7e0823

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MUXControlsTestApp.Utilities;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Common;

#if USING_TAEF
using WEX.TestExecution;
using WEX.TestExecution.Markup;
using WEX.Logging.Interop;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
#endif

using RatingControl = Microsoft/* UWP don't rename */.UI.Xaml.Controls.RatingControl;
using RatingItemFontInfo = Microsoft/* UWP don't rename */.UI.Xaml.Controls.RatingItemFontInfo;
using RatingItemImageInfo = Microsoft/* UWP don't rename */.UI.Xaml.Controls.RatingItemImageInfo;
using Private.Infrastructure;

namespace Windows.UI.Xaml.Tests.MUXControls.ApiTests
{
	[TestClass]
	public class RatingControlTests : MUXApiTestBase
	{
		[TestMethod]
		public async Task VerifyDefaultsAndBasicSetting()
		{
			RatingControl ratingControl = null;
			RunOnUIThread.Execute(() =>
			{
				ratingControl = new RatingControl();
				Verify.IsNotNull(ratingControl);

				Verify.AreEqual(ratingControl.Caption, "");
				Verify.AreEqual(ratingControl.InitialSetValue, 1);
				Verify.AreEqual(ratingControl.IsClearEnabled, true);
				Verify.AreEqual(ratingControl.IsReadOnly, false);
				Verify.AreEqual(ratingControl.MaxRating, 5);
				Verify.AreEqual(ratingControl.PlaceholderValue, -1);
				Verify.AreEqual(ratingControl.Value, -1);

				// Disabled due to 12359255 reliability issue
				// Verify.IsTrue(ratingControl.ItemInfo is RatingItemFontInfo, "Verify default type of ItemInfo");
				// Verify.IsTrue((ratingControl.ItemInfo as RatingItemFontInfo).Glyph.Equals("\uE735"));

				// Now verify basic setting the value, and reaccessing it:

				ratingControl.Caption = "Rating API Test Caption";
				ratingControl.InitialSetValue = 2;
				ratingControl.IsClearEnabled = false;
				ratingControl.IsReadOnly = true;
				ratingControl.MaxRating = 10;
				ratingControl.PlaceholderValue = 3.0;
				ratingControl.Value = 2.0;

				var imageInfo = new RatingItemImageInfo();
				imageInfo.Image = new BitmapImage(new Uri("ms-appx:/Assets/rating_set.png"));
				ratingControl.ItemInfo = imageInfo;
			});


			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Verify.AreEqual(ratingControl.Caption, "Rating API Test Caption");
				Verify.AreEqual(ratingControl.InitialSetValue, 2);
				Verify.AreEqual(ratingControl.IsClearEnabled, false);
				Verify.AreEqual(ratingControl.IsReadOnly, true);
				Verify.AreEqual(ratingControl.MaxRating, 10);
				Verify.AreEqual(ratingControl.PlaceholderValue, 3.0);
				Verify.AreEqual(ratingControl.Value, 2.0);
				Verify.IsTrue(ratingControl.ItemInfo is RatingItemImageInfo, "Verify default type of ItemInfo [2]");
				Verify.IsTrue(((ratingControl.ItemInfo as RatingItemImageInfo).Image as BitmapImage).UriSource.Equals("ms-appx:/Assets/rating_set.png"));
			});
		}

		// Setting the value on a collapsed control can cause it to try and
		// interact with a non-existent AutomationPeer, causing a crash.
		[TestMethod]
		public void VerifyDontCrashWhenCollapsedAndValueSet()
		{
			RunOnUIThread.Execute(() =>
			{
				RatingControl ratingControl = new RatingControl();
				ratingControl.Visibility = Visibility.Collapsed;
				ratingControl.Value = 3.3;
			});
		}

		// Test just verifies the API contraaaact
		[TestMethod]
		public void VerifyValuesCoercion()
		{
			RunOnUIThread.Execute(() =>
			{
				RatingControl ratingControl = new RatingControl();
				Verify.IsNotNull(ratingControl);
				Verify.AreEqual(ratingControl.PlaceholderValue, -1);
				Verify.AreEqual(ratingControl.Value, -1);

				ratingControl.PlaceholderValue = 0.1;
				ratingControl.Value = 0.1;
				Verify.AreEqual(ratingControl.PlaceholderValue, 1.0, "Should coerce small PlaceholderValue values to 1.0");
				Verify.AreEqual(ratingControl.Value, 1.0, "Should coerce small Value values to 1.0");

				ratingControl.PlaceholderValue = 6.0;
				ratingControl.Value = 6.0;
				Verify.AreEqual(ratingControl.PlaceholderValue, 5.0, "Should coerce PlaceholderValue above MaxRating back to MaxRating");
				Verify.AreEqual(ratingControl.Value, 5.0, "Should coerce Value above MaxRating back to MaxRating");

				ratingControl.MaxRating = -2;
				Verify.AreEqual(ratingControl.MaxRating, 1, "Should coerce MaxRating below 1 back up to 1.");

				Verify.AreEqual(ratingControl.PlaceholderValue, 1.0, "Should auto-coerce now outdated PlaceholderValue above MaxRating back to MaxRating [2]");
				Verify.AreEqual(ratingControl.Value, 1.0, "Should auto-coerce now outdated Value above MaxRating back to MaxRating [2]");

				ratingControl.PlaceholderValue = 6.0;
				ratingControl.Value = 6.0;
				Verify.AreEqual(ratingControl.PlaceholderValue, 1.0, "Should coerce set PlaceholderValue above MaxRating back to MaxRating");
				Verify.AreEqual(ratingControl.Value, 1.0, "Should coerce set Value above MaxRating back to MaxRating");
			});
		}
	}
}
