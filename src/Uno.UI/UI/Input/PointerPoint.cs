using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Windows.Devices.Input;
using Windows.Foundation;

#if HAS_UNO_WINUI && IS_UNO_UI_PROJECT
namespace Microsoft.UI.Input
#else
namespace Windows.UI.Input
#endif
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
			PointerDeviceType = (PointerDeviceType)PointerDevice.PointerDeviceType;
			PointerId = pointerId;
			RawPosition = rawPosition;
			Position = position;
			IsInContact = isInContact;
			Properties = properties;
		}

#if HAS_UNO_WINUI && IS_UNO_UI_PROJECT
		public PointerPoint(Windows.UI.Input.PointerPoint point)
		{
			FrameId = point.FrameId;
			Timestamp = point.Timestamp;
			PointerDevice = point.PointerDevice;
			PointerId = point.PointerId;
			RawPosition = point.RawPosition;
			Position = point.Position;
			IsInContact = point.IsInContact;
			PointerDeviceType = (PointerDeviceType)point.PointerDevice.PointerDeviceType;

			Properties = new PointerPointProperties(point.Properties);
		}

		public static explicit operator Windows.UI.Input.PointerPoint(Microsoft.UI.Input.PointerPoint muxPointerPoint)
		{
			return new Windows.UI.Input.PointerPoint(
				muxPointerPoint.FrameId,
				muxPointerPoint.Timestamp,
				muxPointerPoint.PointerDevice,
				muxPointerPoint.PointerId,
				muxPointerPoint.RawPosition,
				muxPointerPoint.Position,
				muxPointerPoint.IsInContact,
				(Windows.UI.Input.PointerPointProperties)muxPointerPoint.Properties);
		}
#endif

		internal PointerPoint At(Point position)
			=> new PointerPoint(
				FrameId,
				Timestamp,
				PointerDevice,
				PointerId,
				RawPosition,
				position: position,
				IsInContact,
				Properties);

		internal PointerIdentifier Pointer => new PointerIdentifier(PointerDevice.PointerDeviceType, PointerId);

		public uint FrameId { get; }

		public ulong Timestamp { get; }

		public PointerDevice PointerDevice { get; }

		public PointerDeviceType PointerDeviceType { get; }

		public uint PointerId { get; }

		public Point RawPosition { get; }

		public Point Position { get; }

		public bool IsInContact { get; }

		public PointerPointProperties Properties { get; }

		/// <inheritdoc />
		public override string ToString()
			=> $"[{PointerDevice.PointerDeviceType}-{PointerId}] @{Position.ToDebugString()} (raw: {RawPosition.ToDebugString()} | ts: {Timestamp} | props: {Properties} | inContact: {IsInContact})";
	}
}
