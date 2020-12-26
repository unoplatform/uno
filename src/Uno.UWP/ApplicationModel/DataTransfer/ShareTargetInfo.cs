#nullable enable

namespace Windows.ApplicationModel.DataTransfer
{
	public partial class ShareTargetInfo
	{
		internal ShareTargetInfo()
		{
		}

		public string AppUserModelId { get; } = "";

		public ShareProvider? ShareProvider { get; } = null;
	}
}
