using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using Windows.Graphics;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Tests.Enterprise;
using static Private.Infrastructure.TestServices;
using Windows.UI.Input.Preview.Injection;
using Microsoft.UI.Xaml.Automation.Peers;
using MUXControlsTestApp.Utilities;
using Combinatorial.MSTest;
using Microsoft.UI.Windowing;
using Private.Infrastructure;
using Uno.UI.Extensions;
using Uno.UI.Toolkit.DevTools.Input;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_ContentDialog
	{
#if HAS_UNO
		[TestMethod]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif

		public async Task When_Press_Should_Not_Lose_Focus()
		{
			var SUT = new MyContentDialog
			{
				Content = "Hello World",
				PrimaryButtonText = "OK",
			};

			SetXamlRootForIslandsOrWinUI(SUT);

			try
			{
				await ShowDialog(SUT);

				SUT.ContentScrollViewer.Background = new SolidColorBrush(Microsoft.UI.Colors.Red);

				await WindowHelper.WaitForIdle();

				var focused = FocusManager.GetFocusedElement(SUT.XamlRoot);
				Assert.IsInstanceOfType<Button>(focused);
				Assert.AreEqual("OK", ((Button)focused).Content.ToString());

				var bounds = SUT.ContentScrollViewer.GetAbsoluteBounds();
				var bottomRight = new Point(bounds.Right, bounds.Bottom);

				var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
				using var mouse = injector.GetMouse();

				mouse.MoveTo(bottomRight.X - 5, bottomRight.Y - 5);
				mouse.Press();
				await WindowHelper.WaitForIdle();
				mouse.Release();
				await WindowHelper.WaitForIdle();

				focused = FocusManager.GetFocusedElement(SUT.XamlRoot);
				Assert.IsInstanceOfType<Button>(focused);
				Assert.AreEqual("OK", ((Button)focused).Content.ToString());

				mouse.MoveTo(bottomRight.X - 5, bottomRight.Y + 5);
				mouse.Press();
				await WindowHelper.WaitForIdle();
				mouse.Release();
				await WindowHelper.WaitForIdle();

				focused = FocusManager.GetFocusedElement(SUT.XamlRoot);
				Assert.IsInstanceOfType<Button>(focused);
				Assert.AreEqual("OK", ((Button)focused).Content.ToString());
			}
			finally
			{
				SUT.Hide();
			}
		}
#endif

		[TestMethod]
		public async Task When_Not_FullSizeDesired()
		{
			var SUT = new MyContentDialog
			{
				Title = "Dialog title",
				Content = "Dialog content",
				PrimaryButtonText = "Accept",
				SecondaryButtonText = "Nope"
			};

			SetXamlRootForIslandsOrWinUI(SUT);

			try
			{
				await ShowDialog(SUT);

				Assert.IsNotNull(SUT.BackgroundElement);

				var actualHeight = SUT.BackgroundElement.ActualHeight;
				Assert.IsTrue(actualHeight > 0); // Is displayed
				Assert.IsTrue(actualHeight < 300); // Is not stretched
			}
			finally
			{
				SUT.Hide();
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaAndroid)] // Very flaky on Skia Android #9080
		public async Task When_FullSizeDesired()
		{
			var SUT = new MyContentDialog
			{
				Title = "Dialog title",
				Content = "Dialog content",
				PrimaryButtonText = "Accept",
				SecondaryButtonText = "Nope"
			};

			SetXamlRootForIslandsOrWinUI(SUT);

			SUT.FullSizeDesired = true;

			try
			{
				await ShowDialog(SUT);


				Assert.IsNotNull(SUT.BackgroundElement);

				var actualHeight = SUT.BackgroundElement.ActualHeight;
				Assert.IsTrue(actualHeight > 0); // Is displayed
				Assert.IsTrue(actualHeight > 400); // Is stretched to full size
			}
			finally
			{
				SUT.Hide();
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaIslands | RuntimeTestPlatforms.Native)]
		public async Task When_Uncapped_FullSizeDesired()
		{
			var SUT = new MyContentDialog(unconstrained: true)
			{
				Content = new Grid
				{
					BorderBrush = new SolidColorBrush(Colors.Red),
					BorderThickness = new Thickness(5),
				},
				Background = new SolidColorBrush(Colors.SkyBlue),
				PrimaryButtonText = "YES",
				SecondaryButtonText = "NO",
			};
			SUT.FullSizeDesired = true;
			SetXamlRootForIslandsOrWinUI(SUT);

			var nativeUnsafeArea = ScreenHelper.GetUnsafeArea();

			using (ScreenHelper.OverrideVisibleBounds(new Thickness(0, 38, 0, 72), skipIfHasNativeUnsafeArea: (nativeUnsafeArea.Top + nativeUnsafeArea.Bottom) > 50))
			{
				try
				{
					await ShowDialog(SUT);

					var bgeScreenRect = SUT.BackgroundElement.GetOnScreenBounds();
					var visibleBounds = ApplicationView.GetForCurrentView().VisibleBounds;

					var scale =
#if HAS_UNO && __SKIA__
						SUT.BackgroundElement.GetScaleFactorForLayoutRounding();
#else
						1;
#endif
					var roundingMargin = 0.5 / scale;

					Assert.AreEqual(visibleBounds.Top, bgeScreenRect.Top, roundingMargin * 2);
					Assert.AreEqual(visibleBounds.Height, bgeScreenRect.Height, roundingMargin * 2);
				}
				finally
				{
					SUT.Hide();
				}
			}
		}

		[TestMethod]
		public async Task When_DefaultButton_Not_Set()
		{
			var SUT = new MyContentDialog
			{
				Title = "Dialog title",
				Content = "Dialog content",
				PrimaryButtonText = "Accept",
				SecondaryButtonText = "Nope"
			};

			SetXamlRootForIslandsOrWinUI(SUT);

			Assert.AreEqual(ContentDialogButton.None, SUT.DefaultButton);

			try
			{
				await ShowDialog(SUT);


				Assert.IsNotNull(SUT.PrimaryButton);

				var fg = SUT.PrimaryButton.Foreground as SolidColorBrush;
				Assert.IsNotNull(fg);
				Assert.AreEqual(Colors.Black, fg.Color);
			}
			finally
			{
				SUT.Hide();
			}
		}

		[TestMethod]
		public async Task When_DefaultButton_Set()
		{
			var SUT = new MyContentDialog
			{
				Title = "Dialog title",
				Content = "Dialog content",
				PrimaryButtonText = "Accept",
				SecondaryButtonText = "Nope"
			};

			SetXamlRootForIslandsOrWinUI(SUT);

			SUT.DefaultButton = ContentDialogButton.Primary;

			try
			{
				await ShowDialog(SUT);


				Assert.IsNotNull(SUT.PrimaryButton);

				var fg = SUT.PrimaryButton.Foreground as SolidColorBrush;
				Assert.IsNotNull(fg);
				Assert.AreEqual(Colors.White, fg.Color);
			}
			finally
			{
				SUT.Hide();
			}
		}

		[TestMethod]
		[RequiresFullWindow]
		public async Task When_Initial_Focus_With_Focusable_Content()
		{
			Button button = new Button() { Content = "Target" };
			var SUT = new MyContentDialog
			{
				Title = "Dialog title",
				Content = button,
				PrimaryButtonText = "Target",
				SecondaryButtonText = "Secondary"
			};

			SetXamlRootForIslandsOrWinUI(SUT);

			SUT.DefaultButton = ContentDialogButton.None;

			try
			{
				await ShowDialog(SUT);

				var focused = FocusManager.GetFocusedElement(SUT.XamlRoot);

				Assert.AreEqual(button, focused);
			}
			finally
			{
				SUT.Hide();
			}
		}

		[TestMethod]
		[RequiresFullWindow]
		public async Task When_Initial_Focus_With_DefaultButton_Not_Set()
		{
			var SUT = new MyContentDialog
			{
				Title = "Dialog title",
				Content = "Dialog content",
				PrimaryButtonText = "Target",
				SecondaryButtonText = "Secondary"
			};

			SetXamlRootForIslandsOrWinUI(SUT);

			SUT.DefaultButton = ContentDialogButton.None;

			try
			{
				await ShowDialog(SUT);

				var focused = FocusManager.GetFocusedElement(SUT.XamlRoot);

				Assert.IsInstanceOfType(focused, typeof(Button));
				Assert.AreEqual("Target", ((Button)focused).Content);
			}
			finally
			{
				SUT.Hide();
			}
		}

		[TestMethod]
		[RequiresFullWindow]
		public async Task When_Initial_Focus_With_DefaultButton_Set()
		{
			var SUT = new MyContentDialog
			{
				Title = "Dialog title",
				Content = "Dialog content",
				PrimaryButtonText = "Accept",
				SecondaryButtonText = "Target"
			};

			SetXamlRootForIslandsOrWinUI(SUT);

			SUT.DefaultButton = ContentDialogButton.Secondary;

			try
			{
				await ShowDialog(SUT);

				var focused = FocusManager.GetFocusedElement(SUT.XamlRoot);

				Assert.IsInstanceOfType(focused, typeof(Button));
				Assert.AreEqual("Target", ((Button)focused).Content);
			}
			finally
			{
				SUT.Hide();
			}
		}

		[TestMethod]
		public async Task When_CloseDeferred()
		{
			var SUT = new MyContentDialog
			{
				Title = "Dialog title",
				Content = "Dialog content",
				PrimaryButtonText = "Accept",
				SecondaryButtonText = "Nope"
			};

			SetXamlRootForIslandsOrWinUI(SUT);

			bool triggered = false;
			var triggeredTwice = false;
			bool hideSecondTime = false;

			async void SUT_Closing(object sender, ContentDialogClosingEventArgs args)
			{
				triggeredTwice |= triggered;
				triggered = true;
				var deferral = args.GetDeferral();
				await WindowHelper.WaitFor(() => hideSecondTime);
				deferral.Complete();
				triggered = false;
			}

			SUT.Closing += SUT_Closing;

			try
			{
				await ShowDialog(SUT);

				SUT.Hide();
				await WindowHelper.WaitFor(() => triggered);
				SUT.Hide();
				hideSecondTime = true;
			}
			finally
			{
				SUT.Closing -= SUT_Closing;
				SUT.Hide();
			}

			await WindowHelper.WaitForIdle();

			// Closing should only be invoked once.
			// NOTE: the assert shouldn't be in SUT_Closing as it's async void.
			Assert.IsFalse(triggeredTwice);
		}

		[TestMethod]
		//#if !__ANDROID__ && !__APPLE_UIKIT__ // Fails on Android because keyboard does not appear when TextBox inside native popup is programmatically focussed - https://github.com/unoplatform/uno/issues/7995
#if !__APPLE_UIKIT__
		[Ignore("Test applies to platforms using software keyboard")]
#endif
		public async Task When_Soft_Keyboard_And_VisibleBounds()
		{
			var nativeUnsafeArea = ScreenHelper.GetUnsafeArea();

			using (ScreenHelper.OverrideVisibleBounds(new Thickness(0, 38, 0, 72), skipIfHasNativeUnsafeArea: (nativeUnsafeArea.Top + nativeUnsafeArea.Bottom) > 50))
			{
				var tb = new TextBox
				{
					Height = 1200
				};

				var dummyButton = new Button { Content = "Dummy" };


				var SUT = new MyContentDialog
				{
					Title = "Dialog title",
					Content = new ScrollViewer
					{
						Content = new StackPanel
						{
							Children =
							{
								dummyButton,
								tb
							}
						}
					},
					PrimaryButtonText = "Accept",
					SecondaryButtonText = "Nope"
				};

				SetXamlRootForIslandsOrWinUI(SUT);

				try
				{
					await ShowDialog(SUT);

					dummyButton.Focus(FocusState.Pointer); // Ensure keyboard is dismissed in case it is initially visible

					var inputPane = InputPane.GetForCurrentView();
					await WindowHelper.WaitFor(() => inputPane.OccludedRect.Height == 0);

					var originalButtonBounds = SUT.PrimaryButton.GetOnScreenBounds();
					var originalBackgroundBounds = SUT.BackgroundElement.GetOnScreenBounds();
					var visibleBounds = ApplicationView.GetForCurrentView().VisibleBounds;
					RectAssert.Contains(visibleBounds, originalButtonBounds);

					await FocusTextBoxWithSoftKeyboard(tb);

					var occludedRect = inputPane.OccludedRect;
					var shiftedButtonBounds = SUT.PrimaryButton.GetOnScreenBounds();
					var shiftedBackgroundBounds = SUT.BackgroundElement.GetOnScreenBounds();

					NumberAssert.Greater(originalButtonBounds.Bottom, occludedRect.Top); // Button's original position should be occluded, otherwise test is pointless
					NumberAssert.Greater(originalBackgroundBounds.Bottom, occludedRect.Top); // ditto background
					NumberAssert.Less(shiftedButtonBounds.Bottom, occludedRect.Top); // Button should be shifted to be visible (along with rest of dialog) while keyboard is open
					NumberAssert.Less(shiftedBackgroundBounds.Bottom, occludedRect.Top); // ditto background
					;
				}
				finally
				{
					SUT.Hide();
				}
			}
		}

		[TestMethod]
#if __SKIA__ || __WASM__
		[Ignore("Currently fails on Skia/WASM, tracked by #15981")]
#endif
		public async Task When_Has_VisibleBounds_LayoutRoot_Respects_VisibleBounds()
		{
			var nativeUnsafeArea = ScreenHelper.GetUnsafeArea();

			using (ScreenHelper.OverrideVisibleBounds(new Thickness(27, 38, 14, 72), skipIfHasNativeUnsafeArea: (nativeUnsafeArea.Top + nativeUnsafeArea.Bottom) > 50))
			{
				var SUT = new MyContentDialog
				{
					Title = "Dialog title",
					Content = "Hello",
					PrimaryButtonText = "Accept",
					SecondaryButtonText = "Nope"
				};

				SetXamlRootForIslandsOrWinUI(SUT);

				try
				{
					await ShowDialog(SUT);

					var layoutRootBounds = SUT.LayoutRoot.GetRelativeBounds((FrameworkElement)WindowHelper.EmbeddedTestRoot.control.XamlRoot.Content);
					var visibleBounds = ApplicationView.GetForCurrentView().VisibleBounds;
					RectAssert.Contains(visibleBounds, layoutRootBounds);
				}
				finally
				{
					SUT.Hide();
				}
			}
		}

		[TestMethod]
		[DataRow(ContentDialogButton.Primary)]
		[DataRow(ContentDialogButton.Secondary)]
		[DataRow(ContentDialogButton.Close)]
		public async Task When_Hide_In_Click(ContentDialogButton buttonType)
		{
			var contentDialog = new ContentDialog
			{
				Content = "Test",
				PrimaryButtonText = "Primary",
				SecondaryButtonText = "Secondary",
				CloseButtonText = "Close",
				XamlRoot = WindowHelper.XamlRoot,
			};

			static void OnButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs e)
			{
				e.Cancel = true;
				sender.Hide();
			}

			switch (buttonType)
			{
				case ContentDialogButton.Primary:
					contentDialog.PrimaryButtonClick += OnButtonClick;
					break;
				case ContentDialogButton.Secondary:
					contentDialog.SecondaryButtonClick += OnButtonClick;
					break;
				case ContentDialogButton.Close:
					contentDialog.CloseButtonClick += OnButtonClick;
					break;
			}

			int closingCount = 0;
			var closed = false;
			contentDialog.Closed += (s, e) => closed = true;
			contentDialog.Closing += (s, e) => closingCount++;

			var dialogTask = contentDialog.ShowAsync();

			Button button = null;

			var buttonName = buttonType switch
			{
				ContentDialogButton.Primary => "PrimaryButton",
				ContentDialogButton.Secondary => "SecondaryButton",
				ContentDialogButton.Close => "CloseButton",
				_ => throw new NotSupportedException()
			};

			await WindowHelper.WaitFor(() => (button = (Button)VisualTreeUtils.FindVisualChildByName(contentDialog, buttonName)) != null);

			await WindowHelper.WaitForLoaded(button);
			(FrameworkElementAutomationPeer.CreatePeerForElement(button) as ButtonAutomationPeer).Invoke();

			await WindowHelper.WaitFor(() => closed);

			await dialogTask;

			Assert.AreEqual(1, closingCount);
		}

		[TestMethod]
		[DataRow(ContentDialogButton.Primary)]
		[DataRow(ContentDialogButton.Secondary)]
		[DataRow(ContentDialogButton.Close)]
		public async Task When_Button_Click(ContentDialogButton buttonType)
		{
			var contentDialog = new ContentDialog
			{
				Content = "Test",
				PrimaryButtonText = "Primary",
				SecondaryButtonText = "Secondary",
				CloseButtonText = "Close",
				XamlRoot = WindowHelper.XamlRoot,
			};

			int closingCount = 0;
			var closed = false;
			contentDialog.Closed += (s, e) => closed = true;
			contentDialog.Closing += (s, e) => closingCount++;

			var dialogTask = contentDialog.ShowAsync();

			Button button = null;

			var buttonName = buttonType switch
			{
				ContentDialogButton.Primary => "PrimaryButton",
				ContentDialogButton.Secondary => "SecondaryButton",
				ContentDialogButton.Close => "CloseButton",
				_ => throw new NotSupportedException()
			};

			await WindowHelper.WaitFor(() => (button = (Button)VisualTreeUtils.FindVisualChildByName(contentDialog, buttonName)) != null);

			await WindowHelper.WaitForLoaded(button);
			(FrameworkElementAutomationPeer.CreatePeerForElement(button) as ButtonAutomationPeer).Invoke();

			await WindowHelper.WaitFor(() => closed);

			await dialogTask;

			Assert.AreEqual(1, closingCount);
		}

