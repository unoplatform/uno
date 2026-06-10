using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Xaml.Core;
using Windows.UI.ViewManagement;

namespace Uno.UI.Tests.Windows_UI_Xaml_Internal;

[TestClass]
public class Given_TextScaleHelper
{
	[TestMethod]
	public void When_NoScaling_Returns_Original()
	{
		Assert.AreEqual(14.0, TextScaleHelper.GetScaledFontSize(14, 1.0, true));
	}

	[TestMethod]
	public void When_ScalingDisabled_Returns_Original()
	{
		Assert.AreEqual(14.0, TextScaleHelper.GetScaledFontSize(14, 2.0, false));
	}

	[TestMethod]
	public void When_FontSizeZero_Returns_Zero()
	{
		Assert.AreEqual(0.0, TextScaleHelper.GetScaledFontSize(0, 2.0, true));
	}

	[TestMethod]
	public void When_FontSizeNegative_Returns_Original()
	{
		Assert.AreEqual(-5.0, TextScaleHelper.GetScaledFontSize(-5, 2.0, true));
	}

	[TestMethod]
	public void When_FontSizeLessThanOne_Clamped()
	{
		var result = TextScaleHelper.GetScaledFontSize(0.5, 2.0, true);
		// Should be capped at 1.0 for the formula
		Assert.IsTrue(result >= 1.0);
	}

	[TestMethod]
	public void When_Scaled_SmallFonts_Scale_More_Than_Large()
	{
		var smallScaled = TextScaleHelper.GetScaledFontSize(10, 2.0, true);
		var largeScaled = TextScaleHelper.GetScaledFontSize(30, 2.0, true);

		var smallRatio = smallScaled / 10.0;
		var largeRatio = largeScaled / 30.0;

		// Small fonts should receive proportionally more scaling
		Assert.IsTrue(smallRatio > largeRatio,
			$"Small ratio ({smallRatio}) should be greater than large ratio ({largeRatio})");
	}

	[TestMethod]
	public void When_FontScaleLessThanOne_Returns_Original()
	{
		// Android/iOS can report scales below 1.0. WinUI only supports >= 1.0,
		// so we clamp to avoid negative or reduced font sizes.
		var result = TextScaleHelper.GetScaledFontSize(1, 0.8, true);
		Assert.AreEqual(1.0, result, "fontScale < 1.0 should return the original font size unchanged");

		var result2 = TextScaleHelper.GetScaledFontSize(14, 0.5, true);
		Assert.AreEqual(14.0, result2, "fontScale < 1.0 should return the original font size unchanged");
	}

	[TestMethod]
	public void When_Scaled_Result_Greater_Than_Input()
	{
		var result = TextScaleHelper.GetScaledFontSize(14, 1.5, true);
		Assert.IsTrue(result > 14.0, $"Scaled size ({result}) should be greater than original (14)");
	}

	[TestMethod]
	public void When_GetEffectiveFontScale_IgnoreOverrides()
	{
		var original = FeatureConfiguration.Font.IgnoreTextScaleFactor;
		try
		{
			FeatureConfiguration.Font.IgnoreTextScaleFactor = true;
			Assert.AreEqual(1.0, TextScaleHelper.GetEffectiveFontScale(2.0));
		}
		finally
		{
			FeatureConfiguration.Font.IgnoreTextScaleFactor = original;
		}
	}

	[TestMethod]
	public void When_GetEffectiveFontScale_ManualOverride()
	{
		var original = FeatureConfiguration.Font.TextScaleFactor;
		try
		{
			FeatureConfiguration.Font.TextScaleFactor = 1.5;
			Assert.AreEqual(1.5, TextScaleHelper.GetEffectiveFontScale(2.0));
		}
		finally
		{
			FeatureConfiguration.Font.TextScaleFactor = original;
		}
	}

	[TestMethod]
	public void When_GetEffectiveFontScale_MaximumClamps()
	{
		var originalMax = FeatureConfiguration.Font.MaximumTextScaleFactor;
		var originalManual = FeatureConfiguration.Font.TextScaleFactor;
		try
		{
			FeatureConfiguration.Font.MaximumTextScaleFactor = 1.5f;
			FeatureConfiguration.Font.TextScaleFactor = null;
			Assert.AreEqual(1.5, TextScaleHelper.GetEffectiveFontScale(2.0));
		}
		finally
		{
			FeatureConfiguration.Font.MaximumTextScaleFactor = originalMax;
			FeatureConfiguration.Font.TextScaleFactor = originalManual;
		}
	}

	[TestMethod]
	public void When_GetEffectiveFontScale_LessThanOne_Clamps_To_One()
	{
		var originalManual = FeatureConfiguration.Font.TextScaleFactor;
		var originalMax = FeatureConfiguration.Font.MaximumTextScaleFactor;
		try
		{
			FeatureConfiguration.Font.TextScaleFactor = null;
			FeatureConfiguration.Font.MaximumTextScaleFactor = null;

			Assert.AreEqual(1.0, TextScaleHelper.GetEffectiveFontScale(0.8));
		}
		finally
		{
			FeatureConfiguration.Font.TextScaleFactor = originalManual;
			FeatureConfiguration.Font.MaximumTextScaleFactor = originalMax;
		}
	}

	[TestMethod]
	public void When_ManualOverride_Changes_UpdateFontScale_Reevaluates_EffectiveScale()
	{
		var coreServices = CoreServices.Instance;
		var originalManual = FeatureConfiguration.Font.TextScaleFactor;
		var originalMax = FeatureConfiguration.Font.MaximumTextScaleFactor;
		var originalIgnore = FeatureConfiguration.Font.IgnoreTextScaleFactor;
		try
		{
			FeatureConfiguration.Font.MaximumTextScaleFactor = null;
			FeatureConfiguration.Font.IgnoreTextScaleFactor = false;
			FeatureConfiguration.Font.TextScaleFactor = 1.0;
			coreServices.UpdateFontScale(1.0);

			var uiSettings = new UISettings();
			var changeCount = 0;
			uiSettings.TextScaleFactorChanged += (_, _) => changeCount++;

			FeatureConfiguration.Font.TextScaleFactor = 1.5;
			coreServices.UpdateFontScale(1.0);

			Assert.AreEqual(1, changeCount,
				"Changing the manual text scale override should trigger effective-scale invalidation even when the OS scale is unchanged.");
		}
		finally
		{
			FeatureConfiguration.Font.TextScaleFactor = originalManual;
			FeatureConfiguration.Font.MaximumTextScaleFactor = originalMax;
			FeatureConfiguration.Font.IgnoreTextScaleFactor = originalIgnore;
			coreServices.UpdateFontScale(1.0);
		}
	}
}
