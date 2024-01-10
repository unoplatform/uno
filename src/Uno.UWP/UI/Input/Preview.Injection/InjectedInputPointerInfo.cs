#nullable enable

using System;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;

namespace Windows.UI.Input.Preview.Injection;

public partial struct InjectedInputPointerInfo
{
	public uint PointerId;

	public InjectedInputPointerOptions PointerOptions;

	public InjectedInputPoint PixelLocation;

	public uint TimeOffsetInMilliseconds;

	public ulong PerformanceCount;

	internal PointerPoint ToPointerPoint(InjectedInputState state)
	{
		var isNew = PointerOptions.HasFlag(InjectedInputPointerOptions.New);
		var properties = isNew
			? new PointerPointProperties()
			: new PointerPointProperties(state.Properties);

		properties.IsPrimary = PointerOptions.HasFlag(InjectedInputPointerOptions.Primary);
		properties.IsInRange = PointerOptions.HasFlag(InjectedInputPointerOptions.InRange);
		// IsLeftButtonPressed = // stateful, cf. below,
		// IsMiddleButtonPressed = // Mouse only
		// IsRightButtonPressed = // stateful, cf. below,
		// IsHorizontalMouseWheel = // Mouse only
		// IsXButton1Pressed =
		// IsXButton2Pressed =
		// IsBarrelButtonPressed = // Pen only
		// IsEraser = // Pen only
		// Pressure = // Touch only
		// Orientation = // Touch only
		// ContactRect = new Rect(Contact.Left, Contact.Top, Contact.Right - Contact.Left, Contact.Bottom - Contact.Top),
		properties.TouchConfidence = PointerOptions.HasFlag(InjectedInputPointerOptions.Confidence);
		properties.IsCanceled = PointerOptions.HasFlag(InjectedInputPointerOptions.Canceled);

		var update = default(PointerUpdateKind);
		if (PointerOptions.HasFlag(InjectedInputPointerOptions.FirstButton))
		{
			if (PointerOptions.HasFlag(InjectedInputPointerOptions.PointerDown))
			{
				properties.IsLeftButtonPressed = true;
				update |= PointerUpdateKind.LeftButtonPressed;
			}
			else if (PointerOptions.HasFlag(InjectedInputPointerOptions.PointerUp))
			{
				properties.IsLeftButtonPressed = false;
				update |= PointerUpdateKind.LeftButtonReleased;
			}
		}

		if (PointerOptions.HasFlag(InjectedInputPointerOptions.SecondButton))
		{
			if (PointerOptions.HasFlag(InjectedInputPointerOptions.PointerDown))
			{
				properties.IsRightButtonPressed = true;
				update |= PointerUpdateKind.RightButtonPressed;
			}
			else if (PointerOptions.HasFlag(InjectedInputPointerOptions.PointerUp))
			{
				properties.IsRightButtonPressed = false;
				update |= PointerUpdateKind.RightButtonReleased;
			}
		}

		properties.PointerUpdateKind = update;
		if (state.Type is PointerDeviceType.Pen)
		{
			properties.IsBarrelButtonPressed = properties.IsRightButtonPressed;
		}

		var location = new Point(PixelLocation.PositionX, PixelLocation.PositionY);
		var point = new PointerPoint(
			state.FrameId + (uint)PerformanceCount,
			state.Timestamp + (ulong)(TimeOffsetInMilliseconds * TimeSpan.TicksPerMillisecond),
			PointerDevice.For(state.Type),
			isNew ? PointerId : state.PointerId,
			location,
			location,
			PointerOptions.HasFlag(InjectedInputPointerOptions.InContact),
			properties);

		return point;
	}

	public bool Equals(InjectedInputPointerInfo other) =>
		PointerId == other.PointerId && PointerOptions.Equals(other.PointerOptions) &&
		PixelLocation.Equals(other.PixelLocation) &&
		TimeOffsetInMilliseconds == other.TimeOffsetInMilliseconds && PerformanceCount == other.PerformanceCount;

	public override bool Equals(object? obj) => obj is InjectedInputPointerInfo other && Equals(other);

	public override int GetHashCode()
	{
		var hashCode = -2014432303;
		hashCode = hashCode * -1521134295 + PointerId.GetHashCode();
		hashCode = hashCode * -1521134295 + PointerOptions.GetHashCode();
		hashCode = hashCode * -1521134295 + PixelLocation.GetHashCode();
		hashCode = hashCode * -1521134295 + TimeOffsetInMilliseconds.GetHashCode();
		hashCode = hashCode * -1521134295 + PerformanceCount.GetHashCode();
		return hashCode;
	}

	public static bool operator ==(InjectedInputPointerInfo left, InjectedInputPointerInfo right) => left.Equals(right);

	public static bool operator !=(InjectedInputPointerInfo left, InjectedInputPointerInfo right) => !(left == right);
}
