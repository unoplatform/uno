using System;
using System.Collections.Generic;

namespace Uno.UI.RemoteControl.Server.Telemetry
{
	/// <summary>
	/// Defines the type of telemetry session.
	/// </summary>
	public enum TelemetrySessionType
	{
		/// <summary>
		/// Root session for the DevServer itself.
		/// </summary>
		Root,

		/// <summary>
		/// Session for a specific client connection.
		/// </summary>
		Connection
	}

	/// <summary>
	/// Represents a telemetry session with metadata and hierarchical relationships.
	/// </summary>
	public record TelemetrySession
	{
		/// <summary>
		/// Gets the unique identifier for this session.
		/// </summary>
		public string Id { get; } = Guid.NewGuid().ToString("N");

		/// <summary>
		/// Gets or sets the type of this session (Root or Connection).
		/// </summary>
		public TelemetrySessionType SessionType { get; init; } = TelemetrySessionType.Root;

		/// <summary>
		/// Gets or sets the connection identifier for connection-type sessions.
		/// </summary>
		public required string ConnectionId { get; init; }

		public string? SolutionPath { get; init; }
	}
}
