#if HAS_INPUT_INJECTOR && !WINAPPSDK
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.RuntimeTests.Extensions;
using Windows.ApplicationModel.Appointments;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input.Preview.Injection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Input;
using Private.Infrastructure;
using Uno.Extensions;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.Helpers;

#if HAS_UNO_WINUI
using GestureRecognizer = Microsoft.UI.Input.GestureRecognizer;
#else
using GestureRecognizer = Windows.UI.Input.GestureRecognizer;
#endif

namespace Uno.UI.RuntimeTests.Tests.Uno_UI_Xaml_Core;

[TestClass]
[RunsOnUIThread]
public class Given_InputManager
{
	[TestMethod]
#if !HAS_INPUT_INJECTOR
	[Ignore("InputInjector is not supported on this platform.")]
#endif
	public async Task When_VisibilityChangesWhileDispatching_Then_RecomputeOriginalSource()
	{
		Border col1, col2;
		var ui = new Grid
		{
			Width = 200,
			Height = 200,
			ColumnDefinitions =
			{
				new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
				new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
			},
			Children =
			{
				(col1 = new Border { Background = new SolidColorBrush(Colors.DeepPink) }),
				(col2 = new Border { Background = new SolidColorBrush(Colors.DeepSkyBlue) }),
			}
		};

		Grid.SetColumn(col1, 0);
		Grid.SetColumn(col2, 1);

		var position = await UITestHelper.Load(ui);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var finger = injector.GetFinger();

		var failed = false;
		col1.PointerExited += (snd, args) => col2.Visibility = Visibility.Collapsed;
		col2.PointerEntered += (snd, args) => failed = true;

		injector.GetFinger().Drag(position.Location.OffsetLinear(10), position.Location.Offset(180, 10));

		Assert.AreEqual(Visibility.Collapsed, col2.Visibility, "The visibility should have been changed when the pointer left the col1.");
		Assert.IsFalse(failed, "The pointer should not have been dispatched to the col2 as it has been set to visibility collapsed.");
	}

	[TestMethod]
#if !HAS_INPUT_INJECTOR
	[Ignore("InputInjector is not supported on this platform.")]
#endif
	public async Task When_LeaveElementWhileManipulating_Then_CaptureNotLost()
	{
		Border sut;
		TranslateTransform transform;
		var ui = new Grid
		{
			Width = 128,
			Height = 512,
			Children =
			{
				(sut = new Border
				{
					Name = "SUT-Border",
					HorizontalAlignment = HorizontalAlignment.Center,
					VerticalAlignment = VerticalAlignment.Center,
					Width = 16,
					Height = GestureRecognizer.Manipulation.StartTouch.TranslateY * 3,
					Background = new SolidColorBrush(Colors.DeepPink),
					ManipulationMode = ManipulationModes.TranslateY,
					RenderTransform = (transform = new TranslateTransform())
				}),
			}
		};

		await UITestHelper.Load(ui);

		var exited = false;
		sut.ManipulationDelta += (snd, e) => transform.Y = e.Cumulative.Translation.Y;
		sut.PointerExited += (snd, e) => exited = true;

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var finger = injector.GetFinger();

		finger.Press(sut.GetAbsoluteBounds().GetCenter());

		// Start manipulation
		finger.MoveBy(0, 50, steps: 50);
		transform.Y.Should().NotBe(0, "Manipulation should have started");

		// Cause a fast move that will trigger a pointer leave
		exited.Should().BeFalse();
		finger.MoveBy(0, 50, steps: 0);
		exited.Should().BeTrue();

		// Confirm that even if we got a leave, pointer is still captured and we are still receiving manipulation events
		var intermediatePosition = transform.Y;
		finger.MoveBy(0, 50);
		transform.Y.Should().Be(intermediatePosition + 50);
	}

	[TestMethod]
#if !HAS_INPUT_INJECTOR
	[Ignore("InputInjector is not supported on this platform.")]
#endif
	public async Task When_LeaveElementWithCapture_Then_PointerEnterLeaveOnlyOnCapturedElement_And_EnterRaisedOnCaptureLost()
	{
		Border sut, sutParent, sutChild, sutSibling;
		var ui = new Grid
		{
			Width = 128,
			Height = 128,
			Children =
			{
				(sutParent = new Border
				{
					HorizontalAlignment = HorizontalAlignment.Left,
					VerticalAlignment = VerticalAlignment.Center,
					Width = 36,
					Height = 36,
					Background = new SolidColorBrush(Colors.DeepSkyBlue),
					Child = (sut = new Border
					{
						Name = "SUT-Border",
						HorizontalAlignment = HorizontalAlignment.Center,
						VerticalAlignment = VerticalAlignment.Center,
						Width = 32,
						Height = 32,
						Background = new SolidColorBrush(Colors.DeepPink),
						Child = (sutChild = new Border
						{
							Name = "SUT-Border",
							HorizontalAlignment = HorizontalAlignment.Center,
							VerticalAlignment = VerticalAlignment.Center,
							Width = 28,
							Height = 28,
							Background = new SolidColorBrush(Colors.Chartreuse),
						})
					})
				}),
				(sutSibling = new Border
				{
					HorizontalAlignment = HorizontalAlignment.Right,
					VerticalAlignment = VerticalAlignment.Center,
					Width = 36,
					Height = 36,
					Background = new SolidColorBrush(Colors.Orange)
				})
			}
		};

		await UITestHelper.Load(ui);

		sut.PointerPressed += (snd, e) => sut.CapturePointer(e.Pointer);

		int parentEntered = 0, sutEntered = 0, childEntered = 0, siblingEntered = 0;
		sutParent.PointerEntered += (snd, e) => parentEntered++;
		sut.PointerEntered += (snd, e) => sutEntered++;
		sutChild.PointerEntered += (snd, e) => childEntered++;
		sutSibling.PointerEntered += (snd, e) => siblingEntered++;

		int parentExited = 0, sutExited = 0, childExited = 0, siblingExited = 0;
		sutParent.PointerExited += (snd, e) => parentExited++;
		sut.PointerExited += (snd, e) => sutExited++;
		sutChild.PointerExited += (snd, e) => childExited++;
		sutSibling.PointerExited += (snd, e) => siblingExited++;

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var finger = injector.GetMouse();

		finger.Press(sut.GetAbsoluteBounds().GetCenter());
		finger.MoveTo(sutSibling.GetAbsoluteBounds().GetCenter());

		parentEntered.Should().Be(1);
		sutEntered.Should().Be(1);
		childEntered.Should().Be(1);
		siblingEntered.Should().Be(0);
		parentExited.Should().Be(0);
		sutExited.Should().Be(1);
		childExited.Should().Be(0);
		siblingExited.Should().Be(0);

		finger.MoveTo(sut.GetAbsoluteBounds().GetCenter());
		finger.MoveTo(sutSibling.GetAbsoluteBounds().GetCenter());

		parentEntered.Should().Be(1);
		sutEntered.Should().Be(2);
		childEntered.Should().Be(1);
		siblingEntered.Should().Be(0);
		parentExited.Should().Be(0);
		sutExited.Should().Be(2);
		childExited.Should().Be(0);
		siblingExited.Should().Be(0);

		finger.Release();

		parentEntered.Should().Be(1);
		sutEntered.Should().Be(2);
		childEntered.Should().Be(1);
		siblingEntered.Should().Be(1, because: "enter event should be raised on element under the mouse when the capture is released");
		parentExited.Should().Be(0);
		sutExited.Should().Be(2);
		childExited.Should().Be(0);
		siblingExited.Should().Be(0);
	}

