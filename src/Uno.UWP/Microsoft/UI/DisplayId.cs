#nullable disable

using System;

namespace Microsoft.UI
{
	public struct DisplayId : IEquatable<DisplayId>
	{
		public ulong Value;

		public DisplayId(ulong _Value)
			=> Value = _Value;

		public static bool operator ==(DisplayId x, DisplayId y)
			=> x.Value == y.Value;

		public static bool operator !=(DisplayId x, DisplayId y)
			=> !(x == y);

		public bool Equals(DisplayId other)
			=> this == other;

		public override bool Equals(object obj)
		{
			if (obj is DisplayId)
			{
				DisplayId y = (DisplayId)obj;
				return this == y;
			}
			return false;
		}

		public override int GetHashCode()
			=> Value.GetHashCode();
	}
}
