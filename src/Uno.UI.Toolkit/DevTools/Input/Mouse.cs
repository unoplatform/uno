using System;
using System.Collections.Generic;
using System.Text;

#nullable enable

using System.Linq;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Input.Preview.Injection;
using Microsoft.UI.Xaml.Controls;

#if !HAS_UNO
using System.Runtime.InteropServices;
#endif

#if HAS_UNO_WINUI || WINAPPSDK
#else
using PointerDeviceType = Windows.Devices.Input.PointerDeviceType;
#endif

namespace Uno.UI.Toolkit.DevTools.Input;

#if HAS_UNO
internal class Mouse : IInjectedPointer, IDisposable
{
	private readonly InputInjector _input;

	public Mouse(InputInjector input)
	{
		_input = input;
	}

	private Point Current => _input.Mouse.Position;

	public void Press(Point position)
		=> Inject(GetMoveTo(position.X, position.Y, null).Concat(new[] { GetPress() }));

	public void PressRight(Point position)
		=> Inject(GetMoveTo(position.X, position.Y, null).Concat(new[] { GetRightPress() }));

	internal void Press(Point position, VirtualKeyModifiers modifiers)
	{
		var infos = GetMoveTo(position.X, position.Y, null).Concat(new[] { GetPress() });
		Inject(infos.Select(info => (info, modifiers)));
	}

	public void Press()
		=> Inject(GetPress());

	public void Press(VirtualKeyModifiers modifiers)
		=> Inject((GetPress(), modifiers));

	public void Release(VirtualKeyModifiers modifiers)
		=> Inject((GetRelease(), modifiers));

	public void Release()
		=> Inject(GetRelease());

	public void PressRight()
		=> Inject(GetRightPress());

	public void ReleaseRight()
		=> Inject(GetRightRelease());

	public void PressMiddle()
		=> Inject(GetMiddlePress());

	public void ReleaseMiddle()
		=> Inject(GetMiddleRelease());

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
		=> Inject(GetMoveBy(deltaX, deltaY, 1));

	public void MoveTo(Point position, uint? steps = null, uint? stepOffsetInMilliseconds = null)
		=> Inject(GetMoveTo(position.X, position.Y, steps, stepOffsetInMilliseconds));

	public void WheelUp() => Wheel(ScrollContentPresenter.ScrollViewerDefaultMouseWheelDelta);
	public void WheelDown() => Wheel(-ScrollContentPresenter.ScrollViewerDefaultMouseWheelDelta);
	public void WheelRight() => Wheel(ScrollContentPresenter.ScrollViewerDefaultMouseWheelDelta, isHorizontal: true);
	public void WheelLeft() => Wheel(-ScrollContentPresenter.ScrollViewerDefaultMouseWheelDelta, isHorizontal: true);

	public void Wheel(double delta, bool isHorizontal = false, uint steps = 1)
		=> Inject(GetWheel(delta, isHorizontal, steps));

	public void RightTap(Point location)
	{
		PressRight();
		ReleaseRight();
	}

	private IEnumerable<InjectedInputMouseInfo> GetMoveTo(double x, double y, uint? steps, uint? stepOffsetInMilliseconds = null)
	{
		var x0 = Current.X;
		var y0 = Current.Y;
		var deltaX = x - x0;
		var deltaY = y - y0;

		steps ??= (uint)Math.Min(Math.Max(Math.Abs(deltaX), Math.Abs(deltaY)), 512);
		stepOffsetInMilliseconds ??= 1;

		if (steps is 0)
		{
			yield break;
		}

		// Could probably use Bresenham's algorithm if performance issues appear
		var stepX = deltaX / steps.Value;
		var stepY = deltaY / steps.Value;

		var prevPositionX = (int)Math.Round(x0);
		var prevPositionY = (int)Math.Round(y0);

		for (var i = 1; i <= steps; i++)
		{
			var newPositionX = (int)Math.Round(x0 + i * stepX);
			var newPositionY = (int)Math.Round(y0 + i * stepY);

			yield return GetMoveBy(newPositionX - prevPositionX, newPositionY - prevPositionY, stepOffsetInMilliseconds.Value);

			prevPositionX = newPositionX;
			prevPositionY = newPositionY;
		}
	}

	private void Inject(IEnumerable<InjectedInputMouseInfo> infos)
		=> _input.InjectMouseInput(infos);

	private void Inject(IEnumerable<(InjectedInputMouseInfo, VirtualKeyModifiers)> infos)
		=> _input.InjectMouseInput(infos);

	private void Inject(params InjectedInputMouseInfo[] infos)
		=> _input.InjectMouseInput(infos);

	private void Inject(params (InjectedInputMouseInfo, VirtualKeyModifiers)[] infos)
		=> _input.InjectMouseInput(infos);

	public void Dispose()
		=> ReleaseAny();

	private static InjectedInputMouseInfo GetPress()
		=> new()
		{
			TimeOffsetInMilliseconds = 1,
			MouseOptions = InjectedInputMouseOptions.LeftDown,
		};

	private static InjectedInputMouseInfo GetRightPress()
		=> new()
		{
			TimeOffsetInMilliseconds = 1,
			MouseOptions = InjectedInputMouseOptions.RightDown,
		};

	private static InjectedInputMouseInfo GetMiddlePress()
		=> new()
		{
			TimeOffsetInMilliseconds = 1,
			MouseOptions = InjectedInputMouseOptions.MiddleDown,
		};

	private static InjectedInputMouseInfo GetMoveBy(double deltaX, double deltaY, uint stepOffsetInMilliseconds)
		=> new()
		{
			DeltaX = (int)deltaX,
			DeltaY = (int)deltaY,
			TimeOffsetInMilliseconds = stepOffsetInMilliseconds,
			MouseOptions = InjectedInputMouseOptions.MoveNoCoalesce,
		};

	private static InjectedInputMouseInfo GetRelease()
		=> new()
		{
			TimeOffsetInMilliseconds = 1,
			MouseOptions = InjectedInputMouseOptions.LeftUp,
		};

	private static InjectedInputMouseInfo GetRightRelease()
		=> new()
		{
			TimeOffsetInMilliseconds = 1,
			MouseOptions = InjectedInputMouseOptions.RightUp,
		};

	private static InjectedInputMouseInfo GetMiddleRelease()
		=> new()
		{
			TimeOffsetInMilliseconds = 1,
			MouseOptions = InjectedInputMouseOptions.MiddleUp,
		};

	public static IEnumerable<InjectedInputMouseInfo> GetWheel(double delta, bool isHorizontal, uint steps = 1)
	{
		if (steps is 0)
		{
			yield break;
		}

		var stepSize = delta / steps;

		var prev = 0;

		for (var i = 1; i <= steps; i++)
		{
			var current = (int)Math.Round(i * stepSize);

			yield return isHorizontal
				? new() { TimeOffsetInMilliseconds = 1, DeltaX = current - prev, MouseOptions = InjectedInputMouseOptions.HWheel }
				: new() { TimeOffsetInMilliseconds = 1, DeltaY = current - prev, MouseOptions = InjectedInputMouseOptions.Wheel };

			prev = current;
		}
	}
}
#endif
