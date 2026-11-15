#nullable enable

using System;

namespace Uno.UI.Composition;

/// <summary>What a per-frame driver subscribes to: a window's frames, or the compositor's for drivers of no window.</summary>
internal interface IFrameTickSource
{
	/// <summary>Raised once per frame, before layout and before the record, with the frame's timestamp.</summary>
	event EventHandler<long>? FrameStarting;

	/// <summary>Estimated interval between presented frames, for drivers that need a nominal step.</summary>
	long FrameIntervalInTicks { get; }
}
