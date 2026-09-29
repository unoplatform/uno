
using System;

namespace Microsoft.UI.Xaml.Data
{
	public partial struct LoadMoreItemsResult : IEquatable<LoadMoreItemsResult>
	{
		public uint Count;

		public bool Equals(LoadMoreItemsResult other) => Count == other.Count;

		public override bool Equals(object obj) => obj is LoadMoreItemsResult other && Equals(other);

		public override int GetHashCode() => Count.GetHashCode();

		public static bool operator ==(LoadMoreItemsResult left, LoadMoreItemsResult right) => left.Equals(right);

		public static bool operator !=(LoadMoreItemsResult left, LoadMoreItemsResult right) => !left.Equals(right);
	}
}