	[TestMethod]
#if !HAS_INPUT_INJECTOR
	[Ignore("InputInjector is not supported on this platform.")]
#endif
	public async Task When_Hover_No_Delay_For_VisualState_Update()
	{
		var comboxBoxItem = new ComboBoxItem()
		{
			Content = "ComboBoxItem Content",
		};

		var position = await UITestHelper.Load(comboxBoxItem);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		var mouse = injector.GetMouse();

		var oldState = VisualStateManager.GetCurrentState(comboxBoxItem, "CommonStates").Name;
		mouse.MoveTo(position.GetCenter().X, position.GetCenter().Y);
		var newState = VisualStateManager.GetCurrentState(comboxBoxItem, "CommonStates").Name;
		Assert.AreEqual("Normal", oldState);
		Assert.AreEqual("PointerOver", newState);
	}

	[TestMethod]
#if __WASM__
	[Ignore("Scrolling is handled by native code and InputInjector is not yet able to inject native pointers.")]
#elif !HAS_INPUT_INJECTOR
	[Ignore("InputInjector is not supported on this platform.")]
#elif __SKIA__
	[Ignore("Disabled due to https://github.com/unoplatform/uno-private/issues/878")]
#endif
	public async Task When_Scroll_No_Delay_For_VisualState_Update()
	{
		var stackPanel = new StackPanel();
		for (var i = 1; i <= 10; i++)
		{
			var button = new Button { Content = "Button " + i };
			stackPanel.Children.Add(button);
		}

		var scrollViewer = new ScrollViewer()
		{
			VerticalScrollMode = ScrollMode.Enabled,
			Content = stackPanel,
			MaxHeight = 50,
		};

		var button1 = stackPanel.Children[0] as Button;
		var button2 = stackPanel.Children[1] as Button;

		var border = new Border { scrollViewer };

		await UITestHelper.Load(border);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		var mouse = injector.GetMouse();

		Assert.AreEqual("Normal", VisualStateManager.GetCurrentState(button1, "CommonStates").Name);

		var position = button1.GetAbsoluteBounds();
		mouse.MoveTo(position.GetCenter().X, position.GetCenter().Y);
		Assert.AreEqual("PointerOver", VisualStateManager.GetCurrentState(button1, "CommonStates").Name);

		mouse.Wheel(-50);

		// Wait for the scroll animation to complete
		await Task.Delay(1000);

		await TestServices.WindowHelper.WaitForIdle();
		Assert.AreEqual("Normal", VisualStateManager.GetCurrentState(button1, "CommonStates").Name);
		Assert.AreEqual("PointerOver", VisualStateManager.GetCurrentState(button2, "CommonStates").Name);
	}

	[TestMethod]
	[Ignore("https://github.com/unoplatform/uno/issues/15509")]
	public async Task When_ProtectedCursor_Basic()
	{
		var stackPanel = new StackPanel();
		for (var i = 1; i <= 10; i++)
		{
			var button = new Button { Content = "Button " + i };
			stackPanel.Children.Add(button);
		}

		var scrollViewer = new ScrollViewer()
		{
			VerticalScrollMode = ScrollMode.Enabled,
			Content = stackPanel,
			MaxHeight = 50,
		};

		var button1 = (Button)stackPanel.Children[0];
		var button2 = (Button)stackPanel.Children[1];

		button1.SetProtectedCursor(InputSystemCursor.Create(InputSystemCursorShape.IBeam));

		var border = new Border { scrollViewer };

		await UITestHelper.Load(border);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		var mouse = injector.GetMouse();

		mouse.MoveTo(button1.GetAbsoluteBounds().GetCenter());
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(CoreCursorType.IBeam, GetCursorShape());

		mouse.MoveTo(button2.GetAbsoluteBounds().GetCenter());
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(CoreCursorType.Arrow, GetCursorShape());
	}

	[TestMethod]
	[Ignore("https://github.com/unoplatform/uno/issues/15509")]
	public async Task When_ProtectedCursor_PointerCaptured()
	{
		var stackPanel = new StackPanel();
		for (var i = 1; i <= 10; i++)
		{
			var button = new Button { Content = "Button " + i };
			stackPanel.Children.Add(button);
		}

		var scrollViewer = new ScrollViewer()
		{
			VerticalScrollMode = ScrollMode.Enabled,
			Content = stackPanel,
			MaxHeight = 50,
		};

		var button1 = (Button)stackPanel.Children[0];
		var button2 = (Button)stackPanel.Children[1];

		button1.SetProtectedCursor(InputSystemCursor.Create(InputSystemCursorShape.IBeam));

		var border = new Border { scrollViewer };

		await UITestHelper.Load(border);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		var mouse = injector.GetMouse();

		mouse.MoveTo(button1.GetAbsoluteBounds().GetCenter());
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(CoreCursorType.IBeam, GetCursorShape());

		mouse.Press();

		mouse.MoveTo(button2.GetAbsoluteBounds().GetCenter());
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(CoreCursorType.IBeam, GetCursorShape());

		mouse.Release();
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(CoreCursorType.Arrow, GetCursorShape());
	}

