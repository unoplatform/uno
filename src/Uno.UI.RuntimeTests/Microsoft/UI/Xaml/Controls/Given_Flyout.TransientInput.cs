using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.DevTools.Input;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using Windows.UI.Input.Preview.Injection;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	public partial class Given_Flyout
	{
		// These tests inject the mouse: on native WinUI that would take over the real pointer.

		[TestMethod]
		[RunsOnUIThread]
		[DataRow(FlyoutShowMode.Transient, true)]
		[DataRow(FlyoutShowMode.TransientWithDismissOnPointerMoveAway, true)]
		[DataRow(FlyoutShowMode.Standard, false)]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Flyout_Dismissed_By_Press_On_Button(FlyoutShowMode showMode, bool expectClick)
		{
			var scene = await TransientInputScene.Create();
			try
			{
				await scene.Show(showMode);

				scene.PressAndReleaseOnButtonNearFlyout();
				await TestServices.WindowHelper.WaitForIdle();

				Assert.IsFalse(scene.Flyout.IsOpen, "The press outside the flyout should close it.");
				Assert.AreEqual(expectClick ? 1 : 0, scene.Clicks, expectClick
					? "A transient flyout lets the press that dismisses it through to the element under it."
					: "A standard flyout keeps the press that dismisses it.");
			}
			finally
			{
				await scene.DisposeAsync();
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[DataRow(FlyoutShowMode.Standard)]
		[DataRow(FlyoutShowMode.Transient)]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Flyout_OverlayInputPassThroughElement_Set_By_App(FlyoutShowMode showMode)
		{
			var scene = await TransientInputScene.Create();
			try
			{
				// The app lets presses through to the first button's container only.
				scene.Flyout.OverlayInputPassThroughElement = scene.ButtonHost;

				await scene.Show(showMode);
				scene.PressAndReleaseOnButtonNearFlyout();
				await TestServices.WindowHelper.WaitForIdle();

				Assert.IsFalse(scene.Flyout.IsOpen);
				Assert.AreEqual(1, scene.Clicks, "The press on the app's pass-through element should go through.");
				Assert.AreSame(scene.ButtonHost, scene.Flyout.OverlayInputPassThroughElement, "Closing must keep the app's element.");

				await scene.Show(showMode);
				Assert.AreSame(scene.ButtonHost, scene.Flyout.OverlayInputPassThroughElement, "Reopening must keep the app's element.");

				scene.PressAndReleaseOnOtherButtonNearFlyout();
				await TestServices.WindowHelper.WaitForIdle();

				Assert.IsFalse(scene.Flyout.IsOpen);
				Assert.AreEqual(0, scene.OtherClicks, "A press outside the app's pass-through element should not go through, whatever the show mode.");
				Assert.AreSame(scene.ButtonHost, scene.Flyout.OverlayInputPassThroughElement);
			}
			finally
			{
				await scene.DisposeAsync();
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Flyout_Transient_Then_Standard_Press_Does_Not_Pass_Through()
		{
			var scene = await TransientInputScene.Create();
			try
			{
				await scene.Show(FlyoutShowMode.Transient);
				Assert.IsNotNull(scene.Flyout.OverlayInputPassThroughElement, "A transient flyout passes input through to the root.");

				scene.Flyout.Hide();
				await TestServices.WindowHelper.WaitForIdle();
				Assert.IsNull(scene.Flyout.OverlayInputPassThroughElement, "The flyout set the root itself, so it lets go of it when it closes.");

				await scene.Show(FlyoutShowMode.Standard);
				Assert.IsNull(scene.Flyout.OverlayInputPassThroughElement);

				scene.PressAndReleaseOnButtonNearFlyout();
				await TestServices.WindowHelper.WaitForIdle();

				Assert.IsFalse(scene.Flyout.IsOpen);
				Assert.AreEqual(0, scene.Clicks, "The transient show must not leak its pass-through into the next standard show.");
			}
			finally
			{
				await scene.DisposeAsync();
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Flyout_ShowMode_Becomes_Standard_While_Open()
		{
			var scene = await TransientInputScene.Create();
			try
			{
				await scene.Show(FlyoutShowMode.Transient);
				Assert.IsNotNull(scene.Flyout.OverlayInputPassThroughElement);

				// What CommandBarFlyout does when its command bar opens: the user is now interacting with the flyout.
				scene.Flyout.ShowMode = FlyoutShowMode.Standard;
				Assert.IsNull(scene.Flyout.OverlayInputPassThroughElement);

				scene.PressAndReleaseOnButtonNearFlyout();
				await TestServices.WindowHelper.WaitForIdle();

				Assert.IsFalse(scene.Flyout.IsOpen);
				Assert.AreEqual(0, scene.Clicks);
			}
			finally
			{
				await scene.DisposeAsync();
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Transient_Flyout_In_ContentDialog_Dismissed_By_Press()
		{
			var behind = new Button { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
			var behindClicks = 0;
			behind.Click += (_, _) => behindClicks++;
			TestServices.WindowHelper.WindowContent = behind;
			await TestServices.WindowHelper.WaitForLoaded(behind);

			var dialog = new ContentDialog { XamlRoot = TestServices.WindowHelper.XamlRoot };
			var scene = await TransientInputScene.Create(root =>
			{
				dialog.Content = root;
				_ = dialog.ShowAsync();
				return Task.FromResult<Action>(dialog.Hide);
			});
			try
			{
				await scene.Show(FlyoutShowMode.Transient);

				scene.PressAndReleaseOnButtonNearFlyout();
				await TestServices.WindowHelper.WaitForIdle();

				Assert.IsFalse(scene.Flyout.IsOpen, "The press outside the flyout should close it.");
				Assert.AreEqual(1, scene.Clicks, "The press should reach the dialog's content under the flyout.");
				Assert.AreEqual(0, behindClicks, "The press must not reach the window behind the dialog.");
				Assert.IsTrue(scene.Root.IsLoaded, "The dialog should stay open.");
			}
			finally
			{
				await scene.DisposeAsync();
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Transient_Flyout_In_Flyout_Dismissed_By_Press()
		{
			var behind = new Button { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
			var behindClicks = 0;
			behind.Click += (_, _) => behindClicks++;
			TestServices.WindowHelper.WindowContent = behind;
			await TestServices.WindowHelper.WaitForLoaded(behind);

			var outer = new Flyout();
			var scene = await TransientInputScene.Create(root =>
			{
				outer.Content = root;
				outer.ShowAt(behind);
				return Task.FromResult<Action>(outer.Hide);
			});
			try
			{
				await scene.Show(FlyoutShowMode.Transient);

				scene.PressAndReleaseOnButtonNearFlyout();
				await TestServices.WindowHelper.WaitForIdle();

				Assert.IsFalse(scene.Flyout.IsOpen, "The press outside the inner flyout should close it.");
				Assert.IsTrue(outer.IsOpen, "The press lands in the outer flyout, which should stay open.");
				Assert.AreEqual(1, scene.Clicks, "The press should reach the outer flyout's content under the inner one.");
				Assert.AreEqual(0, behindClicks, "The press must not reach the window behind the outer flyout.");
			}
			finally
			{
				await scene.DisposeAsync();
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[DataRow(FlyoutShowMode.TransientWithDismissOnPointerMoveAway, true)]
		[DataRow(FlyoutShowMode.Transient, false)]
		[DataRow(FlyoutShowMode.Standard, false)]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Pointer_Moves_Away_From_Flyout(FlyoutShowMode showMode, bool expectHidden)
		{
			var scene = await TransientInputScene.Create();
			try
			{
				await scene.Show(showMode);

				// WinUI hides the flyout once the pointer is more than 80 px from the presenter's bounds.
				scene.MoveBelowFlyout(0);
				await TestServices.WindowHelper.WaitForIdle();
				Assert.IsTrue(scene.Flyout.IsOpen, "The pointer is over the flyout.");

				scene.MoveBelowFlyout(60);
				await TestServices.WindowHelper.WaitForIdle();
				Assert.IsTrue(scene.Flyout.IsOpen, "The pointer is 60 px away, within the threshold.");

				scene.MoveBelowFlyout(100);
				await TestServices.WindowHelper.WaitForIdle();
				Assert.AreEqual(!expectHidden, scene.Flyout.IsOpen, expectHidden
					? "The pointer is 100 px away: the flyout should hide."
					: "Only TransientWithDismissOnPointerMoveAway hides when the pointer moves away.");
			}
			finally
			{
				await scene.DisposeAsync();
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[DataRow(false)]
		[DataRow(true)]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Pointer_Moves_Before_Flyout_Laid_Out(bool reopened)
		{
			var scene = await TransientInputScene.Create();
			try
			{
				if (reopened)
				{
					await scene.Show(FlyoutShowMode.TransientWithDismissOnPointerMoveAway);
					scene.Flyout.Hide();
					await TestServices.WindowHelper.WaitForIdle();
				}

				// Open next to the other target, then move there before any layout pass: the presenter still has the
				// previous opening's bounds (or none), far from the pointer.
				scene.Flyout.ShowAt(scene.FarTarget, new FlyoutShowOptions { ShowMode = FlyoutShowMode.TransientWithDismissOnPointerMoveAway });
				var farBounds = scene.FarTarget.GetAbsoluteBounds();
				scene.Mouse.MoveTo(new Point(farBounds.X + farBounds.Width / 2, farBounds.Bottom + 10), steps: 1);

				await TestServices.WindowHelper.WaitForLoaded(scene.FlyoutContent);
				await TestServices.WindowHelper.WaitForIdle();
				Assert.IsTrue(scene.Flyout.IsOpen, "A move next to where the flyout opens must not hide it.");
			}
			finally
			{
				await scene.DisposeAsync();
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Pointer_Captured_In_Flyout_Moves_Away()
		{
			var scene = await TransientInputScene.Create();
			try
			{
				await scene.Show(FlyoutShowMode.TransientWithDismissOnPointerMoveAway);

				// Press a button in the flyout and drag far away: the moves are the button's, as on WinUI.
				var inner = scene.InnerButton.GetAbsoluteBounds();
				scene.Mouse.MoveTo(new Point(inner.X + inner.Width / 2, inner.Y + inner.Height / 2), steps: 1);
				scene.Mouse.Press();
				scene.Mouse.MoveTo(new Point(inner.X + inner.Width / 2, inner.Bottom + 150), steps: 5);
				await TestServices.WindowHelper.WaitForIdle();
				Assert.IsTrue(scene.Flyout.IsOpen, "Moves of a pointer captured inside the flyout must not hide it.");

				scene.Mouse.Release();
				scene.Mouse.MoveBy(0, 5);
				await TestServices.WindowHelper.WaitForIdle();
				Assert.IsFalse(scene.Flyout.IsOpen, "Once released, a move that far away hides it.");
			}
			finally
			{
				await scene.DisposeAsync();
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Transient_Flyout_Right_Click_Opens_Context_Menu_Under()
		{
			var scene = await TransientInputScene.Create();
			var contextMenu = new MenuFlyout { Items = { new MenuFlyoutItem { Text = "Context" } } };
			scene.Button.ContextFlyout = contextMenu;
			try
			{
				await scene.Show(FlyoutShowMode.Transient);

				// WinUI lets right-clicks through the light-dismiss layer (PointerInputProcessor
				// ShouldEventAlwaysPassThroughPopupLightDismissLayer); a transient flyout lets any press through.
				var flyoutBounds = scene.FlyoutContent.GetAbsoluteBounds();
				scene.Mouse.MoveTo(new Point(flyoutBounds.Left + 10, flyoutBounds.Bottom + 20), steps: 1);
				scene.Mouse.PressRight();
				// As a real click does, release after the closed flyout's deferred Closed callback, which would otherwise
				// hide the context menu opened meanwhile (https://github.com/unoplatform/uno/issues/26654).
				await TestServices.WindowHelper.WaitForIdle();
				scene.Mouse.ReleaseRight();
				await TestServices.WindowHelper.WaitForIdle();

				Assert.IsFalse(scene.Flyout.IsOpen, "The right-click outside the flyout should close it.");
				Assert.IsTrue(contextMenu.IsOpen, "The right-click should open the context menu of the button under it.");
			}
			finally
			{
				contextMenu.Hide();
				await scene.DisposeAsync();
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Transient_Flyout_In_Standard_Flyout_Press_Outside_Both()
		{
			var behind = new Button { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
			var behindClicks = 0;
			behind.Click += (_, _) => behindClicks++;
			TestServices.WindowHelper.WindowContent = behind;
			await TestServices.WindowHelper.WaitForLoaded(behind);

			var outer = new Flyout();
			var scene = await TransientInputScene.Create(root =>
			{
				outer.Content = root;
				outer.ShowAt(behind);
				return Task.FromResult<Action>(outer.Hide);
			});
			try
			{
				await scene.Show(FlyoutShowMode.Transient);

				var outerBounds = scene.Root.GetAbsoluteBounds();
				var behindBounds = behind.GetAbsoluteBounds();
				var point = new Point(behindBounds.Right - 10, behindBounds.Bottom - 10);
				Assert.IsFalse(outerBounds.Contains(point), "The press must land outside both flyouts.");
				scene.Mouse.MoveTo(point, steps: 1);
				scene.Mouse.Press();
				scene.Mouse.Release();
				await TestServices.WindowHelper.WaitForIdle();

				Assert.IsFalse(scene.Flyout.IsOpen, "The press outside should close the inner flyout.");
				Assert.IsFalse(outer.IsOpen, "The press outside should close the outer flyout.");
				// Uno-specific: the outer flyout's light-dismiss layer takes the press, as a Standard flyout eats the
				// press that dismisses it. WinUI lets it reach the page.
				Assert.AreEqual(0, behindClicks);
			}
			finally
			{
				await scene.DisposeAsync();
			}
		}

		private sealed class TransientInputScene
		{
			private readonly Mouse _mouse;
			private Action _cleanup;

			private TransientInputScene(Mouse mouse)
			{
				_mouse = mouse;
			}

			public Grid Root { get; private set; }
			public Grid ButtonHost { get; private set; }
			public Button Button { get; private set; }
			public Button OtherButton { get; private set; }
			public Border Target { get; private set; }
			public Border FarTarget { get; private set; }
			public Button InnerButton { get; private set; }
			public Mouse Mouse => _mouse;
			public Border FlyoutContent { get; private set; }
			public Flyout Flyout { get; private set; }
			public int Clicks { get; private set; }
			public int OtherClicks { get; private set; }

			public static async Task<TransientInputScene> Create(Func<Grid, Task<Action>> host = null)
			{
				var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
				var scene = new TransientInputScene(injector.GetMouse());

				// Two buttons side by side, each filling its half, and a small target between them at the top: the flyout
				// opens below the target, over both buttons.
				scene.Button = new Button { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
				scene.OtherButton = new Button { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
				scene.Button.Click += (_, _) => scene.Clicks++;
				scene.OtherButton.Click += (_, _) => scene.OtherClicks++;
				scene.ButtonHost = new Grid { Children = { scene.Button } };
				Grid.SetColumn(scene.OtherButton, 1);
				scene.Target = new Border
				{
					Width = 20,
					Height = 20,
					Background = new SolidColorBrush(Microsoft.UI.Colors.Red),
					HorizontalAlignment = HorizontalAlignment.Center,
					VerticalAlignment = VerticalAlignment.Top,
				};
				Grid.SetColumnSpan(scene.Target, 2);
				scene.FarTarget = new Border
				{
					Width = 20,
					Height = 20,
					Background = new SolidColorBrush(Microsoft.UI.Colors.Blue),
					HorizontalAlignment = HorizontalAlignment.Left,
					VerticalAlignment = VerticalAlignment.Bottom,
				};
				scene.Root = new Grid
				{
					Width = 300,
					Height = 200,
					ColumnDefinitions = { new ColumnDefinition(), new ColumnDefinition() },
					Children = { scene.ButtonHost, scene.OtherButton, scene.Target, scene.FarTarget },
				};
				scene.InnerButton = new Button { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
				scene.FlyoutContent = new Border { Width = 120, Height = 40, Child = scene.InnerButton };
				scene.Flyout = new Flyout { Content = scene.FlyoutContent, Placement = FlyoutPlacementMode.Bottom };

				if (host is null)
				{
					TestServices.WindowHelper.WindowContent = scene.Root;
				}
				else
				{
					scene._cleanup = await host(scene.Root);
				}

				await TestServices.WindowHelper.WaitForLoaded(scene.Root);
				await TestServices.WindowHelper.WaitForIdle();

				return scene;
			}

			public async Task Show(FlyoutShowMode showMode)
			{
				Flyout.ShowAt(Target, new FlyoutShowOptions { ShowMode = showMode });
				await TestServices.WindowHelper.WaitForLoaded(FlyoutContent);
				await TestServices.WindowHelper.WaitForIdle();
				Assert.IsTrue(Flyout.IsOpen);
			}

			// 20 px below the flyout, so a flyout that hides when the pointer moves away stays open until the press.
			public void PressAndReleaseOnButtonNearFlyout()
				=> PressAndRelease(FlyoutContent.GetAbsoluteBounds().Left + 10);

			public void PressAndReleaseOnOtherButtonNearFlyout()
				=> PressAndRelease(FlyoutContent.GetAbsoluteBounds().Right - 10);

			// Below the presenter (the flyout's content plus its padding and border), centered; 0 is just inside it.
			public void MoveBelowFlyout(double distance)
			{
				DependencyObject presenter = FlyoutContent;
				while (presenter is not FlyoutPresenter)
				{
					presenter = VisualTreeHelper.GetParent(presenter);
				}

				var presenterBounds = ((FlyoutPresenter)presenter).GetAbsoluteBounds();
				var y = distance == 0 ? presenterBounds.Bottom - 1 : presenterBounds.Bottom + distance;
				_mouse.MoveTo(new Point(presenterBounds.Left + presenterBounds.Width / 2, y), steps: 1);
			}

			private void PressAndRelease(double x)
			{
				var flyoutBounds = FlyoutContent.GetAbsoluteBounds();
				var point = new Point(x, flyoutBounds.Bottom + 20);
				_mouse.MoveTo(point, steps: 1);
				_mouse.Press();
				_mouse.Release();
			}

			public async Task DisposeAsync()
			{
				_mouse.Dispose();
				Flyout.Hide();
				_cleanup?.Invoke();
				TestServices.WindowHelper.WindowContent = null;
				await TestServices.WindowHelper.WaitForIdle();
			}
		}
	}
}
