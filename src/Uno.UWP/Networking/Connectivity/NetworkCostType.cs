namespace Windows.Networking.Connectivity
{
	/// <summary>
	/// Defines the network cost types.
	/// </summary>
	public enum NetworkCostType
	{
		/// <summary>
		/// Cost information is not available.
		/// </summary>
		Unknown,

		/// <summary>
		/// The connection is unlimited and has unrestricted usage charges and capacity constraints.
		/// </summary>
		Unrestricted,

		/// <summary>
		/// The use of this connection is unrestricted up to a specific limit.
		/// </summary>
		Fixed,

		/// <summary>
		/// The connection is costed on a per-byte basis.
		/// </summary>
		Variable,
	}
}