	[TestMethod]
	[Ignore("https://github.com/unoplatform/uno/issues/15509")]
	public async Task When_ProtectedCursor_Scrolled()
	{
		var stackPanel = new StackPanel();
		for (var i = 1; i <= 10; i++)
		{
			var button = new Button { Content = "Button " + i };
			stackPanel.Children.Add(button);
		}

		var scrollViewer = new ScrollViewer()
		{
			VerticalScrollMode = ScrollMode.Enabled,
			Content = stackPanel,
			MaxHeight = 50,
		};

		var button1 = (Button)stackPanel.Children[0];

		button1.SetProtectedCursor(InputSystemCursor.Create(InputSystemCursorShape.IBeam));

		var border = new Border { scrollViewer };

		await UITestHelper.Load(border);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		var mouse = injector.GetMouse();

		mouse.MoveTo(button1.GetAbsoluteBounds().GetCenter());
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(CoreCursorType.IBeam, GetCursorShape());

		// We need to use an actual pointer event. Note that on WinUI, this works normally without a pointer event,
		// meaning that WinUI most likely reapplies hit testing when programatically scrolling. We don't do this
		// in Uno. The fix is most likely not in ProtectedCursor-specific logic, but in ScrollViewer logic. We should
		// be hit testing again to see if we need to raise enter/leave events.
		// scrollViewer.ScrollToVerticalOffset(button1.ActualHeight);
		mouse.Wheel(-100);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(CoreCursorType.Arrow, GetCursorShape());
	}

	[TestMethod]
	[Ignore("https://github.com/unoplatform/uno/issues/15509")]
	public async Task When_ProtectedCursor_Scrolled_PointerCaptured()
	{
		var stackPanel = new StackPanel();
		for (var i = 1; i <= 10; i++)
		{
			var button = new Button { Content = "Button " + i };
			stackPanel.Children.Add(button);
		}

		var scrollViewer = new ScrollViewer()
		{
			VerticalScrollMode = ScrollMode.Enabled,
			Content = stackPanel,
			MaxHeight = 50,
		};

		var button1 = (Button)stackPanel.Children[0];

		button1.SetProtectedCursor(InputSystemCursor.Create(InputSystemCursorShape.IBeam));

		var border = new Border { scrollViewer };

		await UITestHelper.Load(border);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		var mouse = injector.GetMouse();

		mouse.MoveTo(button1.GetAbsoluteBounds().GetCenter());
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(CoreCursorType.IBeam, GetCursorShape());

		mouse.Press();

		// We need to use an actual pointer event. Note that on WinUI, this works normally without a pointer event,
		// meaning that WinUI most likely reapplies hit testing when programatically scrolling. We don't do this
		// in Uno. The fix is most likely not in ProtectedCursor-specific logic, but in ScrollViewer logic. We should
		// be hit testing again to see if we need to raise enter/leave events.
		// scrollViewer.ScrollToVerticalOffset(button1.ActualHeight);
		mouse.Wheel(-100);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(CoreCursorType.IBeam, GetCursorShape());

		mouse.Release();
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(CoreCursorType.Arrow, GetCursorShape());
	}

	[TestMethod]
	[Ignore("https://github.com/unoplatform/uno/issues/15509")]
	public async Task When_ProtectedCursor_Disposed()
	{
		var stackPanel = new StackPanel();
		for (var i = 1; i <= 10; i++)
		{
			var button = new Button { Content = "Button " + i };
			stackPanel.Children.Add(button);
		}

		var scrollViewer = new ScrollViewer()
		{
			VerticalScrollMode = ScrollMode.Enabled,
			Content = stackPanel,
			MaxHeight = 50,
		};

		var button1 = (Button)stackPanel.Children[0];

		var cursor = InputSystemCursor.Create(InputSystemCursorShape.IBeam);
		button1.SetProtectedCursor(cursor);

		var border = new Border { scrollViewer };

		await UITestHelper.Load(border);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		var mouse = injector.GetMouse();

		mouse.MoveTo(button1.GetAbsoluteBounds().GetCenter());
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(CoreCursorType.IBeam, GetCursorShape());

		cursor.Dispose();
		await TestServices.WindowHelper.WaitForIdle();

		// This is the behaviour on WinUI. The cursor won't change until you move the pointer.
		Assert.AreEqual(CoreCursorType.IBeam, GetCursorShape());

		mouse.MoveBy(1, 0);
		await TestServices.WindowHelper.WaitForIdle();

		var finalShape = GetCursorShape();

		// Each platform has its own way of dealing with null. We don't care about the specific
		// value, we are just testing that PointerInputSource.PointerCursor was indeed set to null,
		// the resulting cursor shape is irrelevant.
		SetCursorShape(null);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(GetCursorShape(), finalShape);
	}

	[TestMethod]
	[Ignore("https://github.com/unoplatform/uno/issues/15509")]
	public async Task When_ProtectedCursor_Set_Reset()
	{
		var stackPanel = new StackPanel();
		for (var i = 1; i <= 10; i++)
		{
			var button = new Button { Content = "Button " + i };
			stackPanel.Children.Add(button);
		}

		var scrollViewer = new ScrollViewer()
		{
			VerticalScrollMode = ScrollMode.Enabled,
			Content = stackPanel,
			MaxHeight = 50,
		};

		var button1 = (Button)stackPanel.Children[0];

		button1.SetProtectedCursor(InputSystemCursor.Create(InputSystemCursorShape.IBeam));

		var border = new Border { scrollViewer };

		await UITestHelper.Load(border);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		var mouse = injector.GetMouse();

		mouse.MoveTo(button1.GetAbsoluteBounds().GetCenter());
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(CoreCursorType.IBeam, GetCursorShape());

		button1.SetProtectedCursor(null);
		await TestServices.WindowHelper.WaitForIdle();

		// This is NOT the behaviour on WinUI. The cursor should change immediately. To align this behaviour,
		// we would need to keep track of which element is currently the one the cursor is based on.
		// To keep it simple, we skip this in Uno. You will need to nudge the pointer a bit to take effect.
		Assert.AreEqual(CoreCursorType.IBeam, GetCursorShape());

		mouse.MoveBy(1, 0);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(CoreCursorType.Arrow, GetCursorShape());
	}

