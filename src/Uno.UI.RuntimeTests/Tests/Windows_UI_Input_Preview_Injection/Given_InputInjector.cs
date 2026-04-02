#if HAS_INPUT_INJECTOR || WINAPPSDK

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Input.Preview.Injection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Extensions;
using Uno.UITest;
using static Private.Infrastructure.TestServices.WindowHelper;
using Private.Infrastructure;
using Windows.Foundation;
using Uno.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Toolkit.DevTools.Input;

#if HAS_UNO_WINUI
using PointerDeviceType = Microsoft.UI.Input.PointerDeviceType;
#else
using PointerDeviceType = Windows.Devices.Input.PointerDeviceType;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Input_Preview_Injection;

[TestClass]
public class Given_InputInjector
{
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_InjectTouch()
	{
		if (TestServices.WindowHelper.IsXamlIsland)
		{
			// Input injection is not supported in XamlIslands
			return;
		}

		var target = new Border
		{
			Background = new SolidColorBrush(Colors.DeepPink),
			Width = 200,
			Height = 200,
		};

		var actual = new List<PointerRoutedEventArgs>();
		target.PointerEntered += (snd, args) => actual.Add(args);
		target.PointerPressed += (snd, args) => actual.Add(args);
		target.PointerMoved += (snd, args) => actual.Add(args);
		target.PointerReleased += (snd, args) => actual.Add(args);
		target.PointerExited += (snd, args) => actual.Add(args);

		WindowContent = target;
		await WaitForLoaded(target);
		await WaitForIdle();

		var targetLocation = target.TransformToVisual(null).TransformPoint(default);
		var injector = InputInjector.TryCreate();

		injector.InitializeTouchInjection(InjectedInputVisualizationMode.Default);
		injector.InjectTouchInput(new[]
		{
			new InjectedInputTouchInfo
			{
				PointerInfo = new()
				{
					PointerId = 42,
					PixelLocation = new ()
					{
						PositionX = (int)targetLocation.X + 100,
						PositionY = (int)targetLocation.Y + 100
					},
					PointerOptions = InjectedInputPointerOptions.New
						| InjectedInputPointerOptions.FirstButton
						| InjectedInputPointerOptions.PointerDown
						| InjectedInputPointerOptions.InContact
				}
			},
			new InjectedInputTouchInfo
			{
				PointerInfo = new()
				{
					PointerId = 42,
					PixelLocation =
					{
						PositionX = (int)targetLocation.X + 100 + 2,
						PositionY = (int)targetLocation.Y + 100 + 2
					},
					PointerOptions = InjectedInputPointerOptions.FirstButton
						| InjectedInputPointerOptions.Update
						| InjectedInputPointerOptions.InContact
				}
			},
			new InjectedInputTouchInfo
			{
				PointerInfo = new()
				{
					PointerId = 42,
					PixelLocation =
					{
						PositionX = (int)targetLocation.X + 100 + 2,
						PositionY = (int)targetLocation.Y + 100 + 2
					},
					PointerOptions = InjectedInputPointerOptions.FirstButton
						| InjectedInputPointerOptions.PointerUp
				}
			}
		});
		injector.UninitializeTouchInjection();

		Assert.AreNotEqual(0, actual.Count);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_MouseClick_PointerPressedAndReleasedAreRaised()
	{
		if (TestServices.WindowHelper.IsXamlIsland)
		{
			return;
		}

		var border = new Border
		{
			Background = new SolidColorBrush(Colors.DeepPink),
			Width = 200,
			Height = 200,
		};

		var pressedCount = 0;
		var releasedCount = 0;
		border.PointerPressed += (s, e) => pressedCount++;
		border.PointerReleased += (s, e) => releasedCount++;

		WindowContent = border;
		await WaitForLoaded(border);
		await WaitForIdle();

		var injector = InputInjector.TryCreate();
		Assert.IsNotNull(injector);

		var mouse = injector.GetMouse();
		var center = border.TransformToVisual(TestServices.WindowHelper.XamlRoot.Content).TransformPoint(
			new Point(border.ActualWidth / 2, border.ActualHeight / 2));
		mouse.Press(center);
		mouse.Release();

		await WaitForIdle();

		Assert.AreNotEqual(0, pressedCount, "PointerPressed should have been raised");
		Assert.AreNotEqual(0, releasedCount, "PointerReleased should have been raised");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_MouseMove_PointerEventsAreRaised()
	{
		if (TestServices.WindowHelper.IsXamlIsland)
		{
			return;
		}

		var border = new Border
		{
			Background = new SolidColorBrush(Colors.DeepPink),
			Width = 200,
			Height = 200,
		};

		var enteredCount = 0;
		var movedCount = 0;
		border.PointerEntered += (s, e) => enteredCount++;
		border.PointerMoved += (s, e) => movedCount++;

		WindowContent = border;
		await WaitForLoaded(border);
		await WaitForIdle();

		var injector = InputInjector.TryCreate();
		Assert.IsNotNull(injector);

		var mouse = injector.GetMouse();
		var topLeft = border.TransformToVisual(TestServices.WindowHelper.XamlRoot.Content).TransformPoint(new Point(0, 0));
		var left = new Point(topLeft.X + 20, topLeft.Y + border.ActualHeight / 2);
		var right = new Point(topLeft.X + border.ActualWidth - 20, topLeft.Y + border.ActualHeight / 2);

		mouse.MoveTo(left, steps: 5);
		mouse.MoveTo(right, steps: 5);

		await WaitForIdle();

		Assert.AreNotEqual(0, movedCount, "PointerMoved should have been raised");
	}

#if HAS_UNO
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_PenTap_PointerPressedAndReleasedAreRaised()
	{
		if (TestServices.WindowHelper.IsXamlIsland)
		{
			// Input injection is not supported in XamlIslands
			return;
		}

		var border = new Border
		{
			Background = new SolidColorBrush(Colors.DeepPink),
			Width = 200,
			Height = 200,
		};

		var pressedCount = 0;
		var releasedCount = 0;
		border.PointerPressed += (s, e) => pressedCount++;
		border.PointerReleased += (s, e) => releasedCount++;

		WindowContent = border;
		await WaitForLoaded(border);
		await WaitForIdle();

		var injector = InputInjector.TryCreate();
		Assert.IsNotNull(injector);

		using var pen = injector.GetPen();
		var center = border.GetAbsoluteBounds().GetCenter();
		pen.Tap(center);

		await WaitForIdle();

		Assert.AreNotEqual(0, pressedCount);
		Assert.AreNotEqual(0, releasedCount);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_PenMove_PointerMovedIsRaised()
	{
		if (TestServices.WindowHelper.IsXamlIsland)
		{
			// Input injection is not supported in XamlIslands
			return;
		}

		var border = new Border
		{
			Background = new SolidColorBrush(Colors.DeepPink),
			Width = 200,
			Height = 200,
		};

		var movedCount = 0;
		border.PointerMoved += (s, e) => movedCount++;

		WindowContent = border;
		await WaitForLoaded(border);
		await WaitForIdle();

		var injector = InputInjector.TryCreate();
		Assert.IsNotNull(injector);

		using var pen = injector.GetPen();
		var bounds = border.GetAbsoluteBounds();
		var left = new Point(bounds.Left + 20, bounds.Top + bounds.Height / 2);
		var right = new Point(bounds.Right - 20, bounds.Top + bounds.Height / 2);

		pen.Press(left);
		pen.MoveTo(right, steps: 5);
		pen.Release();

		await WaitForIdle();

		Assert.AreNotEqual(0, movedCount);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_PenPress_PointerDeviceTypeIsPen()
	{
		if (TestServices.WindowHelper.IsXamlIsland)
		{
			// Input injection is not supported in XamlIslands
			return;
		}

		var border = new Border
		{
			Background = new SolidColorBrush(Colors.DeepPink),
			Width = 200,
			Height = 200,
		};

		PointerDeviceType? capturedDeviceType = null;
		border.PointerPressed += (s, e) => capturedDeviceType = e.Pointer.PointerDeviceType;

		WindowContent = border;
		await WaitForLoaded(border);
		await WaitForIdle();

		var injector = InputInjector.TryCreate();
		Assert.IsNotNull(injector);

		using var pen = injector.GetPen();
		var center = border.GetAbsoluteBounds().GetCenter();
		pen.Press(center);
		pen.Release();

		await WaitForIdle();

		Assert.IsNotNull(capturedDeviceType);
		Assert.AreEqual(PointerDeviceType.Pen, capturedDeviceType);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_PenPressure_PressureIsSet()
	{
		if (TestServices.WindowHelper.IsXamlIsland)
		{
			return;
		}

		var border = new Border
		{
			Background = new SolidColorBrush(Colors.DeepPink),
			Width = 200,
			Height = 200,
		};

		float? capturedPressure = null;
		border.PointerPressed += (s, e) => capturedPressure = e.GetCurrentPoint(null).Properties.Pressure;

		WindowContent = border;
		await WaitForLoaded(border);
		await WaitForIdle();

		var injector = InputInjector.TryCreate();
		Assert.IsNotNull(injector);

		var center = border.GetAbsoluteBounds().GetCenter();
		injector.InitializePenInjection(InjectedInputVisualizationMode.Default);
		injector.InjectPenInput(new InjectedInputPenInfo
		{
			PointerInfo = new()
			{
				PointerId = 1,
				PixelLocation = new() { PositionX = (int)center.X, PositionY = (int)center.Y },
				PointerOptions = InjectedInputPointerOptions.New
					| InjectedInputPointerOptions.FirstButton
					| InjectedInputPointerOptions.PointerDown
					| InjectedInputPointerOptions.InContact
					| InjectedInputPointerOptions.InRange
			},
			PenParameters = InjectedInputPenParameters.Pressure,
			Pressure = 0.75
		});
		injector.UninitializePenInjection();

		await WaitForIdle();

		Assert.IsNotNull(capturedPressure);
		Assert.AreEqual(0.75f, capturedPressure.Value, 0.01f);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_PenTilt_TiltValuesAreSet()
	{
		if (TestServices.WindowHelper.IsXamlIsland)
		{
			return;
		}

		var border = new Border
		{
			Background = new SolidColorBrush(Colors.DeepPink),
			Width = 200,
			Height = 200,
		};

		float? capturedXTilt = null;
		float? capturedYTilt = null;
		border.PointerPressed += (s, e) =>
		{
			var props = e.GetCurrentPoint(null).Properties;
			capturedXTilt = props.XTilt;
			capturedYTilt = props.YTilt;
		};

		WindowContent = border;
		await WaitForLoaded(border);
		await WaitForIdle();

		var injector = InputInjector.TryCreate();
		Assert.IsNotNull(injector);

		var center = border.GetAbsoluteBounds().GetCenter();
		injector.InitializePenInjection(InjectedInputVisualizationMode.Default);
		injector.InjectPenInput(new InjectedInputPenInfo
		{
			PointerInfo = new()
			{
				PointerId = 1,
				PixelLocation = new() { PositionX = (int)center.X, PositionY = (int)center.Y },
				PointerOptions = InjectedInputPointerOptions.New
					| InjectedInputPointerOptions.FirstButton
					| InjectedInputPointerOptions.PointerDown
					| InjectedInputPointerOptions.InContact
					| InjectedInputPointerOptions.InRange
			},
			PenParameters = InjectedInputPenParameters.Pressure
				| InjectedInputPenParameters.TiltX
				| InjectedInputPenParameters.TiltY,
			Pressure = 0.5,
			TiltX = 30,
			TiltY = -15
		});
		injector.UninitializePenInjection();

		await WaitForIdle();

		Assert.IsNotNull(capturedXTilt);
		Assert.IsNotNull(capturedYTilt);
		Assert.AreEqual(30f, capturedXTilt.Value, 0.01f);
		Assert.AreEqual(-15f, capturedYTilt.Value, 0.01f);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_PenBarrelButton_IsBarrelButtonPressed()
	{
		if (TestServices.WindowHelper.IsXamlIsland)
		{
			return;
		}

		var border = new Border
		{
			Background = new SolidColorBrush(Colors.DeepPink),
			Width = 200,
			Height = 200,
		};

		bool? capturedBarrelButton = null;
		border.PointerPressed += (s, e) => capturedBarrelButton = e.GetCurrentPoint(null).Properties.IsBarrelButtonPressed;

		WindowContent = border;
		await WaitForLoaded(border);
		await WaitForIdle();

		var injector = InputInjector.TryCreate();
		Assert.IsNotNull(injector);

		var center = border.GetAbsoluteBounds().GetCenter();
		injector.InitializePenInjection(InjectedInputVisualizationMode.Default);
		injector.InjectPenInput(new InjectedInputPenInfo
		{
			PointerInfo = new()
			{
				PointerId = 1,
				PixelLocation = new() { PositionX = (int)center.X, PositionY = (int)center.Y },
				PointerOptions = InjectedInputPointerOptions.New
					| InjectedInputPointerOptions.FirstButton
					| InjectedInputPointerOptions.PointerDown
					| InjectedInputPointerOptions.InContact
					| InjectedInputPointerOptions.InRange
			},
			PenParameters = InjectedInputPenParameters.Pressure,
			Pressure = 0.5,
			PenButtons = InjectedInputPenButtons.Barrel
		});
		injector.UninitializePenInjection();

		await WaitForIdle();

		Assert.IsNotNull(capturedBarrelButton);
		Assert.IsTrue(capturedBarrelButton.Value);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_PenEraser_IsEraserSet()
	{
		if (TestServices.WindowHelper.IsXamlIsland)
		{
			return;
		}

		var border = new Border
		{
			Background = new SolidColorBrush(Colors.DeepPink),
			Width = 200,
			Height = 200,
		};

		bool? capturedEraser = null;
		border.PointerPressed += (s, e) => capturedEraser = e.GetCurrentPoint(null).Properties.IsEraser;

		WindowContent = border;
		await WaitForLoaded(border);
		await WaitForIdle();

		var injector = InputInjector.TryCreate();
		Assert.IsNotNull(injector);

		var center = border.GetAbsoluteBounds().GetCenter();
		injector.InitializePenInjection(InjectedInputVisualizationMode.Default);
		injector.InjectPenInput(new InjectedInputPenInfo
		{
			PointerInfo = new()
			{
				PointerId = 1,
				PixelLocation = new() { PositionX = (int)center.X, PositionY = (int)center.Y },
				PointerOptions = InjectedInputPointerOptions.New
					| InjectedInputPointerOptions.FirstButton
					| InjectedInputPointerOptions.PointerDown
					| InjectedInputPointerOptions.InContact
					| InjectedInputPointerOptions.InRange
			},
			PenParameters = InjectedInputPenParameters.Pressure,
			Pressure = 0.5,
			PenButtons = InjectedInputPenButtons.Eraser
		});
		injector.UninitializePenInjection();

		await WaitForIdle();

		Assert.IsNotNull(capturedEraser);
		Assert.IsTrue(capturedEraser.Value);
	}
#endif
}
#endif
