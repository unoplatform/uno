using System;
using System.Collections.Generic;
using System.Net;

namespace Uno.UI.RemoteControl.Server.Telemetry
{
	/// <summary>
	/// Provides connection-specific context information for scoped services.
	/// This service is registered as scoped and contains metadata about the current WebSocket connection.
	/// </summary>
	public sealed class ConnectionContext
	{
		/// <summary>
		/// Gets the unique identifier for this connection.
		/// </summary>
		public Guid ConnectionId { get; } = Guid.NewGuid();

		/// <summary>
		/// Gets or sets the remote IP address of the client.
		/// </summary>
		public IPAddress? RemoteIpAddress { get; set; }

		/// <summary>
		/// Gets or sets the timestamp when the connection was established.
		/// </summary>
		public DateTimeOffset ConnectedAt { get; set; } = DateTimeOffset.UtcNow;

		/// <summary>
		/// Gets or sets the user agent string from the WebSocket request headers.
		/// </summary>
		public string? UserAgent { get; set; }

		/// <summary>
		/// Gets or sets additional connection metadata.
		/// </summary>
		public IReadOnlyDictionary<string, string> Metadata => _metadata.AsReadOnly();
		private readonly Dictionary<string, string> _metadata = new();

		/// <summary>
		/// Adds metadata to this connection context.
		/// </summary>
		/// <param name="key">The metadata key</param>
		/// <param name="value">The metadata value</param>
		public void AddMetadata(string key, string value)
		{
			_metadata[key] = value;
		}

		/// <summary>
		/// Gets a string representation of the connection context for logging.
		/// </summary>
		/// <returns>A string describing the connection</returns>
		public override string ToString()
		{
			return $"Connection {ConnectionId:N} from {RemoteIpAddress} at {ConnectedAt:yyyy-MM-dd HH:mm:ss} UTC";
		}
	}
}
