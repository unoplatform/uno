using Windows.Foundation;
using Windows.Networking.Connectivity;

namespace Windows.Networking
{
	/// <summary>
	/// Provides data for a hostname or an IP address.
	/// </summary>
	public partial class HostName : IStringable
	{
		internal HostName()
		{
		}

		/// <summary>
		/// Gets the canonical name for the HostName object.
		/// </summary>
		public string? CanonicalName { get; internal set; }

		/// <summary>
		/// Gets the display name for the HostName object.
		/// </summary>
		public string? DisplayName { get; internal set; }

		/// <summary>
		/// Gets the IPInformation object for a local IP address assigned to a HostName object.
		/// </summary>
		public IPInformation? IPInformation { get; internal set; }

		/// <summary>
		/// Gets the original string used to construct the HostName object.
		/// </summary>
		public string? RawName { get; internal set; }

		/// <summary>
		/// Gets the HostNameType of the HostName object.
		/// </summary>
		public HostNameType Type { get; internal set; }
	}
}
