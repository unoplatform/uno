using Windows.Foundation;

namespace Windows.ApplicationModel.DataTransfer
{
	public partial class ShareUIOptions
	{
		public ShareUIOptions()
		{		
		}

#if __IOS__

		public ShareUITheme Theme { get; set; }

		public Rect? SelectionRect { get; set; }
#endif
	}
}