#if HAS_UNO
		[TestMethod]
		[CombinatorialData]
		[Ignore("Test is failing on all targets https://github.com/unoplatform/uno/issues/17984")]
		public async Task When_BackButton_Pressed(bool isCloseButtonEnabled)
		{
			var closeButtonClickEvent = new Event();
			var closedEvent = new Event();
			var openedEvent = new Event();
			var closeButtonClickRegistration = new SafeEventRegistration<ContentDialog, TypedEventHandler<ContentDialog, ContentDialogButtonClickEventArgs>>("CloseButtonClick");
			var closedRegistration = new SafeEventRegistration<ContentDialog, TypedEventHandler<ContentDialog, ContentDialogClosedEventArgs>>("Closed");
			var openedRegistration = new SafeEventRegistration<ContentDialog, TypedEventHandler<ContentDialog, ContentDialogOpenedEventArgs>>("Opened");

			var SUT = new MyContentDialog
			{
				Title = "Dialog title",
				Content = "Dialog content",
				CloseButtonText = "Close",
			};

			SetXamlRootForIslandsOrWinUI(SUT);

			if (!isCloseButtonEnabled)
			{
				var disabledStyle = new Style(typeof(Button));
				disabledStyle.Setters.Add(new Setter(Control.IsEnabledProperty, false));

				SUT.CloseButtonStyle = disabledStyle;
			}


			closeButtonClickRegistration.Attach(SUT, (s, e) => closeButtonClickEvent.Set());
			closedRegistration.Attach(SUT, (s, e) => closedEvent.Set());
			openedRegistration.Attach(SUT, (s, e) => openedEvent.Set());

			try
			{
				await ShowDialog(SUT);

				await openedEvent.WaitForDefault();
				VERIFY_IS_TRUE(SUT._popup.IsOpen);

				SystemNavigationManager.GetForCurrentView().RequestBack();

				await closeButtonClickEvent.WaitForDefault();
				await closedEvent.WaitForDefault();

				Assert.AreEqual(isCloseButtonEnabled, closeButtonClickEvent.HasFired());
				VERIFY_IS_TRUE(closedEvent.HasFired());
				VERIFY_IS_FALSE(SUT._popup.IsOpen);
			}
			finally
			{
				SUT.Hide();
			}
		}

		[TestMethod]
		public async Task When_Popup_Closed()
		{
			var closedEvent = new Event();
			var openedEvent = new Event();
			var closedRegistration = new SafeEventRegistration<ContentDialog, TypedEventHandler<ContentDialog, ContentDialogClosedEventArgs>>("Closed");
			var openedRegistration = new SafeEventRegistration<ContentDialog, TypedEventHandler<ContentDialog, ContentDialogOpenedEventArgs>>("Opened");

			var SUT = new MyContentDialog
			{
				Title = "Dialog title",
				Content = "Dialog content",
				CloseButtonText = "Close",
			};

			SetXamlRootForIslandsOrWinUI(SUT);

			closedRegistration.Attach(SUT, (s, e) => closedEvent.Set());
			openedRegistration.Attach(SUT, (s, e) => openedEvent.Set());

			try
			{
				var showAsyncResult = SUT.ShowAsync().AsTask();

				await openedEvent.WaitForDefault();

				SUT._popup.IsOpen = false;

				await closedEvent.WaitForDefault();

				VERIFY_IS_TRUE(closedEvent.HasFired());
				VERIFY_IS_FALSE(SUT._popup.IsOpen);

				if (await Task.WhenAny(showAsyncResult, Task.Delay(2000)) == showAsyncResult)
				{
					var dialogResult = showAsyncResult.Result;
					VERIFY_ARE_EQUAL(ContentDialogResult.None, dialogResult);
				}
				else
				{
					Assert.Fail("Timed out waiting for ShowAsync");
				}
			}
			finally
			{
				SUT.Hide();
			}
		}
