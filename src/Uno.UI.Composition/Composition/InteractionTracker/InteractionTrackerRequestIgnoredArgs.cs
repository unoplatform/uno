namespace Windows.UI.Composition.Interactions;

public partial class InteractionTrackerRequestIgnoredArgs
{
	internal InteractionTrackerRequestIgnoredArgs(int requestId)
		=> RequestId = requestId;

	public int RequestId { get; }
}
