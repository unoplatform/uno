#nullable enable

using System;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;


namespace Windows.UI.Input.Preview.Injection;

using PointerPoint = global::Microsoft.UI.Input.PointerPoint;
using PointerPointProperties = global::Microsoft.UI.Input.PointerPointProperties;
using PointerUpdateKind = global::Microsoft.UI.Input.PointerUpdateKind;

public partial class InjectedInputMouseInfo
{
	public uint TimeOffsetInMilliseconds { get; set; }

	public InjectedInputMouseOptions MouseOptions { get; set; }

	public uint MouseData { get; set; }

	public int DeltaY { get; set; }

	public int DeltaX { get; set; }

	internal PointerEventArgs ToEventArgs(InjectedInputState state, VirtualKeyModifiers modifiers, Size bounds)
	{
		var update = default(PointerUpdateKind);
		var position = state.Position;
		var properties = new PointerPointProperties(state.Properties)
		{
			IsPrimary = false,
			IsInRange = true // always true for mouse
		};

		if (MouseOptions.HasFlag(InjectedInputMouseOptions.LeftDown))
		{
			properties.IsLeftButtonPressed = true;
			update |= PointerUpdateKind.LeftButtonPressed;
		}
		else if (MouseOptions.HasFlag(InjectedInputMouseOptions.LeftUp))
		{
			properties.IsLeftButtonPressed = false;
			update |= PointerUpdateKind.LeftButtonReleased;
		}

		if (MouseOptions.HasFlag(InjectedInputMouseOptions.MiddleDown))
		{
			properties.IsMiddleButtonPressed = true;
			update |= PointerUpdateKind.MiddleButtonPressed;
		}
		else if (MouseOptions.HasFlag(InjectedInputMouseOptions.MiddleUp))
		{
			properties.IsMiddleButtonPressed = false;
			update |= PointerUpdateKind.MiddleButtonReleased;
		}

		if (MouseOptions.HasFlag(InjectedInputMouseOptions.RightDown))
		{
			properties.IsRightButtonPressed = true;
			update |= PointerUpdateKind.RightButtonPressed;
		}
		else if (MouseOptions.HasFlag(InjectedInputMouseOptions.RightUp))
		{
			properties.IsRightButtonPressed = false;
			update |= PointerUpdateKind.RightButtonReleased;
		}

		if (MouseOptions.HasFlag(InjectedInputMouseOptions.XDown))
		{
			properties.IsXButton1Pressed = true;
			update |= PointerUpdateKind.XButton1Pressed;
		}
		else if (MouseOptions.HasFlag(InjectedInputMouseOptions.XUp))
		{
			properties.IsXButton1Pressed = false;
			update |= PointerUpdateKind.XButton1Released;
		}

		if (MouseOptions.HasFlag(InjectedInputMouseOptions.Wheel))
		{
			properties.MouseWheelDelta = DeltaY;
			properties.IsHorizontalMouseWheel = false;
		}
		else if (MouseOptions.HasFlag(InjectedInputMouseOptions.HWheel))
		{
			properties.MouseWheelDelta = DeltaX;
			properties.IsHorizontalMouseWheel = true;
		}
		else if (MouseOptions.HasFlag(InjectedInputMouseOptions.Move) || MouseOptions.HasFlag(InjectedInputMouseOptions.MoveNoCoalesce))
		{
			properties.MouseWheelDelta = default;
			properties.IsHorizontalMouseWheel = false;

			if (MouseOptions.HasFlag(InjectedInputMouseOptions.Absolute))
			{
				// DeltaX/DeltaY are normalized coordinates in the 0-65535 range, mirroring Win32 SendInput's
				// MOUSEEVENTF_ABSOLUTE convention: (0,0) is the top-left corner of the display surface and
				// (65535,65535) is the bottom-right corner. Uno has no portable notion of monitors/virtual
				// desktop wired into pointer injection, so - unlike real Windows, where VirtualDesk switches
				// between the primary monitor and the full virtual desktop - the normalized space always maps
				// onto the current root visual's bounds.
				if (bounds.Width > 0 && bounds.Height > 0)
				{
					position = new Point(DeltaX / 65535.0 * bounds.Width, DeltaY / 65535.0 * bounds.Height);
				}
			}
			else
			{
				position.X += DeltaX;
				position.Y += DeltaY;
			}
		}
		else
		{
			properties.MouseWheelDelta = default;
			properties.IsHorizontalMouseWheel = false;
		}

		properties.PointerUpdateKind = update;

		var timestampInMicroseconds = state.Timestamp + TimeOffsetInMilliseconds * 1000;
		var point = new PointerPoint(
			state.FrameId + TimeOffsetInMilliseconds,
			timestampInMicroseconds,
			PointerDevice.For(global::Windows.Devices.Input.PointerDeviceType.Mouse),
			uint.MaxValue - 42, // Try to avoid conflict with the real mouse pointer
			position,
			position,
			isInContact: properties.HasPressedButton,
			properties);

		return new PointerEventArgs(point, modifiers);
	}
}
