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
		public string ConnectionId { get; } = Guid.NewGuid().ToString("N");

		/// <summary>
		/// Gets or sets the timestamp when the connection was established.
		/// </summary>
		public DateTimeOffset ConnectedAt { get; set; } = DateTimeOffset.UtcNow;

		public string SolutionPath { get; set; } = string.Empty;

		/// <summary>
		/// Gets a string representation of the connection context for logging.
		/// </summary>
		/// <returns>A string describing the connection</returns>
		public override string ToString() => $"Connection {ConnectionId} at {ConnectedAt:yyyy-MM-dd HH:mm:ss} UTC";
	}
}
