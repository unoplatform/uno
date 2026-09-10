using System;

namespace Microsoft.UI.Composition;

internal interface IKeyFrameEvaluator
{
	(object Value, bool ShouldStop) Evaluate();
	object Evaluate(float progress);
	void Pause();
	void Resume();
	void Seek(float progress);

	float PlaybackRate { get; set; }

	/// <summary>
	/// Re-anchors playback to <paramref name="progress"/> without pausing. Clock-driven playback (at
	/// the current rate) continues from the new position.
	/// </summary>
	void SeekTo(float progress);

	float Progress { get; }

	bool IsPaused { get; }

	/// <summary>
	/// The time remaining until the animation completes.
	/// </summary>
	TimeSpan Remaining { get; }
}
