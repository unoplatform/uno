using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Input.Preview.Injection;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Controls;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Uno_UI_Xaml_Core;

// Note: This file contains a bunch of helpers that are expected to be moved to the test engine among the pointer injection work

public static class UITestHelper
{
	public static async Task<Windows.Foundation.Rect> Load(FrameworkElement element)
	{
		TestServices.WindowHelper.WindowContent = element;
		await TestServices.WindowHelper.WaitForLoaded(element);
		element.UpdateLayout();
		await TestServices.WindowHelper.WaitForIdle();

		return element.GetAbsoluteBounds();
	}

	public static async Task<RawBitmap> ScreenShot(FrameworkElement element, bool opaque = true)
	{
		var renderer = new RenderTargetBitmap();
		element.UpdateLayout();
		await TestServices.WindowHelper.WaitForIdle();
		await renderer.RenderAsync(element);
		var bitmap = await RawBitmap.From(renderer, element);

		return bitmap;
	}
}

public static class InputInjectorExtensions
{
	public static Finger GetFinger(this InputInjector injector, uint id = 42)
		=> new(injector, id);

#if !WINDOWS_UWP
	public static Mouse GetMouse(this InputInjector injector)
		=> new(injector);
#endif
}

public interface IInjectedPointer
{
	void Press(Point position);

	void MoveTo(Point position);

	void MoveBy(double deltaX = 0, double deltaY = 0);

	void Release();
}

public static class FrameworkElementExtensions
{
	public static Rect GetAbsoluteBounds(this FrameworkElement element)
		=> element.TransformToVisual(null).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));
}

public static class PointExtensions
{
	public static Point Offset(this Point point, double xAndY)
		=> new(point.X + xAndY, point.Y + xAndY);

	public static Point Offset(this Point point, double x, double y)
		=> new(point.X + x, point.Y + y);
}

public static class InjectedPointerExtensions
{
	public static void Press(this IInjectedPointer pointer, double x, double y)
		=> pointer.Press(new(x, y));

	public static void MoveTo(this IInjectedPointer pointer, double x, double y)
		=> pointer.MoveTo(new(x, y));

	public static void Drag(this IInjectedPointer pointer, Point from, Point to)
	{
		pointer.Press(from);
		pointer.MoveTo(to);
		pointer.Release();
	}
}

public class Finger : IInjectedPointer, IDisposable
{
	private const uint _defaultMoveSteps = 10;

	private readonly InputInjector _injector;
	private readonly uint _id;

	private Point? _currentPosition;

	public Finger(InputInjector injector, uint id)
	{
		_injector = injector;
		_id = id;

		_injector.InitializeTouchInjection(InjectedInputVisualizationMode.Default);
	}

	public void Press(Point position)
	{
		if (_currentPosition is null)
		{
			Inject(GetPress(_id, position));
			_currentPosition = position;
		}
	}

	void IInjectedPointer.MoveTo(Point position) => MoveTo(position);
	public void MoveTo(Point position, uint steps = _defaultMoveSteps)
	{
		if (_currentPosition is { } current)
		{
			Inject(GetMove(current, position, steps));
			_currentPosition = position;
		}
	}

	void IInjectedPointer.MoveBy(double deltaX, double deltaY) => MoveBy(deltaX, deltaY);
	public void MoveBy(double deltaX, double deltaY, uint steps = _defaultMoveSteps)
	{
		if (_currentPosition is { } current)
		{
			MoveTo(current.Offset(deltaX, deltaY), steps);
		}
	}

	public void Release()
	{
		if (_currentPosition is { } current)
		{
			Inject(GetRelease(current));
			_currentPosition = null;
		}
	}

	public void Dispose()
	{
		Release();
		_injector.UninitializeTouchInjection();
	}

	public static InjectedInputTouchInfo GetPress(uint id, Point position)
		=> new()
		{
			PointerInfo = new()
			{
				PointerId = id,
				PixelLocation = At(position),
				PointerOptions = InjectedInputPointerOptions.New
					| InjectedInputPointerOptions.FirstButton
					| InjectedInputPointerOptions.PointerDown
					| InjectedInputPointerOptions.InContact
			}
		};

	public static IEnumerable<InjectedInputTouchInfo> GetMove(Point fromPosition, Point toPosition, uint steps = _defaultMoveSteps)
	{
		steps += 1; // We need to send at least the final location, but steps refers to the number of intermediate points

		var stepX = (toPosition.X - fromPosition.X) / steps;
		var stepY = (toPosition.Y - fromPosition.Y) / steps;
		for (var step = 1; step <= steps; step++)
		{
			yield return new()
			{
				PointerInfo = new()
				{
					PixelLocation = At(fromPosition.X + step * stepX, fromPosition.Y + step * stepY),
					PointerOptions = InjectedInputPointerOptions.Update
						| InjectedInputPointerOptions.FirstButton
						| InjectedInputPointerOptions.InContact
						| InjectedInputPointerOptions.InRange
				}
			};
		}
	}

	public static InjectedInputTouchInfo GetRelease(Point position)
		=> new()
		{
			PointerInfo = new()
			{
				PixelLocation = At(position),
				PointerOptions = InjectedInputPointerOptions.FirstButton
					| InjectedInputPointerOptions.PointerUp
			}
		};

	private void Inject(IEnumerable<InjectedInputTouchInfo> infos)
		=> _injector.InjectTouchInput(infos.ToArray());

