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
		public Guid Id { get; } = Guid.NewGuid();

		/// <summary>
		/// Gets or sets the type of this session (Root or Connection).
		/// </summary>
		public TelemetrySessionType SessionType { get; set; } = TelemetrySessionType.Root;

		/// <summary>
		/// Gets or sets the connection identifier for connection-type sessions.
		/// </summary>
		public Guid? ConnectionId { get; set; }

		/// <summary>
		/// Gets or sets the timestamp when this session was created.
		/// </summary>
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		/// <summary>
		/// Gets or sets the parent session for hierarchical relationships.
		/// </summary>
		public TelemetrySession? ParentSession { get; set; }

		/// <summary>
		/// Gets the collection of child sessions.
		/// </summary>
		public IReadOnlyList<TelemetrySession> ChildSessions => _childSessions.AsReadOnly();
		private readonly List<TelemetrySession> _childSessions = [];

		/// <summary>
		/// Gets or sets metadata about the connection (platform, client type, etc.).
		/// </summary>
		public IReadOnlyDictionary<string, string> Metadata => _metadata.AsReadOnly();
		private readonly Dictionary<string, string> _metadata = [];

		/// <summary>
		/// Creates a child session for a connection.
		/// </summary>
		/// <param name="connectionId">The unique identifier for the connection</param>
		/// <param name="metadata">Optional metadata for the connection</param>
		/// <returns>A new child telemetry session</returns>
		public TelemetrySession CreateChildSession(Guid connectionId, IDictionary<string, string>? metadata = null)
		{
			var childSession = new TelemetrySession
			{
				SessionType = TelemetrySessionType.Connection,
				ConnectionId = connectionId,
				ParentSession = this,
				CreatedAt = DateTime.UtcNow
			};

			if (metadata != null)
			{
				foreach (var kvp in metadata)
				{
					childSession._metadata[kvp.Key] = kvp.Value;
				}
			}

			_childSessions.Add(childSession);
			return childSession;
		}

		/// <summary>
		/// Adds metadata to this session.
		/// </summary>
		/// <param name="key">The metadata key</param>
		/// <param name="value">The metadata value</param>
		public void AddMetadata(string key, string value)
		{
			_metadata[key] = value;
		}

		/// <summary>
		/// Removes a child session.
		/// </summary>
		/// <param name="childSession">The child session to remove</param>
		/// <returns>True if the session was removed, false otherwise</returns>
		public bool RemoveChildSession(TelemetrySession childSession)
		{
			return _childSessions.Remove(childSession);
		}
	}
}
