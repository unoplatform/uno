using System;
using System.Collections.Generic;
using System.Text;
using Uno;
using Windows.Devices.Input;

namespace Windows.UI.Xaml.Input
{
	public partial class Pointer : IEquatable<Pointer>
	{
		public Pointer(uint id, PointerDeviceType type, bool isInContact, bool isInRange)
		{
			PointerId = id;
			PointerDeviceType = type;
			IsInContact = isInContact;
			IsInRange = isInRange;
		}


#if __MACOS__
		internal Pointer(AppKit.NSEvent uiEvent)
		{
			switch (uiEvent.Type)
			{
				case AppKit.NSEventType.DirectTouch:
					PointerDeviceType = PointerDeviceType.Touch;
					break;

				case AppKit.NSEventType.MouseMoved:
					PointerDeviceType = PointerDeviceType.Mouse;
					break;
			}
		}
#elif __WASM__
		internal Pointer(uint id, PointerDeviceType type)
		{
			PointerId = id;
			PointerDeviceType = type;
		}
#endif

		public uint PointerId { get; }

		public PointerDeviceType PointerDeviceType { get;}

		public bool IsInContact { get; }

		public bool IsInRange { get; }

		public override string ToString()
		{
			return $"{PointerDeviceType}/{PointerId}";
		}

		public bool Equals(Pointer other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return PointerDeviceType == other.PointerDeviceType && PointerId == other.PointerId;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((Pointer) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return ((int) PointerDeviceType * 397) ^ (int) PointerId;
			}
		}
	}
}