	[TestMethod]
	public void Verify_Initialized()
	{
		var xamlRoot = TestServices.WindowHelper.XamlRoot;
		var contentRoot = xamlRoot.VisualTree.ContentRoot;
		Assert.IsTrue(contentRoot.InputManager.Initialized);
	}

	[TestMethod]
#if !HAS_INPUT_INJECTOR
	[Ignore("InputInjector is not supported on this platform.")]
#endif
	public async Task When_ReleaseOnSibling_Then_GetLeave()
	{
		Border col1, col2;
		var ui = new Grid
		{
			Width = 200,
			Height = 200,
			ColumnDefinitions =
			{
				new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
				new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
			},
			Children =
			{
				(col1 = new Border { Background = new SolidColorBrush(Colors.DeepPink) }),
				(col2 = new Border { Background = new SolidColorBrush(Colors.DeepSkyBlue) }),
			}
		};

		Grid.SetColumn(col1, 0);
		Grid.SetColumn(col2, 1);

		await UITestHelper.Load(ui);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var finger = injector.GetFinger();

		var exited = false;
		col1.PointerExited += (snd, args) => exited = true;

		finger.Press(col1.GetAbsoluteBounds().GetCenter());
		finger.MoveBy(1, 1);
		finger.Release(col2.GetAbsoluteBounds().GetCenter());

		Assert.IsTrue(exited, "Exited should have been raised on col1 as part of the release.");
	}

	[TestMethod]
#if __WASM__
	[Ignore("Scrolling is handled by native code and InputInjector is not yet able to inject native pointers.")]
#elif !HAS_INPUT_INJECTOR
	[Ignore("InputInjector is not supported on this platform.")]
#endif
	public async Task When_DirectManipulation_Then_AllTreeSetOverAndPressedFalse()
	{
		ScrollViewer sv;
		Border elt;
		var ui = new Grid
		{
			Width = 200,
			Height = 200,
			Children =
			{
				(sv = new ScrollViewer
				{
					Background = new SolidColorBrush(Colors.DeepPink),
					Content = elt = new Border
					{
						Background = new SolidColorBrush(Colors.DeepSkyBlue),
						Margin = new Thickness(10),
						Width = 800,
						Height = 800,
					}
				}),
			}
		};

		await UITestHelper.Load(ui);
		var root = ui.XamlRoot!.Content!;
		root.Should().NotBeNull();

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var finger = injector.GetFinger();

		finger.Press(new(sv.GetAbsoluteBounds().GetCenter().X, sv.GetAbsoluteBounds().Bottom - 5));

		elt.IsPointerOver.Should().BeTrue();
		sv.IsPointerOver.Should().BeTrue();
		ui.IsPointerOver.Should().BeTrue();
		root.IsPointerOver.Should().BeTrue();

		elt.IsPointerPressed.Should().BeTrue();
		sv.IsPointerPressed.Should().BeTrue();
		ui.IsPointerPressed.Should().BeTrue();
		root.IsPointerPressed.Should().BeTrue();

		// Scroll (cause direct manipulation to kick-in)
		finger.MoveTo(new(sv.GetAbsoluteBounds().GetCenter().X, sv.GetAbsoluteBounds().Top + 5));

		elt.IsPointerOver.Should().BeFalse();
		sv.IsPointerOver.Should().BeFalse();
		ui.IsPointerOver.Should().BeFalse();
		root.IsPointerOver.Should().BeFalse();

		elt.IsPointerPressed.Should().BeFalse();
		sv.IsPointerPressed.Should().BeFalse();
		ui.IsPointerPressed.Should().BeFalse();
		root.IsPointerPressed.Should().BeFalse();

		// This could be impacted by another test ... but if it fails it means that there is a bug in the InputManager!
		root.GetAllChildren().OfType<UIElement>().Any(elt => elt.IsPointerOver).Should().BeFalse();
		root.GetAllChildren().OfType<UIElement>().Any(elt => elt.IsPointerPressed).Should().BeFalse();
	}


	[TestMethod]
#if __WASM__
	[Ignore("Scrolling is handled by native code and InputInjector is not yet able to inject native pointers.")]
#elif !HAS_INPUT_INJECTOR
	[Ignore("InputInjector is not supported on this platform.")]
#endif
	public async Task When_DirectManipulationInertial_Then_AllSubsequentEventsIgnored()
	{
		Border elt;
		var root = new Grid
		{
			Width = 200,
			Height = 200,
			Children =
			{
				new ScrollViewer
				{
					Background = new SolidColorBrush(Colors.DeepPink),
					Content = elt = new Border
					{
						Background = new SolidColorBrush(Colors.DeepSkyBlue),
						Margin = new Thickness(10),
						Width = 800,
						Height = 80000,
					}
				},
			}
		};

		var bounds = await UITestHelper.Load(root);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var finger = injector.GetFinger();

		// Start inertial scrolling
		finger.Drag(from: bounds.GetCenter(), to: bounds.GetCenter().Offset(y: -3000), steps: 1, stepOffsetInMilliseconds: 300);

		var events = root.SubscribeToPointerEvents();
		elt.SubscribeToPointerEvents(events);

		await UITestHelper.WaitForIdle(waitForCompositionAnimations: false); // Give opportunity to ui thread to doe some work

		finger.Press(bounds.GetCenter());
		finger.Release(bounds.GetCenter());

		await UITestHelper.WaitForIdle(waitForCompositionAnimations: false); // Give opportunity to ui thread to doe some work

		events.Should().BeEmpty(because: "all pointer events should have been muted by direct manipulation");
	}


