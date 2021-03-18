#if __ANDROID__
namespace Windows.Networking.Connectivity
{
	/// <summary>
	/// Provides access to property values that indicate the current cost of a network connection.
	/// </summary>
	public partial class ConnectionCost
	{
		internal ConnectionCost(NetworkCostType costType)
		{
			NetworkCostType = costType;
		}

		/// <summary>
		/// Gets a value that indicates the current network cost for a connection.
		/// </summary>
		public NetworkCostType NetworkCostType { get; }
	}
}
#endif
