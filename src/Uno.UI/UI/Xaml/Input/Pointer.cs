using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Uno;

using PointerIdentifier = Windows.Devices.Input.PointerIdentifier; // internal type (should be in Uno namespace)
using Microsoft.UI.Input;

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

			UniqueId = new PointerIdentifier((global::Windows.Devices.Input.PointerDeviceType)type, id);
		}

		internal Pointer(PointerIdentifier uniqueId, bool isInContact, bool isInRange)
		{
			PointerId = uniqueId.Id;
			PointerDeviceType = (PointerDeviceType)uniqueId.Type;
			IsInContact = isInContact;
			IsInRange = isInRange;

			UniqueId = uniqueId;
		}

#if __WASM__
		internal Pointer(uint id, PointerDeviceType type)
		{
			PointerId = id;
			PointerDeviceType = type;

			UniqueId = new PointerIdentifier((global::Windows.Devices.Input.PointerDeviceType)type, id);
		}
#endif

		/// <summary>
		/// A unique identifier which contains <see cref="PointerDeviceType"/> and <see cref="PointerId"/>.
		/// </summary>
		internal global::Windows.Devices.Input.PointerIdentifier UniqueId { get; }

		public uint PointerId { get; }

		public PointerDeviceType PointerDeviceType { get; }

		public bool IsInContact { get; }

		public bool IsInRange { get; }

		public override string ToString()
			=> UniqueId.ToString();

		public override int GetHashCode()
			=> UniqueId.GetHashCode();

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
	}
}
