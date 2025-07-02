using System;

namespace Uno.UI.RemoteControl.Server.Telemetry
{
	public record TelemetrySession
	{
		public Guid Id { get; } = Guid.NewGuid();
	}
}
