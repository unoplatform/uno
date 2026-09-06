using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation
{
	[TestClass]
	public class Given_AutomationPeer
	{
		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetHeadingLevel()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetHeadingLevel();
			Assert.AreEqual(AutomationHeadingLevel.None, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_IsDialog()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.IsDialog();
			Assert.IsFalse(result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetPattern()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetPattern(PatternInterface.Drag);
			Assert.IsNull(result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetAcceleratorKey()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetAcceleratorKey();
			Assert.AreEqual(string.Empty, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetAccessKey()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetAccessKey();
			Assert.AreEqual(string.Empty, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetAutomationControlType()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetAutomationControlType();
			Assert.AreEqual(AutomationControlType.Custom, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetAutomationId()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetAutomationId();
			Assert.AreEqual(string.Empty, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetBoundingRectangle()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetBoundingRectangle();
			Assert.AreEqual(default, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetChildren()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetChildren();
			Assert.IsNull(result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetClassName()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetClassName();
			Assert.AreEqual(string.Empty, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetClickablePoint()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetClickablePoint();
			Assert.AreEqual(default, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia | RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_FrameworkElementAutomationPeer_GetClickablePoint()
		{
			var button = new Button
			{
				Content = "Subject",
				Width = 120,
				Height = 40,
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				Margin = new Thickness(40, 30, 0, 0),
			};

			try
			{
				await UITestHelper.Load(new Grid
				{
					Width = 300,
					Height = 200,
					Children = { button },
				});

				var peer = FrameworkElementAutomationPeer.CreatePeerForElement(button);
				Assert.IsNotNull(peer);

				var bounds = peer.GetBoundingRectangle();
				var point = peer.GetClickablePoint();

				Assert.AreEqual(bounds.X + bounds.Width / 2, point.X, 0.5);
				Assert.AreEqual(bounds.Y + bounds.Height / 2, point.Y, 0.5);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
		public async Task When_PartiallyClipped_FrameworkElementAutomationPeer_GetClickablePoint()
		{
			var button = new Button
			{
				Content = "Subject",
				Width = 120,
				Height = 40,
			};
			var scrollViewer = new ScrollViewer
			{
				Width = 80,
				Height = 20,
				HorizontalScrollMode = ScrollMode.Enabled,
				HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
				VerticalScrollMode = ScrollMode.Enabled,
				VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
				Content = button,
			};

			try
			{
				await UITestHelper.Load(scrollViewer);

				var peer = FrameworkElementAutomationPeer.CreatePeerForElement(button);
				Assert.IsNotNull(peer);

				var bounds = peer.GetBoundingRectangle();
				var point = peer.GetClickablePoint();

				Assert.IsTrue(bounds.Width < button.ActualWidth);
				Assert.IsTrue(bounds.Height < button.ActualHeight);
				Assert.AreEqual(bounds.X + bounds.Width / 2, point.X, 0.5);
				Assert.AreEqual(bounds.Y + bounds.Height / 2, point.Y, 0.5);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
		public async Task When_SelfClipped_FrameworkElementAutomationPeer_GetClickablePoint()
		{
			var button = new Button
			{
				Content = "Subject",
				Width = 120,
				Height = 40,
				Clip = new RectangleGeometry { Rect = new Rect(0, 0, 20, 40) },
			};

			try
			{
				await UITestHelper.Load(button);

				var peer = FrameworkElementAutomationPeer.CreatePeerForElement(button);
				Assert.IsNotNull(peer);

				var clippedBounds = button
					.TransformToVisual(null)
					.TransformBounds(new Rect(0, 0, 20, 40));
				var point = peer.GetClickablePoint();

				Assert.AreEqual(clippedBounds.X + clippedBounds.Width / 2, point.X, 0.5);
				Assert.AreEqual(clippedBounds.Y + clippedBounds.Height / 2, point.Y, 0.5);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia | RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_ToggleSwitchAutomationPeer_GetClickablePoint()
		{
			var toggleSwitch = new ToggleSwitch
			{
				Header = "Subject",
				Width = 160,
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				Margin = new Thickness(40, 30, 0, 0),
			};

			try
			{
				await UITestHelper.Load(new Grid
				{
					Width = 300,
					Height = 200,
					Children = { toggleSwitch },
				});

				var peer = FrameworkElementAutomationPeer.CreatePeerForElement(toggleSwitch);
				Assert.IsNotNull(peer);

				var thumb = toggleSwitch.FindFirstDescendant<Thumb>();
				Assert.IsNotNull(thumb);

				var thumbPeer = FrameworkElementAutomationPeer.CreatePeerForElement(thumb);
				Assert.IsNotNull(thumbPeer);

				var thumbBounds = thumbPeer.GetBoundingRectangle();
				var point = peer.GetClickablePoint();

				Assert.AreEqual(thumbBounds.X + thumbBounds.Width / 2, point.X, 0.5);
				Assert.AreEqual(thumbBounds.Y + thumbBounds.Height / 2, point.Y, 0.5);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
		public async Task When_ColorSpectrumAutomationPeer_GetClickablePoint()
		{
			var colorSpectrum = new ColorSpectrum
			{
				Width = 300,
				Height = 160,
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				Margin = new Thickness(40, 30, 0, 0),
			};

			try
			{
				await UITestHelper.Load(new Grid
				{
					Width = 400,
					Height = 240,
					Children = { colorSpectrum },
				});
				await WindowHelper.WaitForIdle();

				var peer = FrameworkElementAutomationPeer.CreatePeerForElement(colorSpectrum);
				Assert.IsNotNull(peer);

				var inputTarget = colorSpectrum.FindFirstDescendant<FrameworkElement>("InputTarget")
					?? throw new InvalidOperationException("ColorSpectrum InputTarget not found.");
				var inputBounds = inputTarget
					.TransformToVisual(null)
					.TransformBounds(new Rect(0, 0, inputTarget.ActualWidth, inputTarget.ActualHeight));
				var point = peer.GetClickablePoint();

				Assert.IsTrue(inputBounds.Width > 0);
				Assert.IsTrue(inputBounds.Height > 0);
				Assert.AreEqual(inputBounds.X + inputBounds.Width / 2, point.X, 0.5);
				Assert.AreEqual(inputBounds.Y + inputBounds.Height / 2, point.Y, 0.5);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetHelpText()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetHelpText();
			Assert.AreEqual(string.Empty, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetItemStatus()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetItemStatus();
			Assert.AreEqual(string.Empty, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetItemType()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetItemType();
			Assert.AreEqual(string.Empty, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetLabeledBy()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetLabeledBy();
			Assert.IsNull(result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetLocalizedControlType()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetLocalizedControlType();
			Assert.AreEqual("custom", result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetName()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetName();
			Assert.AreEqual(string.Empty, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetOrientation()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetOrientation();
			Assert.AreEqual(AutomationOrientation.None, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_HasKeyboardFocus()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.HasKeyboardFocus();
			Assert.IsFalse(result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_IsContentElement()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.IsContentElement();
			Assert.IsFalse(result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_IsControlElement()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.IsControlElement();
			Assert.IsFalse(result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_IsEnabled()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.IsEnabled();
			Assert.IsTrue(result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_IsKeyboardFocusable()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.IsKeyboardFocusable();
			Assert.IsFalse(result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_IsOffscreen()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.IsOffscreen();
			Assert.IsFalse(result);
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)] // IsOffscreen clipping is Skia-only.
		public async Task When_Element_Scrolled_Out_Of_ScrollViewer_IsOffscreen()
		{
			var target = new Button { Content = "Subject", Width = 120, Height = 40 };
			var scrollViewer = new ScrollViewer
			{
				Width = 200,
				Height = 200,
				Content = new StackPanel
				{
					Children =
					{
						target,
						new Border { Height = 1000 },
					},
				},
			};

			try
			{
				await UITestHelper.Load(scrollViewer);

				var peer = FrameworkElementAutomationPeer.CreatePeerForElement(target);
				Assert.IsNotNull(peer);

				// The target sits at the top of the viewport, so it is visible.
				Assert.IsFalse(peer.IsOffscreen(), "Element should be on-screen before scrolling.");

				// Scroll the target completely above the viewport.
				scrollViewer.ChangeView(null, 500, null, disableAnimation: true);
				await WindowHelper.WaitForEqual(500, () => scrollViewer.VerticalOffset);
				await WindowHelper.WaitForIdle();

				Assert.IsTrue(peer.IsOffscreen(), "Element should be off-screen after being scrolled out of the viewport.");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)] // IsOffscreen clipping is Skia-only.
		public async Task When_Popup_Content_InWindow_IsOffscreen()
		{
			var target = new Button { Content = "Subject", Width = 120, Height = 40 };
			var popup = new Popup
			{
				XamlRoot = WindowHelper.XamlRoot,
				HorizontalOffset = 50,
				VerticalOffset = 50,
				Child = target,
			};

			try
			{
				popup.IsOpen = true;
				await WindowHelper.WaitForLoaded(target);
				await WindowHelper.WaitForIdle();

				var peer = FrameworkElementAutomationPeer.CreatePeerForElement(target);
				Assert.IsNotNull(peer);

				Assert.IsFalse(peer.IsOffscreen(), "In-window open popup content should be on-screen (false).");
			}
			finally
			{
				popup.IsOpen = false;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)] // IsOffscreen clipping is Skia-only.
		public async Task When_Popup_Content_Outside_Window_IsOffscreen()
		{
			var target = new Button { Content = "Subject", Width = 120, Height = 40 };
			var popup = new Popup
			{
				XamlRoot = WindowHelper.XamlRoot,
				HorizontalOffset = 100000,
				VerticalOffset = 100000,
				Child = target,
			};

			try
			{
				popup.IsOpen = true;
				await WindowHelper.WaitForLoaded(target);
				await WindowHelper.WaitForIdle();

				var peer = FrameworkElementAutomationPeer.CreatePeerForElement(target);
				Assert.IsNotNull(peer);

				Assert.IsTrue(peer.IsOffscreen(), "Popup content far outside the window should be off-screen (true).");
			}
			finally
			{
				popup.IsOpen = false;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)] // IsOffscreen clipping is Skia-only.
		public async Task When_Element_Scrolled_Out_Inside_Popup_IsOffscreen()
		{
			var target = new Button { Content = "Subject", Width = 120, Height = 40 };
			var scrollViewer = new ScrollViewer
			{
				Width = 200,
				Height = 200,
				Content = new StackPanel
				{
					Children =
					{
						target,
						new Border { Height = 1000 },
					},
				},
			};
			var popup = new Popup
			{
				XamlRoot = WindowHelper.XamlRoot,
				HorizontalOffset = 50,
				VerticalOffset = 50,
				Child = scrollViewer,
			};

			try
			{
				popup.IsOpen = true;
				await WindowHelper.WaitForLoaded(scrollViewer);
				await WindowHelper.WaitForIdle();

				var peer = FrameworkElementAutomationPeer.CreatePeerForElement(target);
				Assert.IsNotNull(peer);

				Assert.IsFalse(peer.IsOffscreen(), "Element should be on-screen before scrolling (inside popup).");

				scrollViewer.ChangeView(null, 500, null, disableAnimation: true);
				await WindowHelper.WaitForEqual(500, () => scrollViewer.VerticalOffset);
				await WindowHelper.WaitForIdle();

				Assert.IsTrue(peer.IsOffscreen(), "Element scrolled out of a popup's ScrollViewer should be off-screen (true).");
			}
			finally
			{
				popup.IsOpen = false;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_IsPassword()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.IsPassword();
			Assert.IsFalse(result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_IsRequiredForForm()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.IsRequiredForForm();
			Assert.IsFalse(result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_SetFocus()
		{
			var automationPeer = new TestAutomationPeer();
			// Should not throw
			automationPeer.SetFocus();
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetPeerFromPoint()
		{
			var automationPeer = new TestAutomationPeer();
#pragma warning disable CS0618 // Type or member is obsolete
			var result = automationPeer.GetPeerFromPoint(default);
#pragma warning restore CS0618 // Type or member is obsolete
			Assert.AreEqual(automationPeer, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetLiveSetting()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetLiveSetting();
			Assert.AreEqual(AutomationLiveSetting.Off, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_Navigate()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.Navigate(default);
			Assert.IsNull(result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetElementFromPoint()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetElementFromPoint(default);
			Assert.AreEqual(automationPeer, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetFocusedElement()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetFocusedElement();
			Assert.AreEqual(automationPeer, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_ShowContextMenu()
		{
			var automationPeer = new TestAutomationPeer();
			// Should not throw
			automationPeer.ShowContextMenu();
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetControlledPeers()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetControlledPeers();
			Assert.IsNull(result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetAnnotations()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetAnnotations();
			Assert.IsNull(result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetPositionInSet()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetPositionInSet();
			Assert.AreEqual(-1, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetSizeOfSet()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetSizeOfSet();
			Assert.AreEqual(-1, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetLevel()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetLevel();
			Assert.AreEqual(-1, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetLandmarkType()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetLandmarkType();
			Assert.AreEqual(AutomationLandmarkType.None, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetLocalizedLandmarkType()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetLocalizedLandmarkType();
			Assert.AreEqual(string.Empty, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_IsPeripheral()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.IsPeripheral();
			Assert.IsFalse(result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_IsDataValidForForm()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.IsDataValidForForm();
			Assert.IsTrue(result);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_AutomationPeer_Default_GetFullDescription()
		{
			var automationPeer = new TestAutomationPeer();
			var result = automationPeer.GetFullDescription();
			Assert.AreEqual(string.Empty, result);
		}

		[TestMethod]
		[RunsOnUIThread]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23802")]
		public async Task When_Button_With_PathIcon_Content_And_AutomationName()
		{
			var button = new Button
			{
				Width = 48,
				Height = 48,
				Content = new PathIcon { Data = new RectangleGeometry { Rect = new Rect(0, 0, 12, 12) } },
			};
			AutomationProperties.SetName(button, "Refresh");

			try
			{
				await UITestHelper.Load(button);

				var peer = FrameworkElementAutomationPeer.CreatePeerForElement(button);
				Assert.IsNotNull(peer);
				Assert.AreEqual("Refresh", peer.GetName());
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23802")]
		public async Task When_Button_With_PathIcon_Content_Without_AutomationName()
		{
			var button = new Button
			{
				Width = 48,
				Height = 48,
				Content = new PathIcon { Data = new RectangleGeometry { Rect = new Rect(0, 0, 12, 12) } },
			};

			try
			{
				await UITestHelper.Load(button);

				var peer = FrameworkElementAutomationPeer.CreatePeerForElement(button);
				Assert.IsNotNull(peer);

				// Icon content contributes no text — an icon-only button without AutomationProperties.Name is unnamed.
				Assert.AreEqual(string.Empty, peer.GetName());
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23802")]
		public async Task When_Button_With_String_Content_Then_GetName_Returns_Content()
		{
			var button = new Button { Content = "Help Center" };

			try
			{
				await UITestHelper.Load(button);

				var peer = FrameworkElementAutomationPeer.CreatePeerForElement(button);
				Assert.IsNotNull(peer);
				Assert.AreEqual("Help Center", peer.GetName());
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23802")]
		public async Task When_PathIcon_Has_No_AutomationPeer()
		{
			var pathIcon = new PathIcon { Data = new RectangleGeometry { Rect = new Rect(0, 0, 12, 12) } };
			var button = new Button
			{
				Width = 48,
				Height = 48,
				Content = pathIcon,
			};
			AutomationProperties.SetName(button, "Refresh");

			try
			{
				await UITestHelper.Load(button);

				Assert.IsNull(FrameworkElementAutomationPeer.CreatePeerForElement(pathIcon));

				var buttonPeer = FrameworkElementAutomationPeer.CreatePeerForElement(button);
				Assert.IsNotNull(buttonPeer);
				var children = buttonPeer.GetChildren();
				Assert.IsTrue(children == null || children.Count == 0, "An icon-only Button should be a leaf in the automation tree.");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		private class TestAutomationPeer : AutomationPeer
		{
			public TestAutomationPeer()
			{
			}
		}
	}
}
