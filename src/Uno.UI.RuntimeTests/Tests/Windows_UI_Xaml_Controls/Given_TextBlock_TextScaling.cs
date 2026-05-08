using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI.ViewManagement;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_TextBlock_TextScaling
{
	[TestMethod]
	public void When_IsTextScaleFactorEnabled_Default_Is_True()
	{
		var textBlock = new TextBlock();
		Assert.IsTrue(textBlock.IsTextScaleFactorEnabled);
	}

	[TestMethod]
	public void When_IsTextScaleFactorEnabled_Can_Be_Set_False()
	{
		var textBlock = new TextBlock();
		textBlock.IsTextScaleFactorEnabled = false;
		Assert.IsFalse(textBlock.IsTextScaleFactorEnabled);
	}

	[TestMethod]
	public void When_Control_IsTextScaleFactorEnabled_Default_Is_True()
	{
		var button = new Button();
		Assert.IsTrue(button.IsTextScaleFactorEnabled);
	}

	[TestMethod]
	public void When_ContentPresenter_IsTextScaleFactorEnabled_Default_Is_True()
	{
		var presenter = new ContentPresenter();
		Assert.IsTrue(presenter.IsTextScaleFactorEnabled);
	}

	[TestMethod]
	public void When_TextScaleFactor_Returns_Valid_Value()
	{
		var uiSettings = new UISettings();
		Assert.IsTrue(uiSettings.TextScaleFactor > 0,
			$"TextScaleFactor should be a positive value, got {uiSettings.TextScaleFactor}");
	}

#if __SKIA__
	[TestMethod]
	public async Task When_TextScaleFactorEnabled_Size_Is_Larger()
	{
		var originalOverride = FeatureConfiguration.Font.TextScaleFactor;
		try
		{
			FeatureConfiguration.Font.TextScaleFactor = 1.5;

			var scaledBlock = new TextBlock
			{
				Text = "Test text",
				FontSize = 14,
				IsTextScaleFactorEnabled = true
			};

			var unscaledBlock = new TextBlock
			{
				Text = "Test text",
				FontSize = 14,
				IsTextScaleFactorEnabled = false
			};

			var panel = new StackPanel
			{
				Children = { scaledBlock, unscaledBlock }
			};

			await UITestHelper.Load(panel);

			Assert.IsTrue(scaledBlock.DesiredSize.Height > unscaledBlock.DesiredSize.Height,
				$"Scaled height ({scaledBlock.DesiredSize.Height}) should be larger than unscaled ({unscaledBlock.DesiredSize.Height})");
			Assert.IsTrue(scaledBlock.DesiredSize.Width > unscaledBlock.DesiredSize.Width,
				$"Scaled width ({scaledBlock.DesiredSize.Width}) should be larger than unscaled ({unscaledBlock.DesiredSize.Width})");
		}
		finally
		{
			FeatureConfiguration.Font.TextScaleFactor = originalOverride;
		}
	}

	[TestMethod]
	public async Task When_TextScaleFactorDisabled_No_Scaling()
	{
		var originalOverride = FeatureConfiguration.Font.TextScaleFactor;
		try
		{
			FeatureConfiguration.Font.TextScaleFactor = 1.5;

			var textBlock = new TextBlock
			{
				Text = "Test text",
				FontSize = 14,
				IsTextScaleFactorEnabled = false
			};

			await UITestHelper.Load(textBlock);

			var sizeWithScaleDisabled = textBlock.DesiredSize;

			// Now enable scaling on the same block
			textBlock.IsTextScaleFactorEnabled = true;
			textBlock.InvalidateMeasure();
			await TestServices.WindowHelper.WaitForIdle();

			var sizeWithScaleEnabled = textBlock.DesiredSize;

			Assert.IsTrue(sizeWithScaleEnabled.Height > sizeWithScaleDisabled.Height,
				$"Enabled height ({sizeWithScaleEnabled.Height}) should be larger than disabled ({sizeWithScaleDisabled.Height})");
		}
		finally
		{
			FeatureConfiguration.Font.TextScaleFactor = originalOverride;
		}
	}

	[TestMethod]
	public async Task When_IsTextScaleFactorEnabled_Propagates_To_Inlines()
	{
		var originalOverride = FeatureConfiguration.Font.TextScaleFactor;
		try
		{
			FeatureConfiguration.Font.TextScaleFactor = 1.5;

			var scaledBlock = new TextBlock
			{
				Text = "Test text",
				FontSize = 14,
				IsTextScaleFactorEnabled = true
			};

			var unscaledBlock = new TextBlock
			{
				Text = "Test text",
				FontSize = 14,
				IsTextScaleFactorEnabled = false
			};

			var panel = new StackPanel
			{
				Children = { scaledBlock, unscaledBlock }
			};

			await UITestHelper.Load(panel);

			// Verify IsTextScaleFactorEnabled propagated to child inlines
			Assert.IsTrue(scaledBlock.Inlines.Count > 0, "Scaled block should have inlines");
			Assert.IsTrue(unscaledBlock.Inlines.Count > 0, "Unscaled block should have inlines");

			// The inlines should inherit the parent's IsTextScaleFactorEnabled
			foreach (var inline in unscaledBlock.Inlines)
			{
				Assert.IsFalse(inline.IsTextScaleFactorEnabled,
					"Inlines should inherit IsTextScaleFactorEnabled=false from parent TextBlock");
			}

			// Verify via size difference that scaling is actually different
			Assert.IsTrue(scaledBlock.DesiredSize.Height > unscaledBlock.DesiredSize.Height,
				$"Scaled block ({scaledBlock.DesiredSize.Height}) should be taller than unscaled ({unscaledBlock.DesiredSize.Height})");
		}
		finally
		{
			FeatureConfiguration.Font.TextScaleFactor = originalOverride;
		}
	}

	[TestMethod]
	public async Task When_TextScaleFactorChanges_Nested_Inlines_Are_Invalidated()
	{
		var originalOverride = FeatureConfiguration.Font.TextScaleFactor;
		try
		{
			FeatureConfiguration.Font.TextScaleFactor = 1.0;

			var textBlock = new TextBlock
			{
				FontSize = 14,
				IsTextScaleFactorEnabled = true,
				Inlines =
				{
					new Span
					{
						Inlines =
						{
							new Run { Text = "Nested text scaling" }
						}
					}
				}
			};

			await UITestHelper.Load(textBlock);
			var unscaledSize = textBlock.DesiredSize;

			FeatureConfiguration.Font.TextScaleFactor = 1.5;
			global::Uno.UI.Xaml.Core.CoreServices.Instance.UpdateFontScale(1.5);
			await TestServices.WindowHelper.WaitForIdle();

			var scaledSize = textBlock.DesiredSize;
			Assert.IsTrue(scaledSize.Height > unscaledSize.Height,
				$"Nested inline height ({scaledSize.Height}) should be larger than unscaled ({unscaledSize.Height})");
		}
		finally
		{
			FeatureConfiguration.Font.TextScaleFactor = originalOverride;
			global::Uno.UI.Xaml.Core.CoreServices.Instance.UpdateFontScale(originalOverride ?? 1.0);
		}
	}
#endif
}
