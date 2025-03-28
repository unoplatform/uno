// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Common;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace Windows.UI.Xaml.Tests.MUXControls.InteractionTests
{
	[Ignore("PipsPager tests are failing due to UI test differences")]
	public partial class PipsPagerTests : PipsPagerTestBase
	{
		// TODO Uno: The following tests are not ported yet due to missing test framework features:
		// - KeyboardPageSelectTest
		[SetUp]
		public void TestSetup()
		{
			Run("MUXControlsTestApp.PipsPagerPage");
		}

		[Test]
		[AutoRetry]
		public void PipsPagerChangingPageTest()
		{
			//using (var setup = new TestSetupHelper("PipsPager Tests"))
			{
				elements = new PipsPagerElements(this._app);
				SetPreviousPageButtonVisibilityMode(ButtonVisibilityMode.Visible);
				SetNextPageButtonVisibilityMode(ButtonVisibilityMode.Visible);

				VerifySelectedPageIndex(0);

				InputHelperLeftClick(elements.GetNextPageButton());
				VerifySelectedPageIndex(1);

				InputHelperLeftClick(elements.GetNextPageButton());
				VerifySelectedPageIndex(2);

				InputHelperLeftClick(elements.GetPreviousPageButton());
				VerifySelectedPageIndex(1);

				InputHelperLeftClick(elements.GetPreviousPageButton());
				VerifySelectedPageIndex(0);

				ChangeNumberOfPages(NumberOfPagesOptions.Five);
				VerifyNumberOfPages("5");

				InputHelperLeftClick(elements.GetNextPageButton());
				VerifySelectedPageIndex(1);

				InputHelperLeftClick(elements.GetNextPageButton());
				VerifySelectedPageIndex(2);

				InputHelperLeftClick(elements.GetNextPageButton());
				VerifySelectedPageIndex(3);

				InputHelperLeftClick(elements.GetPreviousPageButton());
				VerifySelectedPageIndex(2);
			}
		}

		[Test]
		[AutoRetry]
		public void PreviousPageButtonChangingPageTest()
		{
			{
				elements = new PipsPagerElements(this._app);
				SetPreviousPageButtonVisibilityMode(ButtonVisibilityMode.Visible);
				SetNextPageButtonVisibilityMode(ButtonVisibilityMode.Visible);

				VerifySelectedPageIndex(0);
				InputHelperLeftClick(elements.GetNextPageButton());

				InputHelperLeftClick(elements.GetPreviousPageButton());
				VerifySelectedPageIndex(0);

				InputHelperLeftClick(elements.GetNextPageButton());
				InputHelperLeftClick(elements.GetNextPageButton());

				InputHelperLeftClick(elements.GetPreviousPageButton());
				VerifySelectedPageIndex(1);
			}
		}

		[Test]
		[AutoRetry]
		public void NextPageButtonChangingPageTest()
		{
			{
				elements = new PipsPagerElements(this._app);
				SetPreviousPageButtonVisibilityMode(ButtonVisibilityMode.Visible);
				SetNextPageButtonVisibilityMode(ButtonVisibilityMode.Visible);

				VerifySelectedPageIndex(0);
				InputHelperLeftClick(elements.GetNextPageButton());
				VerifySelectedPageIndex(1);

				InputHelperLeftClick(elements.GetNextPageButton());
				VerifySelectedPageIndex(2);

				InputHelperLeftClick(elements.GetNextPageButton());
				VerifySelectedPageIndex(3);

				InputHelperLeftClick(elements.GetNextPageButton());
				VerifySelectedPageIndex(4);
			}
		}

		[Test]
		[AutoRetry]
		public void PipsPagerInfinitePagesTest()
		{
			{
				elements = new PipsPagerElements(this._app);
				SetPreviousPageButtonVisibilityMode(ButtonVisibilityMode.Visible);
				SetNextPageButtonVisibilityMode(ButtonVisibilityMode.Visible);

				VerifySelectedPageIndex(0);

				ChangeNumberOfPages(NumberOfPagesOptions.Infinite);
				VerifyNumberOfPages("Infinite");

				InputHelperLeftClick(elements.GetNextPageButton());
				VerifySelectedPageIndex(1);

				InputHelperLeftClick(elements.GetNextPageButton());
				VerifySelectedPageIndex(2);

				InputHelperLeftClick(elements.GetNextPageButton());
				VerifySelectedPageIndex(3);

				InputHelperLeftClick(elements.GetNextPageButton());
				VerifySelectedPageIndex(4);

				InputHelperLeftClick(elements.GetNextPageButton());
				VerifySelectedPageIndex(5);

				InputHelperLeftClick(elements.GetNextPageButton());
				VerifySelectedPageIndex(6);

				InputHelperLeftClick(elements.GetNextPageButton());
				VerifySelectedPageIndex(7);
			}
		}

		[Test]
		[AutoRetry]
		public void PreviousPageButtonVisibilityOptionsTest()
		{
			{
				elements = new PipsPagerElements(this._app);
				SetNextPageButtonVisibilityMode(ButtonVisibilityMode.Visible);

				/* Test for Collapsed */
				SetPreviousPageButtonVisibilityMode(ButtonVisibilityMode.Collapsed);
				VerifyPageButtonWithVisibilityModeSet(ButtonType.Previous, ButtonVisibilityMode.Collapsed);
				InputHelperLeftClick(elements.GetNextPageButton());
				VerifyPageButtonWithVisibilityModeSet(ButtonType.Previous, ButtonVisibilityMode.Collapsed);

				/* Test for Visible */
				SetPreviousPageButtonVisibilityMode(ButtonVisibilityMode.Visible);
				VerifyPageButtonWithVisibilityModeSet(ButtonType.Previous, ButtonVisibilityMode.Visible);
				InputHelperLeftClick(elements.GetPreviousPageButton());
				VerifyPageButtonWithVisibilityModeSet(ButtonType.Previous, ButtonVisibilityMode.Collapsed);

				/* Test for VisibleOnPointerOver */
				SetPreviousPageButtonVisibilityMode(ButtonVisibilityMode.VisibleOnPointerOver);
				VerifyPageButtonWithVisibilityModeSet(ButtonType.Previous, ButtonVisibilityMode.Collapsed);

				InputHelperLeftClick(elements.GetNextPageButton());
				VerifyPageButtonWithVisibilityModeSet(ButtonType.Previous, ButtonVisibilityMode.VisibleOnPointerOver, true);

				InputHelperLeftClick(elements.GetCurrentNumberOfPagesTextBlock());
				VerifyPageButtonWithVisibilityModeSet(ButtonType.Previous, ButtonVisibilityMode.VisibleOnPointerOver);
			}
		}

		[Test]
		[AutoRetry]
		public void NextPageButtonVisibilityOptionsTest()
		{
			{
				elements = new PipsPagerElements(this._app);
				SetPreviousPageButtonVisibilityMode(ButtonVisibilityMode.VisibleOnPointerOver);

				ChangeNumberOfPages(NumberOfPagesOptions.Five);
				VerifyNumberOfPages("5");

				/* Test for Collapsed */
				SetNextPageButtonVisibilityMode(ButtonVisibilityMode.Collapsed);
				VerifyPageButtonWithVisibilityModeSet(ButtonType.Next, ButtonVisibilityMode.Collapsed);

				/* Test for Visible */
				SetNextPageButtonVisibilityMode(ButtonVisibilityMode.Visible);
				VerifyPageButtonWithVisibilityModeSet(ButtonType.Next, ButtonVisibilityMode.Visible);
				/* We step until the end of the list (4 times, since we have 5 pages) */
				InputHelperLeftClick(elements.GetNextPageButton());
				InputHelperLeftClick(elements.GetNextPageButton());
				InputHelperLeftClick(elements.GetNextPageButton());
				InputHelperLeftClick(elements.GetNextPageButton());
				VerifyPageButtonWithVisibilityModeSet(ButtonType.Next, ButtonVisibilityMode.Collapsed);

				/* Test for VisibleOnPointerOver */
				SetNextPageButtonVisibilityMode(ButtonVisibilityMode.VisibleOnPointerOver);
				VerifyPageButtonWithVisibilityModeSet(ButtonType.Next, ButtonVisibilityMode.Collapsed);

				InputHelperLeftClick(elements.GetPreviousPageButton());
				VerifyPageButtonWithVisibilityModeSet(ButtonType.Next, ButtonVisibilityMode.VisibleOnPointerOver, true);

				InputHelperLeftClick(elements.GetCurrentNumberOfPagesTextBlock());
				VerifyPageButtonWithVisibilityModeSet(ButtonType.Next, ButtonVisibilityMode.VisibleOnPointerOver);
			}
		}

		[Test]
		[AutoRetry]
		public void OrientationChangeTest()
		{
			{
				elements = new PipsPagerElements(this._app);
				SetOrientation(Orientation.Horizontal);
				VerifyOrientationChanged("Horizontal");

				SetOrientation(Orientation.Vertical);
				VerifyOrientationChanged("Vertical");

			}
		}

		[Test]
		[AutoRetry]
		public void PipSizeWithDifferentOrientationsTest()
		{
			{
				elements = new PipsPagerElements(this._app);
				var getButtonSizesButton = elements.GetPipsPagerButtonSizesButton();
				InputHelperLeftClick(getButtonSizesButton);

				var horizontalOrientationPipsPagerButtonWidth = elements.GetHorizontalOrientationPipsPagerButtonWidthTextBlock();
				var horizontalOrientationPipsPagerButtonHeight = elements.GetHorizontalOrientationPipsPagerButtonHeightTextBlock();

				var verticalOrientationPipsPagerButtonWidth = elements.GetVerticalOrientationPipsPagerButtonWidthTextBlock();
				var verticalOrientationPipsPagerButtonHeight = elements.GetVerticalOrientationPipsPagerButtonHeightTextBlock();

				Verify.AreEqual("12", horizontalOrientationPipsPagerButtonWidth.FirstResult().Text);
				Verify.AreEqual("20", horizontalOrientationPipsPagerButtonHeight.FirstResult().Text);
				//TODO Uno: Removed vertical pips pager to be able to properly query elements (was not possible due to Uno.UITest limitations
				//Verify.AreEqual("20", verticalOrientationPipsPagerButtonWidth.FirstResult().Text);
				//Verify.AreEqual("12", verticalOrientationPipsPagerButtonHeight.FirstResult().Text);
			}
		}

		[Test]
		[AutoRetry]
		public void PipSizeAfterOrientationChangeTest()
		{
			{
				elements = new PipsPagerElements(this._app);
				var getButtonSizesButton = elements.GetPipsPagerButtonSizesButton();
				InputHelperLeftClick(getButtonSizesButton);

				var horizontalOrientationPipsPagerButtonWidth = elements.GetHorizontalOrientationPipsPagerButtonWidthTextBlock();
				var horizontalOrientationPipsPagerButtonHeight = elements.GetHorizontalOrientationPipsPagerButtonHeightTextBlock();
				Verify.AreEqual("12", horizontalOrientationPipsPagerButtonWidth.FirstResult().Text);
				Verify.AreEqual("20", horizontalOrientationPipsPagerButtonHeight.FirstResult().Text);

				SetOrientation(Orientation.Vertical);
				VerifyOrientationChanged("Vertical");

				InputHelperLeftClick(getButtonSizesButton);

				Verify.AreEqual("20", horizontalOrientationPipsPagerButtonWidth.FirstResult().Text);
				Verify.AreEqual("12", horizontalOrientationPipsPagerButtonHeight.FirstResult().Text);
			}
		}

		private void InputHelperLeftClick(QueryEx result)
		{
			_app.Tap(result);
		}
	}
}
