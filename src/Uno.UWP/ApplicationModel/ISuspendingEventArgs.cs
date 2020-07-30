namespace Windows.ApplicationModel
{
	public partial interface ISuspendingEventArgs
	{
		SuspendingOperation SuspendingOperation { get; }
	}
}
