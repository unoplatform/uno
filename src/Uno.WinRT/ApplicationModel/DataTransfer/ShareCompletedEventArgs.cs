namespace Windows.ApplicationModel.DataTransfer
{
	public partial class ShareCompletedEventArgs
	{
		internal ShareCompletedEventArgs()
		{
		}

		public ShareTargetInfo ShareTarget { get; } = new ShareTargetInfo();
	}
}