	private void Inject(params InjectedInputTouchInfo[] infos)
		=> _injector.InjectTouchInput(infos);

	// Note: This a patch until Uno's pointer injection is being relative to the screen
	private static InjectedInputPoint At(Point position)
		=> At(position.X, position.Y);

	private static InjectedInputPoint At(double x, double y)
#if HAS_UNO
		=> new() { PositionX = (int)x, PositionY = (int)y };
#else
	{
		var bounds = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().VisibleBounds;
		var scale = Windows.Graphics.Display.DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;

		return new()
		{
			PositionX = (int)((bounds.X + x) * scale),
			PositionY = (int)((bounds.Y + y) * scale),
		};
	}
#endif
}

#if !WINDOWS_UWP
public class Mouse : IInjectedPointer, IDisposable
{
	private readonly InputInjector _input;

	public Mouse(InputInjector input)
	{
		_input = input;
	}

	private Point Current => _input.Mouse.Position;

	public void Press(Point position)
		=> Inject(GetMoveTo(position.X, position.Y, null).Concat(new[] { GetPress() }));

	public void Press()
		=> Inject(GetPress());

	public void Release()
		=> Inject(GetRelease());

	public void ReleaseAny()
	{
		var options = default(InjectedInputMouseOptions);

		var current = _input.Mouse;
		if (current.Properties.IsLeftButtonPressed)
		{
			options |= InjectedInputMouseOptions.LeftUp;
		}

		if (current.Properties.IsMiddleButtonPressed)
		{
			options |= InjectedInputMouseOptions.MiddleUp;
		}

		if (current.Properties.IsRightButtonPressed)
		{
			options |= InjectedInputMouseOptions.RightUp;
		}

		if (current.Properties.IsXButton1Pressed)
		{
			options |= InjectedInputMouseOptions.XUp;
		}

		if (options != default)
		{
			Inject(new InjectedInputMouseInfo
			{
				TimeOffsetInMilliseconds = 1,
				MouseOptions = options
			});
		}
	}

	public void MoveBy(double deltaX, double deltaY)
		=> Inject(GetMoveBy(deltaX, deltaY));

	void IInjectedPointer.MoveTo(Point position) => MoveTo(position);
	public void MoveTo(Point position, uint? steps = null)
		=> Inject(GetMoveTo(position.X, position.Y, steps));

	public void WheelUp() => Wheel(ScrollContentPresenter.ScrollViewerDefaultMouseWheelDelta);
	public void WheelDown() => Wheel(-ScrollContentPresenter.ScrollViewerDefaultMouseWheelDelta);
	public void WheelRight() => Wheel(ScrollContentPresenter.ScrollViewerDefaultMouseWheelDelta, isHorizontal: true);
	public void WheelLeft() => Wheel(-ScrollContentPresenter.ScrollViewerDefaultMouseWheelDelta, isHorizontal: true);

	public void Wheel(double delta, bool isHorizontal = false)
		=> Inject(GetWheel(delta, isHorizontal));

	private IEnumerable<InjectedInputMouseInfo> GetMoveTo(double x, double y, uint? steps)
	{
		var deltaX = x - Current.X;
		var deltaY = y - Current.Y;

		steps ??= (uint)Math.Min(Math.Max(Math.Abs(deltaX), Math.Abs(deltaY)), 512);
		if (steps is 0)
		{
			yield break;
		}

		var stepX = deltaX / steps.Value;
		var stepY = deltaY / steps.Value;

		stepX = stepX is > 0 ? Math.Ceiling(stepX) : Math.Floor(stepX);
		stepY = stepY is > 0 ? Math.Ceiling(stepY) : Math.Floor(stepY);

		for (var step = 0; step <= steps && (stepX is not 0 || stepY is not 0); step++)
		{
			yield return GetMoveBy((int)stepX, (int)stepY);

			if (Math.Abs(Current.X - x) < stepX)
			{
				stepX = 0;
			}

			if (Math.Abs(Current.Y - y) < stepY)
			{
				stepY = 0;
			}
		}
	}

	private void Inject(IEnumerable<InjectedInputMouseInfo> infos)
		=> _input.InjectMouseInput(infos);

	private void Inject(params InjectedInputMouseInfo[] infos)
		=> _input.InjectMouseInput(infos);

	public void Dispose()
		=> ReleaseAny();

	private static InjectedInputMouseInfo GetPress()
		=> new()
		{
			TimeOffsetInMilliseconds = 1,
			MouseOptions = InjectedInputMouseOptions.LeftDown,
		};

	private static InjectedInputMouseInfo GetMoveBy(double deltaX, double deltaY)
		=> new()
		{
			DeltaX = (int)deltaX,
			DeltaY = (int)deltaY,
			TimeOffsetInMilliseconds = 1,
			MouseOptions = InjectedInputMouseOptions.MoveNoCoalesce,
		};

	private static InjectedInputMouseInfo GetRelease()
		=> new()
		{
			TimeOffsetInMilliseconds = 1,
			MouseOptions = InjectedInputMouseOptions.LeftUp,
		};

	public static InjectedInputMouseInfo GetWheel(double delta, bool isHorizontal)
		=> isHorizontal
			? new() { TimeOffsetInMilliseconds = 1, DeltaX = (int)delta, MouseOptions = InjectedInputMouseOptions.HWheel }
			: new() { TimeOffsetInMilliseconds = 1, DeltaY = (int)delta, MouseOptions = InjectedInputMouseOptions.Wheel };
}
#endif
