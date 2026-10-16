namespace Microsoft.UI.Composition.Interactions;

public partial class InteractionTrackerCustomAnimationStateEnteredArgs
{
	internal InteractionTrackerCustomAnimationStateEnteredArgs(int requestId, bool isFromBinding)
	{
		RequestId = requestId;
		IsFromBinding = isFromBinding;
	}

	public int RequestId { get; }

	public bool IsFromBinding { get; }
}
