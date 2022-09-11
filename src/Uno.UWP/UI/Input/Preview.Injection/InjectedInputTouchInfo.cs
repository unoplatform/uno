#nullable enable

using System;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;

namespace Windows.UI.Input.Preview.Injection;

public partial class InjectedInputTouchInfo 
{
	public InjectedInputTouchParameters TouchParameters { get; set; }

	public double Pressure { get; set; }

	public int Orientation { get; set; }

	public InjectedInputPointerInfo PointerInfo { get; set; }

	public InjectedInputRectangle Contact { get; set; }

	internal PointerEventArgs ToEventArgs(InjectedInputState state)
	{
		var point = PointerInfo.ToPointerPoint(state);

		if (TouchParameters.HasFlag(InjectedInputTouchParameters.Pressure))
		{
			point.Properties.Pressure = (float)Pressure;
		}

		if (TouchParameters.HasFlag(InjectedInputTouchParameters.Orientation))
		{
			point.Properties.Orientation = Orientation;
		}

		if (TouchParameters.HasFlag(InjectedInputTouchParameters.Contact))
		{
			point.Properties.ContactRect = new Rect(Contact.Left, Contact.Top, Contact.Right - Contact.Left, Contact.Bottom - Contact.Top);
		}

		return new PointerEventArgs(point, VirtualKeyModifiers.None);
	}
}
