namespace Windows.UI.Composition.Interactions;

public partial class InteractionTrackerInteractingStateEnteredArgs
{
	internal InteractionTrackerInteractingStateEnteredArgs(int requestId, bool isFromBinding)
	{
		RequestId = requestId;
		IsFromBinding = isFromBinding;
	}

	public int RequestId { get; }

	public bool IsFromBinding { get; }
}
