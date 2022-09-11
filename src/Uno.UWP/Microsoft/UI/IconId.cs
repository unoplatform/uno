using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI
{
	public struct IconId : IEquatable<IconId>
	{
		public ulong Value;

		public IconId(ulong _Value)
		{
			Value = _Value;
		}

		public static bool operator ==(IconId x, IconId y)
			=> x.Value == y.Value;

		public static bool operator !=(IconId x, IconId y)
			=> !(x == y);

		public bool Equals(IconId other)
			=> this == other;

		public override bool Equals(object obj)
		{
			if (obj is IconId)
			{
				IconId y = (IconId)obj;
				return this == y;
			}
			return false;
		}

		public override int GetHashCode()
			=> Value.GetHashCode();
	}
}
