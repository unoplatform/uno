using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Uno;


#if HAS_UNO_WINUI
using Microsoft.UI.Input;
#else
using Windows.Devices.Input;
using Windows.UI.Input;
#endif

namespace Microsoft.UI.Xaml.Input
{
	public sealed partial class Pointer : IEquatable<Pointer>
	{
		private static int _unknownId;
		internal static long CreateUniqueIdForUnknownPointer()
			=> (long)1 << 63 | (long)Interlocked.Increment(ref _unknownId);

		public Pointer(uint id, PointerDeviceType type, bool isInContact, bool isInRange)
		{
			PointerId = id;
			PointerDeviceType = type;
			IsInContact = isInContact;
			IsInRange = isInRange;

			UniqueId = new Windows.Devices.Input.PointerIdentifier((Windows.Devices.Input.PointerDeviceType)type, id);
		}

#if __WASM__
		internal Pointer(uint id, PointerDeviceType type)
		{
			PointerId = id;
			PointerDeviceType = type;

			UniqueId = new Windows.Devices.Input.PointerIdentifier((Windows.Devices.Input.PointerDeviceType)type, id);
		}
#endif

		/// <summary>
		/// A unique identifier which contains <see cref="PointerDeviceType"/> and <see cref="PointerId"/>.
		/// </summary>
		internal Windows.Devices.Input.PointerIdentifier UniqueId { get; }

		public uint PointerId { get; }

		public PointerDeviceType PointerDeviceType { get; }

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

			return UniqueId == other.UniqueId;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (!(obj is Pointer other)) return false;

			return UniqueId == other.UniqueId;
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return ((int)PointerDeviceType * 397) ^ (int)PointerId;
			}
		}
	}
}
