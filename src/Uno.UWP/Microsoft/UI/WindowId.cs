#nullable disable

using System;

namespace Microsoft.UI
{
	public struct WindowId : IEquatable<WindowId>
	{
		public ulong Value;

		public WindowId(ulong _Value)
			=> Value = _Value;

		public static bool operator ==(WindowId x, WindowId y)
			=> x.Value == y.Value;

		public static bool operator !=(WindowId x, WindowId y)
			=> !(x == y);

		public bool Equals(WindowId other)
			=> this == other;

		public override bool Equals(object obj)
		{
			if (obj is WindowId)
			{
				WindowId y = (WindowId)obj;
				return this == y;
			}
			return false;
		}

		public override int GetHashCode()
			=> Value.GetHashCode();
	}
}
