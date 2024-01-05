using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Text
{
	public partial struct FontWeight
	{
		public FontWeight(ushort weight)
		{
			Weight = weight;
		}

		public ushort Weight;

		public override int GetHashCode() => Weight.GetHashCode();

		public override bool Equals(object? obj)
		{
			if (obj is FontWeight other)
			{
				return other.Weight == Weight;
			}

			return false;
		}


		public static bool operator ==(FontWeight left, FontWeight right)
		{
			return left.Weight == right.Weight;
		}

		public static bool operator !=(FontWeight left, FontWeight right)
		{
			return left.Weight != right.Weight;
		}
	}
}
