namespace Windows.Networking.Connectivity
{
	/// <summary>
	/// Defines the level of connectivity currently available.
	/// </summary>
	public enum NetworkConnectivityLevel
	{
		/// <summary>
		/// No connectivity.
		/// </summary>
		None,

		/// <summary>
		/// Local network access only.
		/// </summary>
		LocalAccess,

		/// <summary>
		/// Limited internet access.		
		/// </summary>
		/// <remarks>
		/// This value indicates captive portal connectivity, where local access to a web portal is provided,
		/// but access to the Internet requires that specific credentials are provided via the portal.
		/// This level of connectivity is generally encountered when using connections hosted in public
		/// locations(for example, coffee shops and book stores).
		/// </remarks>
		ConstrainedInternetAccess,

		/// <summary>
		/// Local and Internet access.
		/// </summary>
		InternetAccess,
	}
}
