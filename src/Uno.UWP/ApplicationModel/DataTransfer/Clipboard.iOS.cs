#if __IOS__
using UIKit;

namespace Windows.ApplicationModel.DataTransfer
{
	public static partial class Clipboard
	{
		public static void SetContent(DataPackage content)
		{
			UIPasteboard.General.String = content.Text;
		}
	}
}
#endif