#if HAS_INPUT_INJECTOR && !WINAPPSDK
using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Input.Preview.Injection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Toolkit.DevTools.Input;

namespace Uno.UI.RuntimeTests.Tests.Uno_UI_Xaml_Core;

/// <summary>
/// Tests for <see cref="InputInjector.TryCreate(object)"/> with a relative root,
/// which scopes hit-testing to a subtree so pointer injection can bypass
/// design-time overlays (spec 045).
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_InputInjector_RelativeRoot
{
	[TestMethod]
#if !HAS_INPUT_INJECTOR
	[Ignore("InputInjector is not supported on this platform.")]
#endif
	public async Task When_TryCreateWithRoot_Then_InjectorCreated()
	{
		var root = new Border { Width = 100, Height = 100, Background = new SolidColorBrush(Colors.Red) };
		await UITestHelper.Load(root);

		var injector = InputInjector.TryCreate(root);

		Assert.IsNotNull(injector, "TryCreate(object) should return an injector when an IInputInjectorTarget is registered.");
	}

	[TestMethod]
#if !HAS_INPUT_INJECTOR
	[Ignore("InputInjector is not supported on this platform.")]
#endif
	public async Task When_TryCreateWithNullRoot_Then_BehavesLikeDefault()
	{
		var root = new Border { Width = 100, Height = 100, Background = new SolidColorBrush(Colors.Red) };
		await UITestHelper.Load(root);

		// null relative root should behave identically to the no-arg overload.
		var withNull = InputInjector.TryCreate((object)null!);
		var withoutRoot = InputInjector.TryCreate();

		Assert.IsNotNull(withNull);
		Assert.IsNotNull(withoutRoot);
	}

	[TestMethod]
#if !HAS_INPUT_INJECTOR
	[Ignore("InputInjector is not supported on this platform.")]
#endif
	public async Task When_ClickWithRelativeRoot_Then_HitTestScopedToSubtree()
	{
		// Simplest case: a single Border as RelativeRoot, click its center.
		// Validates the full scoped injection pipeline end-to-end.
		Border target;
		var ui = new Grid
		{
			Width = 200,
			Height = 200,
			Children =
			{
				(target = new Border
				{
					Name = "target",
					Background = new SolidColorBrush(Colors.Blue),
					HorizontalAlignment = HorizontalAlignment.Stretch,
					VerticalAlignment = VerticalAlignment.Stretch,
				}),
			}
		};

		await UITestHelper.Load(ui);

		bool targetPressed = false;
		target.PointerPressed += (_, _) => targetPressed = true;

		// Coordinates are relative to the RelativeRoot (`target`).
		var scopedInjector = InputInjector.TryCreate(target)
			?? throw new InvalidOperationException("Failed to create scoped InputInjector");
		var mouse = scopedInjector.GetMouse();
		mouse.Press(new Point(100, 100));
		mouse.Release();

		Assert.IsTrue(targetPressed, "Target should receive the press via scoped injection.");
	}

	[TestMethod]
#if !HAS_INPUT_INJECTOR
	[Ignore("InputInjector is not supported on this platform.")]
#endif
	public async Task When_ClickWithRelativeRoot_Then_NestedChildrenStillHit()
	{
		// Verifies that children INSIDE the relative root subtree are still
		// reachable — the scope restricts the search, not the depth.

		Button innerButton;
		Border root;
		var ui = new Grid
		{
			Width = 200,
			Height = 200,
			Children =
			{
				new Border
				{
					Name = "overlay",
					Background = new SolidColorBrush(Colors.Red),
					HorizontalAlignment = HorizontalAlignment.Stretch,
					VerticalAlignment = VerticalAlignment.Stretch,
				},
				(root = new Border
				{
					Name = "root",
					Background = new SolidColorBrush(Colors.Transparent),
					HorizontalAlignment = HorizontalAlignment.Stretch,
					VerticalAlignment = VerticalAlignment.Stretch,
					Child = (innerButton = new Button
					{
						Content = "Click me",
						HorizontalAlignment = HorizontalAlignment.Center,
						VerticalAlignment = VerticalAlignment.Center,
					})
				}),
			}
		};

		// Force `root` behind overlay by adding overlay last.
		ui.Children.Clear();
		ui.Children.Add(root);     // bottom
		ui.Children.Add(new Border // top overlay
		{
			Name = "overlay",
			Background = new SolidColorBrush(Colors.Red),
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch,
		});

		await UITestHelper.Load(ui);

		bool buttonClicked = false;
		innerButton.Click += (_, _) => buttonClicked = true;

		// Coordinates are relative to `root` (the RelativeRoot).
		// The button is centered in the 200x200 root, so (100, 100) in root-local
		// space should land on it.
		var buttonCenter = new Point(100, 100);

		var scopedInjector = InputInjector.TryCreate(root)
			?? throw new InvalidOperationException("Failed to create scoped InputInjector");
		var mouse = scopedInjector.GetMouse();
		mouse.Press(buttonCenter);
		mouse.Release();
		await UITestHelper.WaitForIdle();

		Assert.IsTrue(buttonClicked, "Button inside the relative root subtree should be reachable by scoped injection.");
	}
}
#endif
