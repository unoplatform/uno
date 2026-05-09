using System;
using System.Collections.Generic;

#nullable enable

using System.Linq;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Input.Preview.Injection;

#if !HAS_UNO
using System.Runtime.InteropServices;
#endif

#if HAS_UNO_WINUI || WINAPPSDK
#else
using PointerDeviceType = Windows.Devices.Input.PointerDeviceType;
#endif

namespace Uno.UI.Toolkit.DevTools.Input;

internal partial class Mouse : IInjectedPointer, IDisposable
{
	private const int DefaultMouseWheelDelta = 120;

	private readonly InputInjector _input;

#if !HAS_UNO
	private Point _currentPosition;
	private bool _leftPressed;
	private bool _rightPressed;
	private bool _middlePressed;
	private bool _xButton1Pressed;
#endif

	public Mouse(InputInjector input)
	{
		_input = input;
#if !HAS_UNO
		GetCursorPos(out var pt);
		_currentPosition = new Point(pt.X, pt.Y);
#endif
	}

	private Point Current =>
#if HAS_UNO
		_input.Mouse.Position;
#else
		_currentPosition;
#endif

	public void Press(Point position)
	{
#if HAS_UNO
		Inject(GetMoveTo(position.X, position.Y, null).Concat(new[] { GetPress() }));
#else
		Inject(GetMoveTo(position.X, position.Y, null));
		Inject(GetPress());
#endif
	}

	public void PressRight(Point position)
	{
#if HAS_UNO
		Inject(GetMoveTo(position.X, position.Y, null).Concat(new[] { GetRightPress() }));
#else
		Inject(GetMoveTo(position.X, position.Y, null));
		Inject(GetRightPress());
#endif
	}

#if HAS_UNO
	internal void Press(Point position, VirtualKeyModifiers modifiers)
	{
		var infos = GetMoveTo(position.X, position.Y, null).Concat(new[] { GetPress() });
		Inject(infos.Select(info => (info, modifiers)));
	}
#endif

	public void Press()
		=> Inject(GetPress());

#if HAS_UNO
	public void Press(VirtualKeyModifiers modifiers)
		=> Inject((GetPress(), modifiers));

	public void Release(VirtualKeyModifiers modifiers)
		=> Inject((GetRelease(), modifiers));
#endif

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

#if HAS_UNO
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
#else
		if (_leftPressed)
		{
			options |= InjectedInputMouseOptions.LeftUp;
		}

		if (_middlePressed)
		{
			options |= InjectedInputMouseOptions.MiddleUp;
		}

		if (_rightPressed)
		{
			options |= InjectedInputMouseOptions.RightUp;
		}

		if (_xButton1Pressed)
		{
			options |= InjectedInputMouseOptions.XUp;
		}
#endif

		if (options != default)
		{
			Inject(new InjectedInputMouseInfo
			{
				TimeOffsetInMilliseconds = 1,
				MouseOptions = options
			});
#if !HAS_UNO
			_leftPressed = false;
			_rightPressed = false;
			_middlePressed = false;
			_xButton1Pressed = false;
#endif
		}
	}

	public void MoveBy(double deltaX, double deltaY)
		=> Inject(GetMoveBy(deltaX, deltaY, 1));

	public void MoveTo(Point position, uint? steps = null, uint? stepOffsetInMilliseconds = null)
		=> Inject(GetMoveTo(position.X, position.Y, steps, stepOffsetInMilliseconds));

	public void WheelUp() => Wheel(DefaultMouseWheelDelta);
	public void WheelDown() => Wheel(-DefaultMouseWheelDelta);
	public void WheelRight() => Wheel(DefaultMouseWheelDelta, isHorizontal: true);
	public void WheelLeft() => Wheel(-DefaultMouseWheelDelta, isHorizontal: true);

	public void Wheel(double delta, bool isHorizontal = false, uint steps = 1)
		=> Inject(GetWheel(delta, isHorizontal, steps));

	public void RightTap(Point location)
	{
		PressRight();
		ReleaseRight();
	}

	private IEnumerable<InjectedInputMouseInfo> GetMoveTo(double x, double y, uint? steps, uint? stepOffsetInMilliseconds = null)
	{
#if HAS_UNO
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
#else
		// On WinAppSDK, use absolute coordinates with screen-space conversion.
		// The Absolute flag requires coordinates normalized to 0-65535 range.
		var normalizedTarget = ToNormalizedScreenCoordinates(x, y);
		var normalizedFrom = _currentPosition.X == 0 && _currentPosition.Y == 0
			? normalizedTarget
			: ToNormalizedScreenCoordinates(_currentPosition.X, _currentPosition.Y);

		steps ??= (uint)Math.Min(Math.Max(Math.Abs(normalizedTarget.X - normalizedFrom.X), Math.Abs(normalizedTarget.Y - normalizedFrom.Y)) / 100, 512);
		stepOffsetInMilliseconds ??= 1;

		if (steps is 0)
		{
			steps = 1;
		}

		var stepX = (normalizedTarget.X - normalizedFrom.X) / steps.Value;
		var stepY = (normalizedTarget.Y - normalizedFrom.Y) / steps.Value;

		for (var i = 1; i <= steps; i++)
		{
			var posX = (int)Math.Round(normalizedFrom.X + i * stepX);
			var posY = (int)Math.Round(normalizedFrom.Y + i * stepY);

			yield return new InjectedInputMouseInfo
			{
				DeltaX = posX,
				DeltaY = posY,
				TimeOffsetInMilliseconds = stepOffsetInMilliseconds.Value,
				MouseOptions = InjectedInputMouseOptions.Move | InjectedInputMouseOptions.Absolute,
			};
		}

		_currentPosition = new Point(x, y);
#endif
	}

	private void Inject(IEnumerable<InjectedInputMouseInfo> infos)
		=> _input.InjectMouseInput(infos);

