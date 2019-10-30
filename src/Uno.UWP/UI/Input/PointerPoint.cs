using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Windows.Devices.Input;
using Windows.Foundation;

namespace Windows.UI.Input
{
	public partial class PointerPoint
	{
		internal PointerPoint(
			uint frameId,
			ulong timestamp,
			PointerDevice device,
			uint pointerId,
			Point rawPosition,
			Point position,
			bool isInContact,
			PointerPointProperties properties)
		{
			FrameId = frameId;
			Timestamp = timestamp;
			PointerDevice = device;
			PointerId = pointerId;
			RawPosition = rawPosition;
			Position = position;
			IsInContact = isInContact;
			Properties = properties;
		}

		public uint FrameId { get; }

		public ulong Timestamp { get; }

		public PointerDevice PointerDevice { get; }

		public uint PointerId { get; }

		public Point RawPosition { get; }

		public Point Position { get; }

		public bool IsInContact { get; }

		public PointerPointProperties Properties { get; }
	}
}