	[TestMethod]
#if __WASM__
	[Ignore("Scrolling is handled by native code and InputInjector is not yet able to inject native pointers.")]
#elif !HAS_INPUT_INJECTOR
	[Ignore("InputInjector is not supported on this platform.")]
#endif
	public async Task When_DirectManipulationInertial_Then_SubsequentManipulationStillPassingThrough()
	{
		ScrollViewer sv;
		Border elt;
		var root = new Grid
		{
			Width = 200,
			Height = 200,
			Children =
			{
				(sv = new ScrollViewer
				{
					UpdatesMode = Uno.UI.Xaml.Controls.ScrollViewerUpdatesMode.Synchronous,
					Background = new SolidColorBrush(Colors.DeepPink),
					Content = elt = new Border
					{
						Background = new SolidColorBrush(Colors.DeepSkyBlue),
						Margin = new Thickness(10),
						Width = 800,
						Height = 80000,
					}
				}),
			}
		};

		var bounds = await UITestHelper.Load(root);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var finger = injector.GetFinger();

		// Start inertial scrolling
		finger.Drag(from: bounds.GetCenter(), to: bounds.GetCenter().Offset(y: -1000), steps: 1, stepOffsetInMilliseconds: 100);

		await UITestHelper.WaitForIdle(waitForCompositionAnimations: false); // Give opportunity to ui thread to do some work (process inertia)

		var currentOffset = sv.VerticalOffset;

		// Scroll a bit more
		var secondScrollDelta = 100;
		finger.Drag(from: bounds.GetCenter(), to: bounds.GetCenter().Offset(y: -secondScrollDelta), steps: 1, stepOffsetInMilliseconds: 3000);

		sv.VerticalOffset.Should().BeApproximately(currentOffset + secondScrollDelta, precision: 2, because: "second press should have stop inertia and then the slow scroll have been applied");
	}

	[TestMethod]
#if __WASM__
	[Ignore("Scrolling is handled by native code and InputInjector is not yet able to inject native pointers.")]
#elif !HAS_INPUT_INJECTOR
	[Ignore("InputInjector is not supported on this platform.")]
#endif
	public async Task When_DirectManipulationMultiple_Then_RunsIndependently()
	{
		ScrollViewer sv1, sv2;
		var root = new Grid
		{
			Width = 400,
			Height = 200,
			ColumnDefinitions = { new ColumnDefinition(), new ColumnDefinition() },
			Children =
			{
				(sv1 = new ScrollViewer
				{
					UpdatesMode = Uno.UI.Xaml.Controls.ScrollViewerUpdatesMode.Synchronous,
					Background = new SolidColorBrush(Colors.DeepPink),
					Content = new Border
					{
						Background = new SolidColorBrush(Colors.BlueViolet),
						Margin = new Thickness(10),
						Width = 800,
						Height = 80000,
					}
				}.Apply(sv => Grid.SetColumn(sv, 0))),
				(sv2 = new ScrollViewer
				{
					UpdatesMode = Uno.UI.Xaml.Controls.ScrollViewerUpdatesMode.Synchronous,
					Background = new SolidColorBrush(Colors.Chartreuse),
					Content = new Border
					{
						Background = new SolidColorBrush(Colors.DarkGreen),
						Margin = new Thickness(10),
						Width = 800,
						Height = 80000,
					}
				}.Apply(sv => Grid.SetColumn(sv, 1))),
			}
		};

		await UITestHelper.Load(root);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");

		using var finger1 = injector.GetFinger();
		finger1.Press(sv1.GetAbsoluteBounds().GetCenter());

		using var finger2 = injector.GetFinger(id: 83);
		finger2.Press(sv2.GetAbsoluteBounds().GetCenter());

		// Start scrolling on both fingers
		finger1.MoveBy(y: -200);
		finger2.MoveBy(y: -100);

		sv1.VerticalOffset.Should().BeApproximately(200, precision: 2, because: "first finger should have scrolled the first ScrollViewer");
		sv2.VerticalOffset.Should().BeApproximately(100, precision: 2, because: "second finger should have scrolled the second ScrollViewer");

		// Release first finger and validate that first finger is still scrolling
		finger1.Release();
		finger2.MoveBy(y: -100);
		finger2.Release();

		sv1.VerticalOffset.Should().BeApproximately(200, precision: 2, because: "first ScrollViewer should not have been affected by second finger");
		sv2.VerticalOffset.Should().BeApproximately(200, precision: 2, because: "second finger should still be scrolling the second ScrollViewer after first finger release");
	}

	[TestMethod]
#if __WASM__
	[Ignore("Scrolling is handled by native code and InputInjector is not yet able to inject native pointers.")]
#elif !HAS_INPUT_INJECTOR
	[Ignore("InputInjector is not supported on this platform.")]
#endif
	public async Task When_DirectManipulationMultiple_Then_RunsIndependently_2()
	{
		ScrollViewer sv1, sv2;
		var root = new Grid
		{
			Width = 400,
			Height = 200,
			ColumnDefinitions = { new ColumnDefinition(), new ColumnDefinition() },
			Children =
			{
				(sv1 = new ScrollViewer
				{
					UpdatesMode = Uno.UI.Xaml.Controls.ScrollViewerUpdatesMode.Synchronous,
					Background = new SolidColorBrush(Colors.DeepPink),
					Content = new Border
					{
						Background = new SolidColorBrush(Colors.BlueViolet),
						Margin = new Thickness(10),
						Width = 800,
						Height = 80000,
					}
				}.Apply(sv => Grid.SetColumn(sv, 0))),
				(sv2 = new ScrollViewer
				{
					UpdatesMode = Uno.UI.Xaml.Controls.ScrollViewerUpdatesMode.Synchronous,
					Background = new SolidColorBrush(Colors.Chartreuse),
					Content = new Border
					{
						Background = new SolidColorBrush(Colors.DarkGreen),
						Margin = new Thickness(10),
						Width = 800,
						Height = 80000,
					}
				}.Apply(sv => Grid.SetColumn(sv, 1))),
			}
		};

		await UITestHelper.Load(root);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");

		using var finger1 = injector.GetFinger();
		finger1.Press(sv1.GetAbsoluteBounds().GetCenter());

		using var finger2 = injector.GetFinger(id: 83);
		finger2.Press(sv2.GetAbsoluteBounds().GetCenter());

		// Start scrolling on both fingers
		finger1.MoveBy(y: -100);
		finger2.MoveBy(y: -200);

		sv1.VerticalOffset.Should().BeApproximately(100, precision: 2, because: "first finger should have scrolled the first ScrollViewer");
		sv2.VerticalOffset.Should().BeApproximately(200, precision: 2, because: "second finger should have scrolled the second ScrollViewer");

		// Release second finger and validate that first finger is still scrolling
		finger2.Release();
		finger1.MoveBy(y: -100);
		finger1.Release();

		sv1.VerticalOffset.Should().BeApproximately(200, precision: 2, because: "first finger should still be scrolling the first ScrollViewer after second finger release");
		sv2.VerticalOffset.Should().BeApproximately(200, precision: 2, because: "second ScrollViewer should not have been affected by first finger");
	}

