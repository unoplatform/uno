using Windows.Foundation;

namespace Windows.ApplicationModel.DataTransfer
{
	public partial class ShareUIOptions
	{
		public ShareUIOptions()
		{
		}

		public ShareUITheme Theme { get; set; }

		public Rect? SelectionRect { get; set; }
	}
}
