#nullable enable

using System;
using System.Diagnostics;
using System.Linq;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Core;

namespace Windows.UI.Input.Preview.Injection;

internal class InjectedInputState
{
	private static long _initialTimestamp = Stopwatch.GetTimestamp();

	public InjectedInputState(PointerDeviceType type)
	{
		Type = type;
		StartNewSequence(true);
	}

	public PointerDeviceType Type { get; }

	public uint PointerId { get; set; }

	public uint FrameId { get; set; }

	public ulong Timestamp { get; set; }

	public Point Position { get; set; }

	public PointerPointProperties Properties { get; set; } = new();

	public void StartNewSequence(bool initial = false)
	{
		if (initial)
		{
			Timestamp = (ulong)Stopwatch.GetElapsedTime(_initialTimestamp).TotalMicroseconds;
		}
		else
		{
			Timestamp = Timestamp + 1000; // Continue from the previous timestamp, but move forward in time by 1ms
		}

		FrameId = (uint)(Timestamp / 1000);
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