#endif

#if HAS_UNO
		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/20842")]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaX11)]
		public async Task When_Window_Resized()
		{
			if (!Uno.UI.Xaml.Controls.NativeWindowFactory.SupportsMultipleWindows)
			{
				Assert.Inconclusive("This test requires spawning a new window.");
			}

			var window = new Window();
			try
			{
				bool activated = false;
				window.Content = new Border();
				window.Activated += (s, e) => activated = true;
				window.Activate();
				await TestServices.WindowHelper.WaitFor(() => activated);
				var dialog = new ContentDialog
				{
					Title = "Hello World",
					Content = "This is a sample dialog.",
					CloseButtonText = "Close",
					XamlRoot = window.Content.XamlRoot,
				};
				_ = dialog.ShowAsync();

				await UITestHelper.WaitForIdle();
				var popup = VisualTreeHelper.GetOpenPopupsForXamlRoot(window.Content.XamlRoot)[0];
				var panel = (ContentDialog)popup.Child;
				var size = panel.ActualSize;

				var appWindow = window.AppWindow;
				AppWindowChangedEventArgs args = null;
				void OnChanged(AppWindow s, AppWindowChangedEventArgs e)
				{
					args = e;
				}
				appWindow.Changed += OnChanged;
				var originalSize = appWindow.Size;

				var adjustedSize = new SizeInt32 { Width = originalSize.Width + 10, Height = originalSize.Height + 10 };
				appWindow.Resize(adjustedSize);
				await TestServices.WindowHelper.WaitFor(() => args is not null);
				Assert.IsTrue(args.DidSizeChange);
				await UITestHelper.WaitForIdle();
				Assert.AreNotEqual(size, panel.ActualSize);
			}
			finally
			{
				window.Close();
			}
		}