	[TestMethod]
#if __WASM__
	[Ignore("Scrolling is handled by native code and InputInjector is not yet able to inject native pointers.")]
#elif !HAS_INPUT_INJECTOR
	[Ignore("InputInjector is not supported on this platform.")]
#endif
	public async Task When_DirectManipulationMultipleWithInertia_Then_RunsIndependently()
	{
		ScrollViewer sv1, sv2;
		var root = new Grid
		{
			Width = 400,
			Height = 200,
			ColumnDefinitions = { new ColumnDefinition(), new ColumnDefinition() },
			Children =
			{
				(sv1 = new ScrollViewer
				{
					UpdatesMode = Uno.UI.Xaml.Controls.ScrollViewerUpdatesMode.Synchronous,
					IsScrollInertiaEnabled = true,
					Background = new SolidColorBrush(Colors.DeepPink),
					Content = new Border
					{
						Background = new SolidColorBrush(Colors.BlueViolet),
						Margin = new Thickness(10),
						Width = 800,
						Height = 80000,
					}
				}.Apply(sv => Grid.SetColumn(sv, 0))),
				(sv2 = new ScrollViewer
				{
					UpdatesMode = Uno.UI.Xaml.Controls.ScrollViewerUpdatesMode.Synchronous,
					IsScrollInertiaEnabled = false,
					Background = new SolidColorBrush(Colors.Chartreuse),
					Content = new Border
					{
						Background = new SolidColorBrush(Colors.DarkGreen),
						Margin = new Thickness(10),
						Width = 800,
						Height = 80000,
					}
				}.Apply(sv => Grid.SetColumn(sv, 1))),
			}
		};

		await UITestHelper.Load(root);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var finger = injector.GetFinger(); // We re-used the same finger ID on all interactions

		// Start inertia on SV1
		finger.Drag(
			from: sv1.GetAbsoluteBounds().GetCenter(),
			to: sv1.GetAbsoluteBounds().GetCenter().Offset(y: -200),
			steps: 1,
			stepOffsetInMilliseconds: 20);

		var sv1VOffsetAtEndOfSv1Drag = sv1.VerticalOffset; // Capture the initial offset after the drag
		sv1VOffsetAtEndOfSv1Drag.Should().BeGreaterThanOrEqualTo(200);

		// Attempt to scroll SV2
		finger.Drag(
			from: sv2.GetAbsoluteBounds().GetCenter(),
			to: sv2.GetAbsoluteBounds().GetCenter().Offset(y: -200),
			steps: 1);
		sv2.VerticalOffset.Should().BeApproximately(200, precision: 2, because: "second finger should have scrolled the second ScrollViewer");

		await UITestHelper.WaitForRender(); // Allow time for the inertia to process at least one frame

		var sv1VOffsetAtEndOfSv2Drag = sv1.VerticalOffset; // Capture the initial offset after the drag
		sv1VOffsetAtEndOfSv2Drag.Should().BeGreaterThan(sv1VOffsetAtEndOfSv1Drag, because: "Inertia should still be running after SV2 interaction");

		await UITestHelper.WaitForRender(10); // Allow time for the inertia to process more frames (to confirm it's still running even after finger2 has been released)

		sv1.VerticalOffset.Should().BeGreaterThan(sv1VOffsetAtEndOfSv2Drag, because: "Inertia should still be running");
	}

