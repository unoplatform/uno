// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml.Markup;
using MUXControlsTestApp.Utilities;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Common;
using System.Threading;


#if USING_TAEF
using WEX.TestExecution;
using WEX.TestExecution.Markup;
using WEX.Logging.Interop;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
#endif

using TeachingTip = Microsoft/* UWP don't rename */.UI.Xaml.Controls.TeachingTip;
using IconSource = Microsoft/* UWP don't rename */.UI.Xaml.Controls.IconSource;
using SymbolIconSource = Microsoft/* UWP don't rename */.UI.Xaml.Controls.SymbolIconSource;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
using Microsoft/* UWP don't rename */.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Shapes;

#if !HAS_UNO_WINUI
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
#endif

using Private.Infrastructure;
using System.Threading.Tasks;
using Uno.UI.RuntimeTests.Helpers;
using Microsoft.UI.Private.Controls;

namespace Microsoft.UI.Xaml.Tests.MUXControls.ApiTests;

[TestClass]
public class TeachingTipTests
{
	[TestMethod]
	[Ignore("Fails on Uno targets due to #15755")]
	[TestProperty("TestPass:IncludeOnlyOn", "Desktop")] // TeachingTip doesn't appear to show up correctly in OneCore.
	public async Task TeachingTipBackgroundTest()
	{
		TeachingTip teachingTip = null, teachingTipLightDismiss = null;
		SolidColorBrush blueBrush = null;
		Brush lightDismissBackgroundBrush = null;
		var loadedEvent = new UnoAutoResetEvent(false);
		RunOnUIThread.Execute(() =>
		{
			Grid root = new Grid();
			teachingTip = new TeachingTip();
			teachingTip.Loaded += (object sender, RoutedEventArgs args) => { loadedEvent.Set(); };

			teachingTipLightDismiss = new TeachingTip();
			teachingTipLightDismiss.IsLightDismissEnabled = true;

			// Set LightDismiss background before show... it shouldn't take effect in the tree
			blueBrush = new SolidColorBrush(Colors.Blue);
			teachingTipLightDismiss.Background = blueBrush;

			root.Resources.Add("TeachingTip", teachingTip);
			root.Resources.Add("TeachingTipLightDismiss", teachingTipLightDismiss);

			lightDismissBackgroundBrush = MUXControlsTestApp.App.Current.Resources["TeachingTipTransientBackground"] as Brush;
			Verify.IsNotNull(lightDismissBackgroundBrush, "lightDismissBackgroundBrush");

			teachingTip.IsOpen = true;
			teachingTipLightDismiss.IsOpen = true;

			MUXControlsTestApp.App.TestContentRoot = root;
		});

		await TestServices.WindowHelper.WaitForIdle();
		await loadedEvent.WaitOne();
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var redBrush = new SolidColorBrush(Colors.Red);
			teachingTip.SetValue(TeachingTip.BackgroundProperty, redBrush);
			Verify.AreSame(redBrush, teachingTip.GetValue(TeachingTip.BackgroundProperty) as Brush);
			Verify.AreSame(redBrush, teachingTip.Background);

			teachingTip.Background = blueBrush;
			Verify.AreSame(blueBrush, teachingTip.Background);

			{
				var popup = TeachingTipTestHooks.GetPopup(teachingTip);
				var child = popup.Child;
				var grandChild = VisualTreeHelper.GetChild(child, 0);
				Verify.AreSame(blueBrush, ((Grid)grandChild).Background, "Checking TeachingTip.Background TemplateBinding works");
			}

			{
				var popup = TeachingTipTestHooks.GetPopup(teachingTipLightDismiss);
				var child = popup.Child as Grid;

				Log.Comment("Checking LightDismiss TeachingTip Background is using resource for first invocation");

				Grid tailEdgeBorder = VisualTreeUtils.FindVisualChildByName(child, "TailEdgeBorder") as Grid;
				Polygon tailPolygon = VisualTreeUtils.FindVisualChildByName(child, "TailPolygon") as Polygon;
				Polygon topTailPolygonHighlight = VisualTreeUtils.FindVisualChildByName(child, "TopTailPolygonHighlight") as Polygon;
				Grid contentRootGrid = VisualTreeUtils.FindVisualChildByName(child, "ContentRootGrid") as Grid;
				ContentPresenter mainContentPresenter = VisualTreeUtils.FindVisualChildByName(child, "MainContentPresenter") as ContentPresenter;
				Border heroContentBorder = VisualTreeUtils.FindVisualChildByName(child, "HeroContentBorder") as Border;

				VerifyLightDismissTipBackground(tailEdgeBorder.Background, "TailEdgeBorder");
				VerifyLightDismissTipBackground(tailPolygon.Fill, "TailPolygon");
				VerifyLightDismissTipBackground(topTailPolygonHighlight.Fill, "TopTailPolygonHighlight");
				VerifyLightDismissTipBackground(contentRootGrid.Background, "ContentRootGrid");
				VerifyLightDismissTipBackground(mainContentPresenter.Background, "MainContentPresenter");
				VerifyLightDismissTipBackground(heroContentBorder.Background, "HeroContentBorder");

				void VerifyLightDismissTipBackground(Brush brush, string uiPart)
				{
					if (lightDismissBackgroundBrush != brush)
					{
						if (brush is SolidColorBrush actualSolidBrush)
						{
							string teachingTipMessage = $"LightDismiss TeachingTip's {uiPart} Background is SolidColorBrush with color {actualSolidBrush.Color}";
							Log.Comment(teachingTipMessage);
							Verify.Fail(teachingTipMessage);
						}
						else
						{
							Verify.AreSame(lightDismissBackgroundBrush, brush, $"Checking LightDismiss TeachingTip's {uiPart} Background is using resource for first invocation");
						}
					}
				}
			}

			teachingTip.IsLightDismissEnabled = true;
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(blueBrush.Color, ((SolidColorBrush)teachingTip.Background).Color);

			var popup = TeachingTipTestHooks.GetPopup(teachingTip);
			var child = popup.Child as Grid;

			Grid tailEdgeBorder = VisualTreeUtils.FindVisualChildByName(child, "TailEdgeBorder") as Grid;
			Polygon tailPolygon = VisualTreeUtils.FindVisualChildByName(child, "TailPolygon") as Polygon;
			Polygon topTailPolygonHighlight = VisualTreeUtils.FindVisualChildByName(child, "TopTailPolygonHighlight") as Polygon;
			Grid contentRootGrid = VisualTreeUtils.FindVisualChildByName(child, "ContentRootGrid") as Grid;
			ContentPresenter mainContentPresenter = VisualTreeUtils.FindVisualChildByName(child, "MainContentPresenter") as ContentPresenter;
			Border heroContentBorder = VisualTreeUtils.FindVisualChildByName(child, "HeroContentBorder") as Border;

			VerifyBackgroundChanged(tailEdgeBorder.Background, "TailEdgeBorder");
			VerifyBackgroundChanged(tailPolygon.Fill, "TailPolygon");
			VerifyBackgroundChanged(topTailPolygonHighlight.Fill, "TopTailPolygonHighlight");
			VerifyBackgroundChanged(contentRootGrid.Background, "ContentRootGrid");
			VerifyBackgroundChanged(mainContentPresenter.Background, "MainContentPresenter");
			VerifyBackgroundChanged(heroContentBorder.Background, "HeroContentBorder");

			void VerifyBackgroundChanged(Brush brush, string uiPart)
			{
				// If we can no longer cast the background brush to a solid color brush then changing the
				// IsLightDismissEnabled has changed the background as we expected it to.
				if (brush is SolidColorBrush solidColorBrush)
				{
					Verify.AreNotEqual(blueBrush.Color, solidColorBrush.Color, $"TeachingTip's {uiPart} Background should have changed");
				}
			}
		});
	}

	[TestMethod]
	public async Task TeachingTipWithContentAndWithoutHeroContentDoesNotCrash()
	{
		var loadedEvent = new UnoAutoResetEvent(false);
		RunOnUIThread.Execute(() =>
		{
			Grid contentGrid = new Grid();
			SymbolIconSource iconSource = new SymbolIconSource();
			iconSource.Symbol = Symbol.People;
			TeachingTip teachingTip = new TeachingTip();
			teachingTip.Content = contentGrid;
			teachingTip.IconSource = (IconSource)iconSource;
			teachingTip.Loaded += (object sender, RoutedEventArgs args) => { loadedEvent.Set(); };
			TestServices.WindowHelper.WindowContent = teachingTip;
		});

		await TestServices.WindowHelper.WaitForIdle();
		await loadedEvent.WaitOne();
	}

	[TestMethod]
	public async Task TeachingTipWithContentAndWithoutIconSourceDoesNotCrash()
	{
		var loadedEvent = new UnoAutoResetEvent(false);
		RunOnUIThread.Execute(() =>
		{
			Grid contentGrid = new Grid();
			Grid heroGrid = new Grid();
			TeachingTip teachingTip = new TeachingTip();
			teachingTip.Content = contentGrid;
			teachingTip.HeroContent = heroGrid;
			teachingTip.Loaded += (object sender, RoutedEventArgs args) => { loadedEvent.Set(); };
			TestServices.WindowHelper.WindowContent = teachingTip;
		});

		await TestServices.WindowHelper.WaitForIdle();
		await loadedEvent.WaitOne();
	}

	[TestMethod]
	public async Task PropagatePropertiesDown()
	{
		TextBlock content = null;
		TeachingTip tip = null;
		RunOnUIThread.Execute(() =>
		{
			content = new TextBlock()
			{
				Text = "Some text"
			};

			tip = new TeachingTip()
			{
				Content = content,
				FontSize = 22,
				Foreground = new SolidColorBrush()
				{
					Color = Colors.Red
				}
			};

			TestServices.WindowHelper.WindowContent = tip;
			tip.UpdateLayout();
			tip.IsOpen = true;
			tip.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.IsTrue(Math.Abs(22 - content.FontSize) < 1);
			var foregroundBrush = content.Foreground as SolidColorBrush;
			Verify.AreEqual(Colors.Red, foregroundBrush.Color);
		});
	}


	[TestMethod]
	public void TeachingTipHeroContentPlacementTest()
	{
		RunOnUIThread.Execute(() =>
		{
			foreach (var iPlacementMode in Enum.GetValues(typeof(TeachingTipHeroContentPlacementMode)))
			{
				var placementMode = (TeachingTipHeroContentPlacementMode)iPlacementMode;

				Log.Comment($"Verifying TeachingTipHeroContentPlacementMode [{placementMode}]");

				TeachingTip teachingTip = new TeachingTip();
				teachingTip.HeroContentPlacement = placementMode;

				// Open the teaching tip to enter the correct visual state for the HeroContentPlacement.
				teachingTip.IsOpen = true;

				TestServices.WindowHelper.WindowContent = teachingTip;
				teachingTip.UpdateLayout();

				Verify.IsTrue(teachingTip.HeroContentPlacement == placementMode, $"HeroContentPlacement should have been [{placementMode}]");

				var root = VisualTreeUtils.FindVisualChildByName(teachingTip, "Container") as FrameworkElement;

				switch (placementMode)
				{
					case TeachingTipHeroContentPlacementMode.Auto:
						Verify.IsTrue(IsVisualStateActive(root, "HeroContentPlacementStates", "HeroContentTop"),
							"The [HeroContentTop] visual state should have been active");
						break;
					case TeachingTipHeroContentPlacementMode.Top:
						Verify.IsTrue(IsVisualStateActive(root, "HeroContentPlacementStates", "HeroContentTop"),
							"The [HeroContentTop] visual state should have been active");
						break;
					case TeachingTipHeroContentPlacementMode.Bottom:
						Verify.IsTrue(IsVisualStateActive(root, "HeroContentPlacementStates", "HeroContentBottom"),
							"The [HeroContentBottom] visual state should have been active");
						break;
				}
			}
		});

		bool IsVisualStateActive(FrameworkElement root, string groupName, string stateName)
		{
			foreach (var group in VisualStateManager.GetVisualStateGroups(root))
			{
				if (group.Name == groupName)
				{
					return group.CurrentState != null && group.CurrentState.Name == stateName;
				}
			}

			return false;
		}
	}
}
