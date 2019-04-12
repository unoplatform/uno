#if __IOS__
using UIKit;

namespace Windows.ApplicationModel.DataTransfer
{
	public static partial class Clipboard
	{
		public static void SetContent(DataPackage content)
		{
			//Setting to null doesn't reset the clipboard like for Android
			UIPasteboard.General.String = content?.Text ?? string.Empty;			
		}
	}
}
#endif
