#if __MACOS__
#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using AppKit;
using Foundation;
using Windows.Storage;
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
		/// Creates new, native drag and drop data from the contents of the given <see cref="DataPackageView"/>.
		/// </summary>
		/// <param name="data">The content to create the native drag and drop data from.</param>
		internal static async Task<NSDraggingItem[]> CreateNativeDragDropData(DataPackageView data)
		{
			NSDraggingItem draggingItem;
			var items = new List<NSDraggingItem>();

			if (data?.Contains(StandardDataFormats.Html) ?? false)
			{
				var html = await data.GetHtmlFormatAsync();

				if (!string.IsNullOrEmpty(html))
				{

				}
			}

			if (data?.Contains(StandardDataFormats.Rtf) ?? false)
			{
				var rtf = await data.GetRtfAsync();

				if (!string.IsNullOrEmpty(rtf))
				{
					
				}
			}

			if (data?.Contains(StandardDataFormats.Text) ?? false)
			{
				var text = await data.GetTextAsync();

				if (!string.IsNullOrEmpty(text))
				{
					draggingItem = new NSDraggingItem((NSString)text);
					draggingItem.DraggingFrame = new CoreGraphics.CGRect(0, 0, 1, 1); // Must be set
					items.Add(draggingItem);
				}
			}

			return items.ToArray();
		}

		/// <summary>
		/// Creates a new <see cref="DataPackageView"/> from the native drag and drop data.
		/// </summary>
		/// <returns>A new <see cref="DataPackageView"/> representing the native drag and drop data.</returns>
		internal static DataPackageView CreateFromNativeDragDropData(NSDraggingInfo draggingInfo)
		{
			return GetFromNative(draggingInfo.DraggingPasteboard);
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

			// Extract all the standard data format information from the pasteboard items.
			// Each format can only be used once; therefore, the last occurrence of the format will be the one used.
			foreach (NSPasteboardItem item in pasteboard.PasteboardItems)
			{
				if (item.Types.Contains(NSPasteboard.NSPasteboardTypeHTML))
				{
					var html = item.GetStringForType(NSPasteboard.NSPasteboardTypeHTML);
					if (html != null)
					{
						dataPackage.SetHtmlFormat(html);
					}
				}

				if (item.Types.Contains(NSPasteboard.NSPasteboardTypeRTF))
				{
					var rtf = item.GetStringForType(NSPasteboard.NSPasteboardTypeRTF);
					if (rtf != null)
					{
						dataPackage.SetRtf(rtf);
					}
				}

				if (item.Types.Contains(NSPasteboard.NSPasteboardTypeFileUrl))
				{
					// Drag and drop will use temporary URL's similar to: file:///.file/id=1234567.1234567
					var tempFileUrl = item.GetStringForType(NSPasteboard.NSPasteboardTypeFileUrl);

					// Files may be very large, we never want to load them until they are needed.
					// Therefore, create a data provider used to asyncronously fetch the file.
					dataPackage.SetDataProvider(
						StandardDataFormats.StorageItems,
						async cancellationToken =>
						{
							// Convert rom a temp Url (see above example) into an absolute file path
							var fileUrl = new NSUrl(tempFileUrl);
							var file = await StorageFile.GetFileFromPathAsync(fileUrl.FilePathUrl.AbsoluteString);

							var storageItems = new List<IStorageItem>();
							storageItems.Add(file);

							return storageItems.AsReadOnly();
						});
				}

				if (item.Types.Contains(NSPasteboard.NSPasteboardTypeString))
				{
					var text = item.GetStringForType(NSPasteboard.NSPasteboardTypeString);
					if (text != null)
					{
						dataPackage.SetText(text);
					}
				}
			}

			return dataPackage.GetView();
		}
	}
}

#endif
