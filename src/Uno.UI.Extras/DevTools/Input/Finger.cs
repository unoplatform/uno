using System;
using System.Collections.Generic;

#nullable enable

using System.Linq;
using Windows.Foundation;
using Windows.UI.Input.Preview.Injection;

#if !HAS_UNO
using System.Runtime.InteropServices;
#endif

#if HAS_UNO_WINUI || WINAPPSDK
#else
using PointerDeviceType = Windows.Devices.Input.PointerDeviceType;
#endif

namespace Uno.UI.Toolkit.DevTools.Input;

internal partial class Finger : IInjectedPointer, IDisposable
{
	private const uint _defaultMoveSteps = 10;
	private const uint _defaultStepOffsetInMilliseconds = 1;

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

	public static IEnumerable<InjectedInputTouchInfo> GetMove(uint id, Point fromPosition, Point toPosition, uint steps = _defaultMoveSteps, uint stepOffsetInMilliseconds = _defaultStepOffsetInMilliseconds)
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
				}
			};
		}
	}

	public static InjectedInputTouchInfo GetRelease(uint id, Point position)
		=> new()
		{
			PointerInfo = new()
			{
				PointerId = id,
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

#if !HAS_UNO
	[LibraryImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);
	[StructLayout(LayoutKind.Sequential)]
	private struct RECT
	{
		public int Left;
		public int Top;
		public int Right;
		public int Bottom;
	}

#endif

#if !HAS_UNO
	internal static Microsoft.UI.Xaml.Window? TestServices_WindowHelper_CurrentTestWindow { get; set; }
#endif

	private static InjectedInputPoint At(double x, double y)
#if HAS_UNO
		=> new() { PositionX = (int)x, PositionY = (int)y };
#else
	{
		RECT rect = new();
		GetWindowRect(WinRT.Interop.WindowNative.GetWindowHandle(TestServices_WindowHelper_CurrentTestWindow), ref rect);
		var scale = TestServices_WindowHelper_CurrentTestWindow?.Content.XamlRoot.RasterizationScale ?? 1;

		return new()
		{
			PositionX = (int)((rect.Left + x) * scale),
			PositionY = (int)((rect.Top + y) * scale),
		};
	}
#endif
}
