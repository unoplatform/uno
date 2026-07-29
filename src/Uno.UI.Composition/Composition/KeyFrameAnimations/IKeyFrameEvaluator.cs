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

	float Progress { get; }

	bool IsPaused { get; }

	/// <summary>
	/// The time remaining until the animation completes.
	/// </summary>
	TimeSpan Remaining { get; }
}
