using System;
using System.Linq;

namespace Microsoft.UI.Xaml.Controls
{
	public partial struct FlowLayoutAnchorInfo : IEquatable<FlowLayoutAnchorInfo>
	{
		internal FlowLayoutAnchorInfo(in int index, in double offset)
		{
			Index = index;
			Offset = offset;
		}

		// NOTE: Equality implementation should be modified if a new field/property is added.

		public int Index;

		public double Offset;

		#region Equality Members
		public override bool Equals(object obj) => obj is FlowLayoutAnchorInfo info && Equals(info);
		public bool Equals(FlowLayoutAnchorInfo other) => Index == other.Index && Offset == other.Offset;

		public override int GetHashCode()
		{
			var hashCode = 173447405;
			hashCode = hashCode * -1521134295 + Index.GetHashCode();
			hashCode = hashCode * -1521134295 + Offset.GetHashCode();
			return hashCode;
		}

		public static bool operator ==(FlowLayoutAnchorInfo left, FlowLayoutAnchorInfo right) => left.Equals(right);
		public static bool operator !=(FlowLayoutAnchorInfo left, FlowLayoutAnchorInfo right) => !left.Equals(right);
		#endregion
	}
}