	[TestMethod]
#if __WASM__
	[Ignore("Scrolling is handled by native code and InputInjector is not yet able to inject native pointers.")]
#elif !HAS_INPUT_INJECTOR
	[Ignore("InputInjector is not supported on this platform.")]
#endif
	public async Task When_DirectManipulationMultipleWithMultipleInertia_Then_RunsIndependently()
	{
		ScrollViewer sv1, sv2;
		var root = new Grid
		{
			Width = 400,
			Height = 200,
			ColumnDefinitions = { new ColumnDefinition(), new ColumnDefinition() },
			Children =
			{
				(sv1 = new ScrollViewer
				{
					UpdatesMode = Uno.UI.Xaml.Controls.ScrollViewerUpdatesMode.Synchronous,
					IsScrollInertiaEnabled = true,
					Background = new SolidColorBrush(Colors.DeepPink),
					Content = new Border
					{
						Background = new SolidColorBrush(Colors.BlueViolet),
						Margin = new Thickness(10),
						Width = 800,
						Height = 80000,
					}
				}.Apply(sv => Grid.SetColumn(sv, 0))),
				(sv2 = new ScrollViewer
				{
					UpdatesMode = Uno.UI.Xaml.Controls.ScrollViewerUpdatesMode.Synchronous,
					IsScrollInertiaEnabled = true,
					Background = new SolidColorBrush(Colors.Chartreuse),
					Content = new Border
					{
						Background = new SolidColorBrush(Colors.DarkGreen),
						Margin = new Thickness(10),
						Width = 800,
						Height = 80000,
					}
				}.Apply(sv => Grid.SetColumn(sv, 1))),
			}
		};

		await UITestHelper.Load(root);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var finger = injector.GetFinger(); // We re-used the same finger ID on all interactions

		// Start inertia on SV1
		finger.Drag(
			from: sv1.GetAbsoluteBounds().GetCenter(),
			to: sv1.GetAbsoluteBounds().GetCenter().Offset(y: -200),
			steps: 1,
			stepOffsetInMilliseconds: 20);

		var sv1VOffsetAtEndOfSv1Drag = sv1.VerticalOffset; // Capture the initial offset after the drag
		sv1VOffsetAtEndOfSv1Drag.Should().BeGreaterThanOrEqualTo(200);

		// Start inertia on SV2
		finger.Drag(
			from: sv2.GetAbsoluteBounds().GetCenter(),
			to: sv2.GetAbsoluteBounds().GetCenter().Offset(y: -200),
			steps: 1,
			stepOffsetInMilliseconds: 20);

		await UITestHelper.WaitForRender(); // Allow time for the inertia to process at least one frame

		var sv1VOffsetAtEndOfSv2Drag = sv1.VerticalOffset; // Capture the initial offset after the drag
		sv1VOffsetAtEndOfSv2Drag.Should().BeGreaterThan(sv1VOffsetAtEndOfSv1Drag);
		var sv2VOffsetAtEndOfSv2Drag = sv2.VerticalOffset; // Capture the initial offset after the drag
		sv2VOffsetAtEndOfSv2Drag.Should().BeGreaterThanOrEqualTo(200);

		await Task.Delay(10); // Allow time for the inertia to process more frames (to confirm it's still running even after finger2 has been released)

		sv1.VerticalOffset.Should().BeGreaterThan(sv1VOffsetAtEndOfSv1Drag, because: "Inertia should still be running on sv1");
		sv2.VerticalOffset.Should().BeGreaterThan(sv2VOffsetAtEndOfSv2Drag, because: "Inertia should still be running on sv2");

		// Finally stop scrollers by tapping them
		finger.Tap(sv2.GetAbsoluteBounds().GetCenter());

		var sv1VOffsetAfterSv2Tap = sv1.VerticalOffset;
		var sv2VOffsetAfterSv2Tap = sv2.VerticalOffset;

		await UITestHelper.WaitForRender(); // Allow time for the inertia to process at least one frame

		sv1.VerticalOffset.Should().BeGreaterThan(sv1VOffsetAfterSv2Tap, because: "Inertia should still be running on sv1");
		sv2.VerticalOffset.Should().Be(sv2VOffsetAfterSv2Tap, because: "Inertia should have been stopped on sv2");
	}

	[TestMethod]
#if __WASM__
	[Ignore("Scrolling is handled by native code and InputInjector is not yet able to inject native pointers.")]
#elif !HAS_INPUT_INJECTOR
	[Ignore("InputInjector is not supported on this platform.")]
#endif
	public async Task When_DirectManipulationMixedWithUIElementManipulation_Then_TopMostWins()
	{
		ScrollViewer sv1, sv2;
		Border elt;
		var root = new Grid
		{
			Width = 200,
			Height = 200,
			Children =
			{
				(sv1 = new ScrollViewer
				{
					UpdatesMode = Uno.UI.Xaml.Controls.ScrollViewerUpdatesMode.Synchronous,
					IsScrollInertiaEnabled = false,
					Background = new SolidColorBrush(Colors.DeepPink),
					Content = elt = new Border
					{
						ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY | ManipulationModes.System,
						Background = new SolidColorBrush(Colors.DeepSkyBlue),
						Margin = new Thickness(10),
						Width = 800,
						Height = 800,
						Child = (sv2 = new ScrollViewer
						{
							UpdatesMode = Uno.UI.Xaml.Controls.ScrollViewerUpdatesMode.Synchronous,
							IsScrollInertiaEnabled = false,
							Content = new Border
							{
								Background = new SolidColorBrush(Colors.Chartreuse),
								Margin = new Thickness(10),
								Width = 2400,
								Height = 2400,
							}
						})
					}
				}),
			}
		};

		int starting = 0, started = 0, delta = 0, completed = 0;
		elt.ManipulationStarting += (snd, e) => starting++;
		elt.ManipulationStarted += (snd, e) => started++;
		elt.ManipulationDelta += (snd, e) => delta++;
		elt.ManipulationCompleted += (snd, e) => completed++;

		var bounds = await UITestHelper.Load(root);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var finger = injector.GetFinger();

		finger.Drag(from: bounds.GetCenter(), to: bounds.GetCenter().Offset(y: -100));

		await UITestHelper.WaitForIdle();

		sv2.VerticalOffset.Should().BeApproximately(100, precision: 2);
		starting.Should().Be(1, because: "pointer should have reached the element, giving it the opportunity to init the manipulation");
		started.Should().Be(0, because: "pointer should have been grabbed by the direct manipulation before the manipulation started on the element");
		delta.Should().Be(0);
		completed.Should().Be(0);
	}

