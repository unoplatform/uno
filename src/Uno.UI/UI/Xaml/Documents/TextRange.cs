using System;

namespace Microsoft.UI.Xaml.Documents
{
	public partial struct TextRange : IEquatable<TextRange>
	{
		public int StartIndex;
		public int Length;

		public bool Equals(TextRange other) => StartIndex == other.StartIndex && Length == other.Length;

		public override bool Equals(object obj) => obj is TextRange other && Equals(other);

		public override int GetHashCode() => HashCode.Combine(StartIndex, Length);

		public static bool operator ==(TextRange left, TextRange right) => left.Equals(right);

		public static bool operator !=(TextRange left, TextRange right) => !left.Equals(right);
	}
}
