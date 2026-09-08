#nullable enable

using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.Extensions;
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
			flyout.ShowAt(target, new FlyoutShowOptions { ShowMode = mode });
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(mode == FlyoutShowMode.Auto ? FlyoutShowMode.Standard : mode, flyout.ShowMode);
			if (passThrough)
			{
				Assert.AreSame(target.XamlRoot!.Content, flyout.OverlayInputPassThroughElement);
			}
			else
			{
				Assert.IsNull(flyout.OverlayInputPassThroughElement);
			}
		}
		finally
		{
			flyout.Hide();
			WindowHelper.WindowContent = null;
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
			flyout.ShowAt(target, new FlyoutShowOptions { ShowMode = mode });
			await WindowHelper.WaitForIdle();
			Assert.AreSame(target.XamlRoot!.Content, flyout.OverlayInputPassThroughElement);

			flyout.ShowMode = FlyoutShowMode.Standard;
			Assert.IsNull(flyout.OverlayInputPassThroughElement);
			flyout.ShowMode = mode;
			Assert.AreSame(target.XamlRoot!.Content, flyout.OverlayInputPassThroughElement);

			flyout.OverlayInputPassThroughElement = target;
			flyout.ShowMode = FlyoutShowMode.Standard;
			Assert.AreSame(target, flyout.OverlayInputPassThroughElement, "An application override must not be cleared as an automatic root.");
		}
		finally
		{
			flyout.Hide();
			WindowHelper.WindowContent = null;
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
			flyout.ShowAt(target, new FlyoutShowOptions { ShowMode = FlyoutShowMode.Transient });
			await WindowHelper.WaitForIdle();
			Assert.AreSame(target, flyout.OverlayInputPassThroughElement);
			flyout.ShowMode = FlyoutShowMode.Standard;
			Assert.AreSame(target, flyout.OverlayInputPassThroughElement);
		}
		finally
		{
			flyout.Hide();
			WindowHelper.WindowContent = null;
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
			finger.Press(other.GetAbsoluteBounds().GetCenter());
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
}
