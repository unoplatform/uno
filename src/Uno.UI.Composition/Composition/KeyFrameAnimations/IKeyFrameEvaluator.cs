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

	float Progress { get; }

	/// <summary>
	/// The time remaining until the animation completes.
	/// </summary>
	TimeSpan Remaining { get; }
}
