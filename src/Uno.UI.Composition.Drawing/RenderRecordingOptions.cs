#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Switches a backend reads while recording. Public because a backend stands on the public seam and has no
/// access to the framework's internals.
/// </summary>
public static class RenderRecordingOptions
{
	/// <summary>
	/// Set while a <c>CompositionTarget.Rendering</c> subscriber exists. A backend that can expose its frame
	/// object then keeps a managed one alive on each record, so it can be handed out as
	/// <see cref="IRenderRecord.FrameData"/>; off otherwise, because that costs a managed wrapper per recording
	/// and nothing else reads it.
	/// </summary>
	public static bool CaptureFrameData { get; set; }
}
