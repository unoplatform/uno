namespace Windows.UI.Composition.Interactions;

public partial class InteractionTrackerIdleStateEnteredArgs
{
	internal InteractionTrackerIdleStateEnteredArgs(int requestId, bool isFromBinding)
	{
		RequestId = requestId;
		IsFromBinding = isFromBinding;
	}

	public int RequestId { get; }

	public bool IsFromBinding { get; }
}
