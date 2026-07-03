using System;

namespace Microsoft.UI.Composition;

internal interface IKeyFrameEvaluator
{
	(object Value, bool ShouldStop) Evaluate();
	object Evaluate(float progress);
	void Pause();
	void Resume();

	/// <summary>
	/// Scales how fast wall-clock time advances the animation (1 = real time). A negative rate plays
	/// the animation backwards. Only affects the clock-driven <see cref="Evaluate()"/> path — explicit
	/// <see cref="Evaluate(float)"/> scrubbing is unaffected.
	/// </summary>
	void SetPlaybackRate(float playbackRate);

	/// <summary>
	/// Re-anchors playback to <paramref name="progress"/> without pausing. Clock-driven playback (at
	/// the current rate) continues from the new position.
	/// </summary>
	void SeekTo(float progress);

	float Progress { get; }

	/// <summary>
	/// The time remaining until the animation completes.
	/// </summary>
	TimeSpan Remaining { get; }
}
