#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>Host-internal hook for drawing factories that own GPU resources.</summary>
internal interface IGpuDrawingFactory
{
	/// <summary>
	/// Marks the backend's GPU context as lost, so a later dispose releases its wrappers without issuing
	/// calls into a context that can no longer be made current.
	/// </summary>
	void AbandonContext();
}
