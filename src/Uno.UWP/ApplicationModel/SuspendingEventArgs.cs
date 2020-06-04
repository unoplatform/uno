namespace Windows.ApplicationModel
{
	public sealed partial class SuspendingEventArgs : ISuspendingEventArgs
	{
		internal SuspendingEventArgs(SuspendingOperation operation)
		{
			SuspendingOperation = operation;
		}

		public SuspendingOperation SuspendingOperation { get; }
	}
}
