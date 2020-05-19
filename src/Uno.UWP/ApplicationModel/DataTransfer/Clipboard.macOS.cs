#if __MACOS__
using System;
using System.Threading.Tasks;
using AppKit;
using Foundation;

namespace Windows.ApplicationModel.DataTransfer
{
	public static partial class Clipboard
	{
		private static NSTimer _timer;
		private static nint _lastPasteboardChangeCount;

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
			if (clipboardText != null)
			{
				dataPackageView.SetFormatTask(StandardDataFormats.Text, Task.FromResult(clipboardText));
			}
			return dataPackageView;
		}

		public static void Clear()
		{
			var pasteboard = NSPasteboard.GeneralPasteboard;
			pasteboard.ClearContents();
		}

		private static void StartContentChanged()
		{
			_lastPasteboardChangeCount = NSPasteboard.GeneralPasteboard.ChangeCount;
			_timer = NSTimer.CreateRepeatingScheduledTimer(1, CheckPasteboardChange);
			NSRunLoop.Main.AddTimer(_timer, NSRunLoopMode.Common);
		}

		private static void CheckPasteboardChange(NSTimer obj)
		{
			var currentChangeCount = NSPasteboard.GeneralPasteboard.ChangeCount;
			if (_lastPasteboardChangeCount != currentChangeCount)
			{
				OnContentChanged();
				_lastPasteboardChangeCount = currentChangeCount;
			}
		}

		private static void StopContentChanged() => _timer?.Invalidate();
	}
}
#endif
