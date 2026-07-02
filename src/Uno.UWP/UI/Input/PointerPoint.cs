using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Windows.Devices.Input;
using Windows.Foundation;

namespace Microsoft.UI.Input
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

		internal PointerPoint At(Point position)
			=> new(
				FrameId,
				Timestamp,
				PointerDevice,
				PointerId,
				RawPosition,
				position: position,
				IsInContact,
				Properties);

		internal PointerPoint At(Point rawPosition, Point position)
			=> new(
				FrameId,
				Timestamp,
				PointerDevice,
				PointerId,
				rawPosition: rawPosition,
				position: position,
				IsInContact,
				Properties);

		internal PointerIdentifier Pointer => new(PointerDevice.PointerDeviceType, PointerId);

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
