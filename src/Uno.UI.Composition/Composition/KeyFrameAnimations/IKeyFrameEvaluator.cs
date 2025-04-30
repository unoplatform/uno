using System;

namespace Microsoft.UI.Composition;

internal interface IKeyFrameEvaluator
{
	(object Value, bool ShouldStop) Evaluate();
	object Evaluate(float progress);
	void Pause();
	void Resume();

	float Progress { get; }

	/// <summary>
	/// The time remaining until the animation completes.
	/// </summary>
	TimeSpan Remaining { get; }
}
