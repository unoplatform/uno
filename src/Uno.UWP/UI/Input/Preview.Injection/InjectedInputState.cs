#nullable enable

using System;
using System.Linq;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Core;

namespace Windows.UI.Input.Preview.Injection;

internal class InjectedInputState
{
	public InjectedInputState(PointerDeviceType type)
	{
		Type = type;
		StartNewSequence();
	}

	public PointerDeviceType Type { get; }

	public uint PointerId { get; set; }

	public uint FrameId { get; set; }

	public ulong Timestamp { get; set; }

	public Point Position { get; set; }

	public PointerPointProperties Properties { get; set; } = new();

	public void StartNewSequence()
	{
		Timestamp = (ulong)DateTime.Now.Ticks;
		FrameId = (uint)(Timestamp / TimeSpan.TicksPerMillisecond);
	}

	public void Update(PointerEventArgs args)
	{
		PointerId = args.CurrentPoint.PointerId;
		FrameId = args.CurrentPoint.FrameId;
		Timestamp = args.CurrentPoint.Timestamp;
		Position = args.CurrentPoint.Position;
		Properties = args.CurrentPoint.Properties;
	}
}
