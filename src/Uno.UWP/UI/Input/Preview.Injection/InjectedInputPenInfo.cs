#nullable enable

using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;

namespace Windows.UI.Input.Preview.Injection;

public partial class InjectedInputPenInfo
{
	public InjectedInputPenParameters PenParameters { get; set; }

	public InjectedInputPenButtons PenButtons { get; set; }

	public double Pressure { get; set; } = 0.5;

	public double Rotation { get; set; }

	public int TiltX { get; set; }

	public int TiltY { get; set; }

	public InjectedInputPointerInfo PointerInfo { get; set; }

	internal PointerEventArgs ToEventArgs(InjectedInputState state)
	{
		var point = PointerInfo.ToPointerPoint(state);

		if (PenParameters.HasFlag(InjectedInputPenParameters.Pressure))
		{
			point.Properties.Pressure = (float)Pressure;
		}

		if (PenParameters.HasFlag(InjectedInputPenParameters.TiltX))
		{
			point.Properties.XTilt = TiltX;
		}

		if (PenParameters.HasFlag(InjectedInputPenParameters.TiltY))
		{
			point.Properties.YTilt = TiltY;
		}

		// Handle pen barrel button
		if (PenButtons.HasFlag(InjectedInputPenButtons.Barrel))
		{
			point.Properties.IsBarrelButtonPressed = true;
		}

		// Handle pen eraser button
		if (PenButtons.HasFlag(InjectedInputPenButtons.Eraser))
		{
			point.Properties.IsEraser = true;
		}

		return new PointerEventArgs(point, VirtualKeyModifiers.None);
	}
}