	[TestMethod]
#if __WASM__
	[Ignore("Scrolling is handled by native code and InputInjector is not yet able to inject native pointers.")]
#elif !HAS_INPUT_INJECTOR
	[Ignore("InputInjector is not supported on this platform.")]
#endif
	public async Task When_DirectManipulationMixedWithUIElementManipulationInDifferentDirection_Then_TopMostWins()
	{
		ScrollViewer sv1, sv2;
		Border elt;
		var root = new Grid
		{
			Width = 200,
			Height = 200,
			Children =
			{
				(sv1 = new ScrollViewer
				{
					UpdatesMode = Uno.UI.Xaml.Controls.ScrollViewerUpdatesMode.Synchronous,
					IsScrollInertiaEnabled = false,
					Background = new SolidColorBrush(Colors.DeepPink),
					Content = elt = new Border
					{
						ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY | ManipulationModes.System,
						Background = new SolidColorBrush(Colors.DeepSkyBlue),
						Margin = new Thickness(10),
						Width = 800,
						Height = 800,
						Child = (sv2 = new ScrollViewer
						{
							UpdatesMode = Uno.UI.Xaml.Controls.ScrollViewerUpdatesMode.Synchronous,
							IsScrollInertiaEnabled = false,
							Content = new Border
							{
								Background = new SolidColorBrush(Colors.Chartreuse),
								Margin = new Thickness(10),
								Width = 2400,
								Height = 2400,
							}
						})
					}
				}),
			}
		};

		int starting = 0, started = 0, delta = 0, completed = 0;
		elt.ManipulationStarting += (snd, e) => starting++;
		elt.ManipulationStarted += (snd, e) => started++;
		elt.ManipulationDelta += (snd, e) => delta++;
		elt.ManipulationCompleted += (snd, e) => completed++;

		var bounds = await UITestHelper.Load(root);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var finger = injector.GetFinger();

		finger.Drag(from: bounds.GetCenter(), to: bounds.GetCenter().Offset(x: -100));

		await UITestHelper.WaitForIdle();

		sv2.HorizontalOffset.Should().Be(0, because: "horizontal scrolling has not been enabled");
		starting.Should().Be(1, because: "pointer should have reached the element, giving it the opportunity to init the manipulation");
		started.Should().Be(1, because: "the manipulation should have kick-in as the ScrollViewer was not able to start");
		delta.Should().NotBe(0);
		completed.Should().Be(1);
	}

	[TestMethod]
#if __WASM__
	[Ignore("Scrolling is handled by native code and InputInjector is not yet able to inject native pointers.")]
#elif !HAS_INPUT_INJECTOR
	[Ignore("InputInjector is not supported on this platform.")]
#endif
	public async Task When_DirectManipulationMixedWithUIElementManipulation_Then_TopMostWinsAndChainingWorks()
	{
		ScrollViewer sv1, sv2;
		Border elt;
		var root = new Grid
		{
			Width = 200,
			Height = 200,
			Children =
			{
				(sv1 = new ScrollViewer
				{
					UpdatesMode = Uno.UI.Xaml.Controls.ScrollViewerUpdatesMode.Synchronous,
					IsScrollInertiaEnabled = false,
					Background = new SolidColorBrush(Colors.DeepPink),
					Content = elt = new Border
					{
						ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY | ManipulationModes.System,
						Background = new SolidColorBrush(Colors.DeepSkyBlue),
						Margin = new Thickness(10),
						Width = 800,
						Height = 800,
						Child = (sv2 = new ScrollViewer
						{
							UpdatesMode = Uno.UI.Xaml.Controls.ScrollViewerUpdatesMode.Synchronous,
							IsScrollInertiaEnabled = false,
							Content = new Border
							{
								Background = new SolidColorBrush(Colors.Chartreuse),
								Margin = new Thickness(10),
								Width = 2400,
								Height = 2400,
							}
						})
					}
				}),
			}
		};

		int starting = 0, started = 0, delta = 0, completed = 0;
		elt.ManipulationStarting += (snd, e) => starting++;
		elt.ManipulationStarted += (snd, e) => started++;
		elt.ManipulationDelta += (snd, e) => delta++;
		elt.ManipulationCompleted += (snd, e) => completed++;

		var bounds = await UITestHelper.Load(root);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var finger = injector.GetFinger();

		finger.Drag(from: bounds.GetCenter(), to: bounds.GetCenter().Offset(y: -8000));

		await UITestHelper.WaitForIdle();

		sv1.VerticalOffset.Should().BeApproximately(620, precision: 2);
		sv2.VerticalOffset.Should().BeApproximately(1620, precision: 2);
		starting.Should().Be(1, because: "pointer should have reached the element, giving it the opportunity to init the manipulation");
		started.Should().Be(0, because: "pointer should have been grabbed by the direct manipulation before the manipulation started on the element");
		delta.Should().Be(0);
		completed.Should().Be(0);
	}

	[TestMethod]
#if __WASM__
	[Ignore("Scrolling is handled by native code and InputInjector is not yet able to inject native pointers.")]
#elif !HAS_INPUT_INJECTOR
	[Ignore("InputInjector is not supported on this platform.")]
#endif
	[DataRow(ManipulationModes.None)]
	[DataRow(ManipulationModes.TranslateX)]
	[DataRow(ManipulationModes.TranslateY)]
	[DataRow(ManipulationModes.TranslateRailsX)]
	[DataRow(ManipulationModes.TranslateRailsY)]
	[DataRow(ManipulationModes.TranslateInertia)]
	[DataRow(ManipulationModes.All)] // Does **NOT** include System
	public async Task When_DirectManipulationDisabled(ManipulationModes mode)
	{
		ScrollViewer sv;
		var ui = new Grid
		{
			Width = 200,
			Height = 200,
			Children =
			{
				(sv = new ScrollViewer
				{
					UpdatesMode = Uno.UI.Xaml.Controls.ScrollViewerUpdatesMode.Synchronous,
					IsScrollInertiaEnabled = false,
					Background = new SolidColorBrush(Colors.DeepPink),
					Content = new Border
					{
						ManipulationMode = mode,
						Background = new SolidColorBrush(Colors.DeepSkyBlue),
						Margin = new Thickness(10),
						Width = 800,
						Height = 800,
					}
				}),
			}
		};

		var bounds = await UITestHelper.Load(ui);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var finger = injector.GetFinger();

		finger.Drag(from: bounds.GetCenter(), to: bounds.GetCenter().Offset(y: -8000));

		await UITestHelper.WaitForIdle();

		sv.VerticalOffset.Should().Be(0);
	}

	private CoreCursorType? GetCursorShape()
	{
		var cursor = TestServices.WindowHelper
			.XamlRoot
			.VisualTree
			.ContentRoot
			.InputManager
			.Pointers
			.PointerInputSourceForTestingOnly
			?.PointerCursor;

		return cursor?.Type;
	}

	private static void SetCursorShape(CoreCursor cursor)
	{
		if (TestServices.WindowHelper
			.XamlRoot
			.VisualTree
			.ContentRoot
			.InputManager
			.Pointers
			.PointerInputSourceForTestingOnly is { } source)
		{
			source.PointerCursor = cursor;
		}
	}
}
#endif
