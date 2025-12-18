using System;
using System.Collections.Generic;

#nullable enable

using System.Linq;
using Windows.Foundation;
using Windows.UI.Input.Preview.Injection;

#if HAS_UNO_WINUI || WINAPPSDK
#else
using PointerDeviceType = Windows.Devices.Input.PointerDeviceType;
#endif

namespace Uno.UI.Toolkit.DevTools.Input;

#if HAS_UNO
internal class Pen : IInjectedPointer, IDisposable
{
	private const uint _defaultMoveSteps = 10;
	private const uint _defaultStepOffsetInMilliseconds = 1;

	private readonly InputInjector _injector;
	private readonly uint _id;

	private Point? _currentPosition;

	public Pen(InputInjector injector, uint id = 1)
	{
		_injector = injector;
		_id = id;

		_injector.InitializePenInjection(InjectedInputVisualizationMode.Default);
	}

	public void Press(Point position)
	{
		if (_currentPosition is null)
		{
			Inject(GetPress(_id, position));
			_currentPosition = position;
		}
	}

	void IInjectedPointer.MoveTo(Point position, uint? steps, uint? stepOffsetInMilliseconds) =>
		MoveTo(position, steps ?? _defaultMoveSteps, stepOffsetInMilliseconds ?? _defaultStepOffsetInMilliseconds);

	public void MoveTo(Point position, uint steps = _defaultMoveSteps, uint stepOffsetInMilliseconds = _defaultStepOffsetInMilliseconds)
	{
		if (_currentPosition is { } current)
		{
			Inject(GetMove(_id, current, position, steps, stepOffsetInMilliseconds));
			_currentPosition = position;
		}
	}

	void IInjectedPointer.MoveBy(double deltaX, double deltaY) => MoveBy(deltaX, deltaY);

	public void MoveBy(double x = 0, double y = 0, uint steps = _defaultMoveSteps, uint stepOffsetInMilliseconds = _defaultStepOffsetInMilliseconds)
	{
		if (_currentPosition is { } current)
		{
			MoveTo(current.Offset(x, y), steps, stepOffsetInMilliseconds);
		}
	}

	public void Release(Point position)
	{
		Inject(GetRelease(_id, position));
		_currentPosition = null;
	}

	public void Release()
	{
		if (_currentPosition is { } current)
		{
			Inject(GetRelease(_id, current));
			_currentPosition = null;
		}
	}

	public void Tap(Point position)
	{
		Press(position);
		Release();
	}

	public void Dispose()
	{
		Release();
		_injector.UninitializePenInjection();
	}

	public static InjectedInputPenInfo GetPress(uint id, Point position)
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
					| InjectedInputPointerOptions.InRange
			},
			PenParameters = InjectedInputPenParameters.Pressure,
			Pressure = 0.5
		};

	public static IEnumerable<InjectedInputPenInfo> GetMove(uint id, Point fromPosition, Point toPosition, uint steps = _defaultMoveSteps, uint stepOffsetInMilliseconds = _defaultStepOffsetInMilliseconds)
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
					PointerId = id,
					TimeOffsetInMilliseconds = stepOffsetInMilliseconds,
					PixelLocation = At(fromPosition.X + step * stepX, fromPosition.Y + step * stepY),
					PointerOptions = InjectedInputPointerOptions.Update
						| InjectedInputPointerOptions.FirstButton
						| InjectedInputPointerOptions.InContact
						| InjectedInputPointerOptions.InRange
				},
				PenParameters = InjectedInputPenParameters.Pressure,
				Pressure = 0.5
			};
		}
	}

	public static InjectedInputPenInfo GetRelease(uint id, Point position)
		=> new()
		{
			PointerInfo = new()
			{
				PointerId = id,
				PixelLocation = At(position),
				PointerOptions = InjectedInputPointerOptions.FirstButton
					| InjectedInputPointerOptions.PointerUp
					| InjectedInputPointerOptions.InRange
			}
		};

	private void Inject(IEnumerable<InjectedInputPenInfo> infos)
	{
		foreach (var info in infos)
		{
			_injector.InjectPenInput(info);
		}
	}

	private void Inject(InjectedInputPenInfo info)
		=> _injector.InjectPenInput(info);

	private static InjectedInputPoint At(Point position)
		=> At(position.X, position.Y);

	private static InjectedInputPoint At(double x, double y)
		=> new() { PositionX = (int)x, PositionY = (int)y };
}
#endif