#endif

#if __ANDROID__
		// Fails because keyboard does not appear when TextBox is programmatically focussed, or appearance is not correctly registered - https://github.com/unoplatform/uno/issues/7995
		[Ignore()]
		[TestMethod]
		public async Task When_Soft_Keyboard_And_VisibleBounds_Native()
		{
			using (FeatureConfigurationHelper.UseNativePopups())
			{
				await When_Soft_Keyboard_And_VisibleBounds();
			}
		}

		// Fails because keyboard does not appear when TextBox is programmatically focussed, or appearance is not correctly registered - https://github.com/unoplatform/uno/issues/7995
		[Ignore()]
		[TestMethod]
		public async Task When_Soft_Keyboard_And_VisibleBounds_Managed()
		{
			await When_Soft_Keyboard_And_VisibleBounds();
		}
#endif

		private async Task FocusTextBoxWithSoftKeyboard(TextBox textBox)
		{
			var tcs = new TaskCompletionSource<bool>();

			var cts = new CancellationTokenSource(1000);
			cts.Token.Register(() => tcs.TrySetException(new TimeoutException()));

			var inputPane = InputPane.GetForCurrentView();
			void OnShowing(InputPane sender, InputPaneVisibilityEventArgs args)
			{
				tcs.TrySetResult(true);
			}
			try
			{
				inputPane.Showing += OnShowing;
				textBox.Focus(FocusState.Programmatic);

				if ((await Task.WhenAny(tcs.Task, Task.Delay(2000))) != tcs.Task)
				{
					// If focussing alone doesn't work, try explicitly invoking the keyboard
					inputPane.TryShow();
				}

				if ((await Task.WhenAny(tcs.Task, Task.Delay(8000))) != tcs.Task)
				{
					throw new InvalidOperationException("Failed to show soft keyboard");
				}
			}
			finally
			{
				inputPane.Showing -= OnShowing;
			}
			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForIdle();
		}

		private static async Task ShowDialog(MyContentDialog dialog)
		{
			_ = dialog.ShowAsync();
			await WindowHelper.WaitFor(() => dialog.BackgroundElement != null);
#if !WINAPPSDK
			await WindowHelper.WaitFor(() => dialog.BackgroundElement.ActualHeight > 0); // This is necessary on the current version of Uno because the template is materialized too early
#endif
		}

		private void SetXamlRootForIslandsOrWinUI(ContentDialog dialog)
		{
#if !HAS_UNO_WINUI
			if (WindowHelper.IsXamlIsland)
#endif
			{
				dialog.XamlRoot = WindowHelper.EmbeddedTestRoot.control.XamlRoot;
			}
		}
	}

	public partial class MyContentDialog : ContentDialog
	{
		private readonly bool unconstrained;
		public MyContentDialog(bool unconstrained = false)
		{
			this.unconstrained = unconstrained;
		}

		public Button PrimaryButton { get; private set; }
		public Border BackgroundElement { get; private set; }
		public Border Container { get; private set; }
		public Grid LayoutRoot { get; private set; }
		public ScrollViewer ContentScrollViewer { get; private set; }

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			Container = GetTemplateChild("Container") as Border;
			LayoutRoot = GetTemplateChild("LayoutRoot") as Grid;
			BackgroundElement = GetTemplateChild("BackgroundElement") as Border;

			ContentScrollViewer = GetTemplateChild("ContentScrollViewer") as ScrollViewer;
			PrimaryButton = GetTemplateChild("PrimaryButton") as Button;

			if (unconstrained && BackgroundElement is { })
			{
				BackgroundElement.MinWidth = BackgroundElement.MinHeight = 0;
				BackgroundElement.MaxWidth = BackgroundElement.MaxHeight = double.PositiveInfinity;
			}
		}
	}
}
