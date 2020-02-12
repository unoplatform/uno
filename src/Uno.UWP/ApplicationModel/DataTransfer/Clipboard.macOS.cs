#if __MACOS__
using AppKit;

namespace Windows.ApplicationModel.DataTransfer
{
	public static partial class Clipboard
	{
		public static void SetContent(DataPackage content)
		{
			var pasteboard = NSPasteboard.GeneralPasteboard;
			pasteboard.DeclareTypes(new string[] { NSPasteboard.NSPasteboardTypeString }, null);
			pasteboard.SetStringForType(content?.Text ?? string.Empty, NSPasteboard.NSPasteboardTypeString);
		}

		public static DataPackageView GetContent()
		{
			var dataPackageView = new DataPackageView();
			var pasteboard = NSPasteboard.GeneralPasteboard;
			var clipboardText = pasteboard.GetStringForType(NSPasteboard.NSPasteboardTypeString);
			dataPackageView.SetText(clipboardText);
			return dataPackageView;
		}
	}
}
#endif
