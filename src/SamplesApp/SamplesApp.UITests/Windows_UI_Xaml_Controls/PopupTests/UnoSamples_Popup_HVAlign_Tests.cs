using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.PopupTests
{
	public partial class UnoSamples_Popup_HVAlign_Tests : PopupUITestBase
	{
		[Test]
		[AutoRetry]
		public void Popup_PlacementTest_1Default_HSVS() => Popup_PlacementTest("Default_HSVS", 0f, 0f);

		[Test]
		[AutoRetry]
		public void Popup_PlacementTest_2Default_HTVL() => Popup_PlacementTest("Default_HLVT", 0f, 0f);

		[Test]
		[AutoRetry]
		public void Popup_PlacementTest_3Default_HCVC() => Popup_PlacementTest("Default_HCVC", 0.5f, 0.5f);

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Browser)] // disabled android due to all popup from the 4th column could not be located in visual tree
		public void Popup_PlacementTest_4Default_HRVB() => Popup_PlacementTest("Default_HRVB", 1f, 1f);

		public void Popup_PlacementTest(string targetName, float xMul, float yMul)
		{
			Run("Uno.UI.Samples.Content.UITests.Popup.Popup_HVAlignments");

			// tap button open popup
			_app.Tap(targetName);

			// find the expected popup placement based on button rect
			var buttonRect = _app.Query(targetName).Single().Rect;
			var expectedX = buttonRect.X + (buttonRect.Width * xMul);
			var expectedY = buttonRect.Y + (buttonRect.Height * yMul);

			// compare against actual popup placement
			var popupRect = _app.Query("PopupContent").SingleOrDefault()?.Rect ?? throw new InvalidOperationException("Failed to find 'PopupContent'");
			if (!NearlyEqual(popupRect.X, expectedX) || !NearlyEqual(popupRect.Y, expectedY))
			{
				Assert.Fail(string.Join("\n",
					$"Popup for '{targetName}' is expected to open at ({expectedX},{expectedY}), but is actually opened at ({popupRect.X},{popupRect.Y})",
					"===== Context =====",
					$"buttonRect: [Rect {buttonRect.Width}x{buttonRect.Height}@{buttonRect.X}{buttonRect.Y}]",
					$"xMul: {xMul}, yMul: {yMul}"
				));
			}

			bool NearlyEqual(float a, float b) => a.Equals(b) || Math.Abs(a - b) < float.Epsilon;
		}
	}
}


