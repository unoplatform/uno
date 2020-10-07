#if __MACOS__
#nullable enable

using System;
using System.Collections.Generic;
using AppKit;
using Foundation;
using Windows.UI.Core;

namespace Windows.ApplicationModel.DataTransfer
{
	public partial class DataPackage
	{
		/* UWP has the StandardDataFormats class with properties to define certain types.
		 * These types are mapped to macOS/iOS as shown in the below table.
		 * The latest convention in macOS/iOS is to use Uniform Type Identifiers. 
		 *   https://developer.apple.com/documentation/uniformtypeidentifiers
		 * However, this is still in beta and only available in macOS 11+.
		 * NSPasteboard types are therefore used in code until such time that UTI framework
		 * is available everywhere.
		 * 
		 *  UWP                    Uniform           NSPasteboard types                      NSPasteboard types     
		 *                         Type Identifiers  (currently used)                        (legacy, deprecated)
		 *  ---------------------  ----------------  --------------------------------------  -------------------------
		 *  ApplicationLink        UTType.URL        NSPasteboard.NSPasteboardTypeUrl        NSPasteboard.NSUrlType
		 *  Bitmap                 UTType.Image      NSPasteboard.NSPasteboardTypeTIFF       NSPasteboard.NSTiffType
		 *                                           NSPasteboard.NSPasteboardTypePNG        -
		 *  Html                   UTType.HTML       NSPasteboard.NSPasteboardTypeHTML       NSPasteboard.NSHtmlType
		 *  Rtf                    UTType.RTF        NSPasteboard.NSPasteboardTypeRTF        NSPasteboard.NSRtfType
		 *                                           NSPasteboard.NSPasteboardTypeRTFD (*1)  NSPasteboard.NSRtfdType
		 *  StorageItems           UTType.FileURL    NSPasteboard.NSPasteboardTypeFileUrl    -
		 *  Text                   UTType.PlainText  NSPasteboard.NSPasteboardTypeString     NSPasteboard.NSStringType
		 *  Uri                    UTType.URL        NSPasteboard.NSPasteboardTypeUrl        NSPasteboard.NSUrlType                                                
		 *  UserActivityJsonArray  -                 -                                       -
		 *  WebLink                UTType.URL        NSPasteboard.NSPasteboardTypeUrl        NSPasteboard.NSUrlType 
		 *  
		 *  *1 : macOS 10.6 and later
		 *  
		 */

		// An Uno-internal abstraction is used to simplify native drag/drop & clipboard integration

		/// <summary>
		/// Sets the contents of the <see cref="DataPackage"/> to the native clipboard.
		/// Any conversion between supported standard types is done automatically.
		/// </summary>
		/// <param name="content">The contents to set to the native cipboard.</param>
		internal static void SetToNativeClipboard(DataPackage content)
		{
			SetToNative(content, NSPasteboard.GeneralPasteboard);
		}

		/// <summary>
		/// Gets the contents of the native clipboard.
		/// </summary>
		/// <returns>A new <see cref="DataPackageView"/> representing the native clipboard contents.</returns>
		internal static DataPackageView GetFromNativeClipboard()
		{
			return GetFromNative(NSPasteboard.GeneralPasteboard);
		}

		/// <summary>
		/// Sets the contents of the <see cref="DataPackage"/> to the native drag and drop manager.
		/// </summary>
		/// <param name="content">The contents to set to the native drag and drop manager.</param>
		internal static void SetForNativeDragDrop(DataPackage content)
		{
			return;
		}

		/// <summary>
		/// Gets the contents of the native drag and drop manager.
		/// </summary>
		/// <returns>A new <see cref="DataPackageView"/> representing the native drag and drop manager contents.</returns>
		internal static DataPackageView GetFromNativeDragDrop()
		{
			// More worked needed, signature may change
			return (new DataPackage()).GetView();
		}

		private static void SetToNative(DataPackage content, NSPasteboard pasteboard)
		{
			if (content is null)
			{
				throw new ArgumentNullException(nameof(content));
			}

			if (pasteboard is null)
			{
				throw new ArgumentException(nameof(pasteboard));
			}

			// See notes in Clipboard.Android.cs on async code usage here
			CoreDispatcher.Main.RunAsync(
				CoreDispatcherPriority.High,
				async () =>
				{
					var data = content?.GetView();

					var declaredTypes = new List<string>();

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
						// Use `NSPasteboardTypeRTF` instead of `NSPasteboardTypeRTFD` for max compatiblity
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

		private static DataPackageView GetFromNative(NSPasteboard pasteboard)
		{
			if (pasteboard is null)
			{
				throw new ArgumentException(nameof(pasteboard));
			}

			var dataPackage = new DataPackage();

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
	}
}

#endif
