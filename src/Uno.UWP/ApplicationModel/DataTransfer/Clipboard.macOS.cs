#if __MACOS__
#nullable enable

using System;
using System.Collections.Generic;
using AppKit;
using Foundation;
using Windows.UI.Core;

namespace Windows.ApplicationModel.DataTransfer
{
	public static partial class Clipboard
	{
		private static NSTimer? _timer;
		private static nint _lastPasteboardChangeCount;

		public static void SetContent(DataPackage content)
		{
			DataPackage.SetToNativeClipboard(content);
		}

		public static DataPackageView GetContent()
		{
			return DataPackage.GetFromNativeClipboard();
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
