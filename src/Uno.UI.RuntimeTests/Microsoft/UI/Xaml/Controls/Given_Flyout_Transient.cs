#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.UI.DevTools.Input;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI.Input.Preview.Injection;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_Flyout_Transient
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public void When_Managed_Overlay_PassThrough_Uses_Weak_Storage()
	{
#if HAS_UNO
		Assert.IsTrue(FlyoutBase.OverlayInputPassThroughElementProperty.HasWeakStorage);
#endif
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[DataRow(FlyoutShowMode.Standard, false)]
	[DataRow(FlyoutShowMode.Auto, false)]
	[DataRow(FlyoutShowMode.Transient, true)]
	[DataRow(FlyoutShowMode.TransientWithDismissOnPointerMoveAway, true)]
	public async Task When_ShowMode_Selects_Default_Overlay_PassThrough(FlyoutShowMode mode, bool passThrough)
	{
		var target = new Button { Content = "Target", Margin = new Thickness(100) };
		var flyout = new Flyout { Content = new Border { Width = 50, Height = 30 } };
		try
		{
			await UITestHelper.Load(target);
			await ShowAndWaitForOpened(flyout, target, mode);

			Assert.AreEqual(mode == FlyoutShowMode.Auto ? FlyoutShowMode.Standard : mode, flyout.ShowMode);
			if (passThrough)
			{
				AssertAutomaticRoot(target, flyout);
			}
			else
			{
				Assert.IsNull(flyout.OverlayInputPassThroughElement);
			}
		}
		finally
		{
			try
			{
				await HideAndWaitForClosed(flyout);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[DataRow(FlyoutShowMode.Transient)]
	[DataRow(FlyoutShowMode.TransientWithDismissOnPointerMoveAway)]
	public async Task When_Open_ShowMode_Changes_Clear_Only_Automatic_PassThrough(FlyoutShowMode mode)
	{
		var target = new Button { Content = "Target", Margin = new Thickness(100) };
		var flyout = new Flyout { Content = new Border { Width = 50, Height = 30 } };
		try
		{
			await UITestHelper.Load(target);
			await ShowAndWaitForOpened(flyout, target, mode);
			AssertAutomaticRoot(target, flyout);

			flyout.ShowMode = FlyoutShowMode.Standard;
			Assert.IsNull(flyout.OverlayInputPassThroughElement);
			flyout.ShowMode = mode;
			AssertAutomaticRoot(target, flyout);

			flyout.OverlayInputPassThroughElement = target;
			flyout.ShowMode = FlyoutShowMode.Standard;
			Assert.AreSame(target, flyout.OverlayInputPassThroughElement, "An application override must not be cleared as an automatic root.");
		}
		finally
		{
			try
			{
				await HideAndWaitForClosed(flyout);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	public async Task When_Transient_Show_Preserves_Explicit_PassThrough()
	{
		var target = new Button { Content = "Target", Margin = new Thickness(100) };
		var flyout = new Flyout
		{
			Content = new Border { Width = 50, Height = 30 },
			OverlayInputPassThroughElement = target,
		};
		try
		{
			await UITestHelper.Load(target);
			await ShowAndWaitForOpened(flyout, target, FlyoutShowMode.Transient);
			Assert.AreSame(target, flyout.OverlayInputPassThroughElement);
			flyout.ShowMode = FlyoutShowMode.Standard;
			Assert.AreSame(target, flyout.OverlayInputPassThroughElement);
		}
		finally
		{
			try
			{
				await HideAndWaitForClosed(flyout);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	// This exercises Uno's input-rerouting adapter; the public show-mode policy tests also run on native WinUI.
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	[DataRow(FlyoutShowMode.Standard, false)]
	[DataRow(FlyoutShowMode.Standard, true)]
	[DataRow(FlyoutShowMode.Transient, false)]
	[DataRow(FlyoutShowMode.Transient, true)]
	public async Task When_Managed_Overlay_PassThrough_Only_Forwards_Within_Its_Subtree(FlyoutShowMode mode, bool withinSubtree)
	{
		var target = new Button { Content = "Target", Width = 100, Height = 50 };
		var other = new Button { Content = "Other", Width = 100, Height = 50 };
		var root = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Margin = new Thickness(100),
			Spacing = 120,
			Children = { other, target },
		};
		var flyout = new Flyout
		{
			Content = new Border { Width = 50, Height = 30 },
			OverlayInputPassThroughElement = withinSubtree ? other : target,
		};
		var opened = false;
		var closed = false;
		var clicks = 0;
		flyout.Opened += (_, _) => opened = true;
		flyout.Closed += (_, _) => closed = true;
		other.Click += (_, _) => clicks++;
		try
		{
			await UITestHelper.Load(root);
			flyout.ShowAt(target, new FlyoutShowOptions { ShowMode = mode });
			await WindowHelper.WaitFor(() => opened);

			var injector = InputInjector.TryCreate();
			Assert.IsNotNull(injector);
			using var finger = injector.GetFinger();
			var bounds = other.GetAbsoluteBounds();
			finger.Press(new Windows.Foundation.Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2));
			finger.Release();
			await WindowHelper.WaitFor(() => closed);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(withinSubtree ? 1 : 0, clicks);
		}
		finally
		{
			flyout.Hide();
			WindowHelper.WindowContent = null;
		}
	}

	private static void AssertAutomaticRoot(FrameworkElement target, FlyoutBase flyout)
	{
		var expected = target.XamlRoot!.Content;
		var actual = flyout.OverlayInputPassThroughElement;
		Assert.AreSame(expected, actual,
			$"Expected root {expected?.GetType().FullName}; actual {actual?.GetType().FullName ?? "<null>"}; " +
			$"value equality {Equals(expected, actual)}; stable root reference {ReferenceEquals(expected, target.XamlRoot.Content)}.");
	}

	private static async Task ShowAndWaitForOpened(FlyoutBase flyout, FrameworkElement target, FlyoutShowMode mode)
	{
		// WinUI can stage Open until a previous presenter unloads; dispatcher idle is not an Opened event.
		var opened = false;
		EventHandler<object> handler = (_, _) => opened = true;
		flyout.Opened += handler;
		try
		{
			flyout.ShowAt(target, new FlyoutShowOptions { ShowMode = mode });
			await WindowHelper.WaitFor(() => opened);
		}
		finally
		{
			flyout.Opened -= handler;
		}
	}

	private static async Task HideAndWaitForClosed(FlyoutBase flyout)
	{
		if (!flyout.IsOpen)
		{
			return;
		}

		var closed = false;
		EventHandler<object> handler = (_, _) => closed = true;
		flyout.Closed += handler;
		try
		{
			flyout.Hide();
			await WindowHelper.WaitFor(() => closed);
		}
		finally
		{
			flyout.Closed -= handler;
		}
	}
}
