namespace Uno.UI.RemoteControl.Host.HotReload;

/// <summary>
/// The names of the hot-reload outcome events, sent by <c>ServerHotReloadProcessor.Notify</c> for the outcomes the IDE
/// reports and by <see cref="HotReloadOutcomeTelemetry"/> for those of the dev-server's own workspace.
/// </summary>
internal static class HotReloadOutcomeEvents
{
	public const string Completed = "notify-completed";
	public const string NoChanges = "notify-no-changes";
	public const string Failed = "notify-failed";
	public const string RudeEdit = "notify-rude-edit";

	/// <summary>An operation of the dev-server's own workspace that got no result in time. Never sent by the IDE.</summary>
	public const string Timeout = "notify-timeout";
}
