using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Windows.Devices.Input;
using Windows.Foundation;

#if IS_UNO_UI_PROJECT
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

#nullable enable
		private global::Windows.UI.Input.PointerPoint? _wuxPoint;

		public PointerPoint(global::Windows.UI.Input.PointerPoint point)
		{
			_wuxPoint = point;
			point._muxPoint = this;

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

		// Historically, we had explicit conversion only.
		// In the work for InteractionTracker, we needed an implicit conversion to avoid a breaking change.
		// The compiler doesn't allow to define both. (https://learn.microsoft.com/en-us/dotnet/csharp/misc/cs0557)
		// And changing the explicit conversion operator to implicit conversion operator is a binary breaking change.
		// We manually add this method to avoid this binary breaking change.
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static global::Windows.UI.Input.PointerPoint op_Explicit(Microsoft.UI.Input.PointerPoint muxPointerPoint)
			=> muxPointerPoint;

		public static implicit operator global::Windows.UI.Input.PointerPoint(Microsoft.UI.Input.PointerPoint muxPointerPoint)
		{
			if (muxPointerPoint._wuxPoint is global::Windows.UI.Input.PointerPoint wuxPoint)
			{
				return wuxPoint;
			}

			wuxPoint = new global::Windows.UI.Input.PointerPoint(
				muxPointerPoint.FrameId,
				muxPointerPoint.Timestamp,
				muxPointerPoint.PointerDevice,
				muxPointerPoint.PointerId,
				muxPointerPoint.RawPosition,
				muxPointerPoint.Position,
				muxPointerPoint.IsInContact,
				(global::Windows.UI.Input.PointerPointProperties)muxPointerPoint.Properties);

			wuxPoint._muxPoint = muxPointerPoint;
			muxPointerPoint._wuxPoint = wuxPoint;

			return wuxPoint;
		}

		public static implicit operator global::Microsoft.UI.Input.PointerPoint(global::Windows.UI.Input.PointerPoint wuxPointerPoint)
			=> wuxPointerPoint._muxPoint as global::Microsoft.UI.Input.PointerPoint ?? new global::Microsoft.UI.Input.PointerPoint(wuxPointerPoint);
#nullable restore

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
