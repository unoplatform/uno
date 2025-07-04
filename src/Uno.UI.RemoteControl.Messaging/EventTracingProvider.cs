using System.Diagnostics.Tracing;

namespace Uno.UI.RemoteControl.Messaging;

[EventSource(Name = "Uno.HotReload.TracingProvider")]
internal class EventTracingProvider : EventSource
{
	public static EventTracingProvider Instance { get; } = new EventTracingProvider();

	[Event(1, Level = EventLevel.Informational, Message = "Force hot reload start, requestCorrelationId={0}")]
	public void OnForceHotReloadStart(long requestCorrelationId) => WriteEvent(1, requestCorrelationId);

	[Event(2, Level = EventLevel.Informational, Message = "Force hot reload stop, requestCorrelationId={0}, success={1}")]
	public void OnForceHotReloadStop(long requestCorrelationId, bool success) => WriteEvent(2, requestCorrelationId, success);

	[Event(3, Level = EventLevel.Informational, Message = "Update file start, requestCorrelationId={0}, requestFileFullName={1}, requestForceSaveOnDisk={2}")]
	public void OnUpdateFileStart(long requestCorrelationId, string requestFileFullName, bool requestForceSaveOnDisk)
		=> WriteEvent(3, requestCorrelationId, requestFileFullName, requestForceSaveOnDisk);

	[Event(4, Level = EventLevel.Informational, Message = "Update file stop, requestCorrelationId={0}, success={1}")]
	public void OnUpdateFileStop(long requestCorrelationId, bool success) => WriteEvent(4, requestCorrelationId, success);

	[Event(5, Level = EventLevel.Informational, Message = "Hot reload event, source={0}, event={1}")]
	public void OnHotReloadEvent(string source, string @event) => WriteEvent(5, source, @event);
}