#if HAS_UNO
	private void Inject(IEnumerable<(InjectedInputMouseInfo, VirtualKeyModifiers)> infos)
		=> _input.InjectMouseInput(infos);
#endif

	private void Inject(params InjectedInputMouseInfo[] infos)
		=> _input.InjectMouseInput(infos);

#if HAS_UNO
	private void Inject(params (InjectedInputMouseInfo, VirtualKeyModifiers)[] infos)
		=> _input.InjectMouseInput(infos);
#endif

	public void Dispose()
		=> ReleaseAny();

	private InjectedInputMouseInfo GetPress()
	{
#if !HAS_UNO
		_leftPressed = true;
#endif
		return new()
		{
			TimeOffsetInMilliseconds = 1,
			MouseOptions = InjectedInputMouseOptions.LeftDown,
		};
	}

	private InjectedInputMouseInfo GetRightPress()
	{
#if !HAS_UNO
		_rightPressed = true;
#endif
		return new()
		{
			TimeOffsetInMilliseconds = 1,
			MouseOptions = InjectedInputMouseOptions.RightDown,
		};
	}

	private InjectedInputMouseInfo GetMiddlePress()
	{
#if !HAS_UNO
		_middlePressed = true;
#endif
		return new()
		{
			TimeOffsetInMilliseconds = 1,
			MouseOptions = InjectedInputMouseOptions.MiddleDown,
		};
	}

	private static InjectedInputMouseInfo GetMoveBy(double deltaX, double deltaY, uint stepOffsetInMilliseconds)
		=> new()
		{
			DeltaX = (int)deltaX,
			DeltaY = (int)deltaY,
			TimeOffsetInMilliseconds = stepOffsetInMilliseconds,
			MouseOptions = InjectedInputMouseOptions.MoveNoCoalesce,
		};

	private InjectedInputMouseInfo GetRelease()
	{
#if !HAS_UNO
		_leftPressed = false;
#endif
		return new()
		{
			TimeOffsetInMilliseconds = 1,
			MouseOptions = InjectedInputMouseOptions.LeftUp,
		};
	}

	private InjectedInputMouseInfo GetRightRelease()
	{
#if !HAS_UNO
		_rightPressed = false;
#endif
		return new()
		{
			TimeOffsetInMilliseconds = 1,
			MouseOptions = InjectedInputMouseOptions.RightUp,
		};
	}

	private InjectedInputMouseInfo GetMiddleRelease()
	{
#if !HAS_UNO
		_middlePressed = false;
#endif
		return new()
		{
			TimeOffsetInMilliseconds = 1,
			MouseOptions = InjectedInputMouseOptions.MiddleUp,
		};
	}

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

#if !HAS_UNO
	internal static Microsoft.UI.Xaml.Window? TestServices_WindowHelper_CurrentTestWindow { get; set; }

	private static Point ToNormalizedScreenCoordinates(double x, double y)
	{
		RECT rect = new();
		GetWindowRect(WinRT.Interop.WindowNative.GetWindowHandle(TestServices_WindowHelper_CurrentTestWindow), ref rect);
		var scale = TestServices_WindowHelper_CurrentTestWindow?.Content.XamlRoot.RasterizationScale ?? 1;

		var screenX = (rect.Left + x) * scale;
		var screenY = (rect.Top + y) * scale;

		var screenWidth = GetSystemMetrics(SM_CXSCREEN);
		var screenHeight = GetSystemMetrics(SM_CYSCREEN);

		return new Point(
			screenX / screenWidth * 65535.0,
			screenY / screenHeight * 65535.0);
	}

	private const int SM_CXSCREEN = 0;
	private const int SM_CYSCREEN = 1;

	[LibraryImport("user32.dll")]
	private static partial int GetSystemMetrics(int nIndex);

	[LibraryImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool GetCursorPos(out POINT lpPoint);

	[LibraryImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);

	[StructLayout(LayoutKind.Sequential)]
	private struct POINT
	{
		public int X;
		public int Y;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct RECT
	{
		public int Left;
		public int Top;
		public int Right;
		public int Bottom;
	}
#endif
}
