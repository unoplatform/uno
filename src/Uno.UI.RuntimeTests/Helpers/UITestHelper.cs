#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Input.Preview.Injection;
using Windows.UI.Xaml;
using Private.Infrastructure;

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
}

public static class InputInjectorExtensions
{
	public static Finger GetFinger(this InputInjector injector, uint id = 42)
		=> new(injector, id);
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
	private const int _defaultMoveSteps = 10;

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
	public void MoveTo(Point position, int steps = _defaultMoveSteps)
	{
		if (_currentPosition is { } current)
		{
			Inject(GetMove(current, position, steps));
			_currentPosition = position;
		}
	}

	void IInjectedPointer.MoveBy(double deltaX, double deltaY) => MoveBy(deltaX, deltaY);
	public void MoveBy(double deltaX, double deltaY, int steps = _defaultMoveSteps)
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
				PixelLocation = new()
				{
					PositionX = (int)position.X,
					PositionY = (int)position.Y
				},
				PointerOptions = InjectedInputPointerOptions.New
					| InjectedInputPointerOptions.FirstButton
					| InjectedInputPointerOptions.PointerDown
					| InjectedInputPointerOptions.InContact
			}
		};

	public static IEnumerable<InjectedInputTouchInfo> GetMove(Point fromPosition, Point toPosition, int steps = _defaultMoveSteps)
	{
		var stepX = (toPosition.X - fromPosition.X) / steps;
		var stepY = (toPosition.Y - fromPosition.Y) / steps;
		for (var step = 0; step <= steps; step++)
		{
			yield return new()
			{
				PointerInfo = new()
				{
					PixelLocation = new()
					{
						PositionX = (int)(fromPosition.X + step * stepX),
						PositionY = (int)(fromPosition.Y + step * stepY)
					},
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
				PixelLocation =
				{
					PositionX = (int)position.X,
					PositionY = (int)position.Y
				},
				PointerOptions = InjectedInputPointerOptions.FirstButton
					| InjectedInputPointerOptions.PointerUp
			}
		};

	private void Inject(IEnumerable<InjectedInputTouchInfo> infos)
		=> _injector.InjectTouchInput(infos);

	private void Inject(params InjectedInputTouchInfo[] infos)
		=> _injector.InjectTouchInput(infos);
}

