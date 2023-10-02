using System;

namespace Windows.Foundation
{
	/// <summary>
	/// A class containing static helper methods for working with the Guid type.
	/// </summary>
	public static partial class GuidHelper
	{
		/// <summary>
		/// Gets an empty, zeroed Guid.
		/// </summary>
		public static Guid Empty => Guid.Empty;

		/// <summary>
		/// Creates a new, unique Guid.
		/// </summary>
		/// <returns>New Guid.</returns>
		public static Guid CreateNewGuid() => Guid.NewGuid();

		/// <summary>
		/// Checks whether given Guids are equal.
		/// </summary>
		/// <param name="target">First Guid.</param>
		/// <param name="value">Second Guid.</param>
		/// <returns>A value indicating whether the Guids are equal.</returns>
		public static bool Equals(
#if HAS_UNO_WINUI
			in Guid target, in Guid value
#else
			ref Guid target, ref Guid value
#endif
			) => target.Equals(value);
	}
}
