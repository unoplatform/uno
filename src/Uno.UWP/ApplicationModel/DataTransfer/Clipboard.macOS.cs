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
			if (content is null)
			{
				throw new ArgumentNullException(nameof(content));
			}

			// See notes in Clipboard.Android.cs on async code usage here
			CoreDispatcher.Main.RunAsync(
				CoreDispatcherPriority.High,
				async () =>
				{
					var data = content?.GetView();

					var declaredTypes = new List<string>();
					var pasteboard = NSPasteboard.GeneralPasteboard;

					// Note that order is somewhat important here.
					//
					// According to the docs:
					//    "types should be ordered according to the preference of the source application,
					//     with the most preferred type coming first"
					// https://developer.apple.com/documentation/appkit/nspasteboard/1533561-declaretypes?language=objc
					//
					// This means we want to process certain types like HTML/RTF before general plain text
					// as they are more specific.
					// Types are also declared before setting

					// Declare types
					if (data?.Contains(StandardDataFormats.Html) ?? false)
					{
						declaredTypes.Add(NSPasteboard.NSPasteboardTypeHTML);
					}

					if (data?.Contains(StandardDataFormats.Rtf) ?? false)
					{
						declaredTypes.Add(NSPasteboard.NSPasteboardTypeRTF);
					}

					if (data?.Contains(StandardDataFormats.Text) ?? false)
					{
						declaredTypes.Add(NSPasteboard.NSPasteboardTypeString);
					}

					pasteboard.DeclareTypes(declaredTypes.ToArray(), null);

					// Set content
					if (data?.Contains(StandardDataFormats.Html) ?? false)
					{
						var html = await data.GetHtmlFormatAsync();
						pasteboard.SetStringForType(html ?? string.Empty, NSPasteboard.NSPasteboardTypeHTML);
					}

					if (data?.Contains(StandardDataFormats.Rtf) ?? false)
					{
						var rtf = await data.GetRtfAsync();
						pasteboard.SetStringForType(rtf ?? string.Empty, NSPasteboard.NSPasteboardTypeRTF);
					}

					if (data?.Contains(StandardDataFormats.Text) ?? false)
					{
						var text = await data.GetTextAsync();
						pasteboard.SetStringForType(text ?? string.Empty, NSPasteboard.NSPasteboardTypeString);
					}
				});

			return;
		}

		public static DataPackageView GetContent()
		{
			var dataPackage = new DataPackage();

			var pasteboard = NSPasteboard.GeneralPasteboard;

			var clipHtml = pasteboard.GetStringForType(NSPasteboard.NSPasteboardTypeHTML);
			if (clipHtml != null)
			{
				dataPackage.SetHtmlFormat(clipHtml);
			}

			var clipRtf = pasteboard.GetStringForType(NSPasteboard.NSPasteboardTypeRTF);
			if (clipRtf != null)
			{
				dataPackage.SetRtf(clipRtf);
			}

			var clipText = pasteboard.GetStringForType(NSPasteboard.NSPasteboardTypeString);
			if (clipText != null)
			{
				dataPackage.SetText(clipText);
			}

			return dataPackage.GetView();
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
