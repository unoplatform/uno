#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;

using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
using Uno;
using Uno.Extensions;
using Uno.Foundation;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Interop;
using Uno.Helpers.Serialization;
using Uno.Foundation.Logging;
using Uno.Storage.Internal;
using Uno.UI;
using Uno.UI.Xaml;
using System.Diagnostics.CodeAnalysis;

using NativeMethods = __Windows.ApplicationModel.DataTransfer.DragDrop.Core.DragDropExtension.NativeMethods;
using System.Runtime.InteropServices.JavaScript;

// As IDragDropExtension is internal, the generated registration cannot be used.
// [assembly: ApiExtension(typeof(Windows.ApplicationModel.DataTransfer.DragDrop.Core.IDragDropExtension), typeof(Windows.ApplicationModel.DataTransfer.DragDrop.Core.DragDropExtension))]

namespace Windows.ApplicationModel.DataTransfer.DragDrop.Core
{
	internal partial class DragDropExtension : IDragDropExtension
	{
		private const long _textReadTimeoutTicks = 10 * TimeSpan.TicksPerSecond;

		private static readonly Logger _log = typeof(DragDropExtension).Log();
		private static readonly char[] _newLineChars = new[] { '\r', '\n' };

		private static DragDropExtension? _current;

		/// <summary>
		/// Conditional support for external drag and drop. See the UnoDragDropExternalSupport msbuild property.
		/// </summary>
		public static bool IsExternalDragAndDropSupported { get; } = true;

		public static DragDropExtension GetForCurrentView()
		{
			// Note: We use the GetForCurrentView naming pattern, but we don't support multi-threading yet
			//		 and the '_current' is actually just static.

			if (_current is null && Interlocked.CompareExchange(ref _current, new DragDropExtension(), null) is null)
			{
				if (IsExternalDragAndDropSupported)
				{
					// For now we enable the D&DExtension sync at creation and we don't support disable.
					// This allow us to prevent a drop of a content on an app which actually don't support D&D
					// (would drive the browser to open the dragged file and "dismiss" the app).
					_current.Enable();
				}
				else
				{
					EnableExternalWarning();
				}
			}

			return _current;
		}

		private readonly CoreDragDropManager _manager;

		private int _isInitialized;
		private TSInteropMarshaller.HandleRef<DragDropExtensionEventArgs>? _args;
		private NativeDrop? _pendingNativeDrop;

		private DragDropExtension()
		{
			_manager = CoreDragDropManager.GetForCurrentView()
				?? throw new InvalidOperationException("No CoreDragDropManager available for current thread.");
		}

		[DynamicDependency(nameof(OnNativeDropEvent))]
		private void Enable()
		{
			if (Interlocked.CompareExchange(ref _isInitialized, 1, 0) == 0)
			{
				_args = TSInteropMarshaller.Allocate<DragDropExtensionEventArgs>(
					"UnoStatic_Windows_ApplicationModel_DataTransfer_DragDrop_Core_DragDropExtension:enable",
					"UnoStatic_Windows_ApplicationModel_DataTransfer_DragDrop_Core_DragDropExtension:disable");
			}
			else
			{
				throw new InvalidOperationException("Multiple DragDropExtension is not supported yet.");
			}
		}

		private static void EnableExternalWarning()
		{
			NativeMethods.RegisterNoOp();
		}

		/// <inheritdoc />
		void IDragDropExtension.StartNativeDrag(CoreDragInfo info)
		{
			// There is no way to programmatically initiate a DragAndDrop in browsers.
			// We instead have to rely on the native drag and drop support.
		}

		[Preserve]
		[EditorBrowsable(EditorBrowsableState.Never)]
		[JSExport]
		public static string OnNativeDropEvent()
		{
			try
			{
				if (_log.IsEnabled(LogLevel.Trace))
				{
					_log.Trace("Receiving native drop event.");
				}

				if (_current?._args is { } args)
				{
					args.Value = _current.OnNativeDropEvent(args.Value);
					return "true";
				}
				else
				{
					if (_log.IsEnabled(LogLevel.Error))
					{
						_log.Error($"DragDropExtension not ready to process native drop event (current={_current} | args={_current?._args}).");
					}

					return "false";
				}
			}
			catch (Exception error)
			{
				if (_log.IsEnabled(LogLevel.Error))
				{
					_log.Error($"Failed to dispatch native drop event: {error}");
				}

				return "false";
			}
		}

		private DragDropExtensionEventArgs OnNativeDropEvent(DragDropExtensionEventArgs args)
		{
			if (_log.IsEnabled(LogLevel.Trace))
			{
				_log.Trace($"Received native drop event: {args}");
			}

			DataPackageOperation? acceptedOperation = DataPackageOperation.None;
			switch (args.eventName)
			{
				case "dragenter":
					if (_pendingNativeDrop != null)
					{
						if (_pendingNativeDrop.Id == args.id)
						{
							_log.Error(
								$"The native drop operation (#{_pendingNativeDrop.Id}) has already been started in managed code "
								+ "and should have been ignored by native code. Ignoring that redundant dragenter.");

							// We are ignoring that event, we don't want to change the currently accepted operation
							acceptedOperation = default;

							break;
						}
						else
						{
							_log.Error(
								$"A native drop operation (#{_pendingNativeDrop.Id}) is already pending. "
								+ "Only one native drop operation is supported on wasm currently."
								+ "Aborting previous operation and beginning a new one.");

							_manager.ProcessAborted(_pendingNativeDrop.Id);
						}
					}

					var drop = new NativeDrop(args);
					var allowed = ToDataPackageOperation(args.allowedOperations);
					var data = CreateDataPackage(args.dataItems);
					var info = new CoreDragInfo(drop, data.GetView(), allowed);

					if (_log.IsEnabled(LogLevel.Debug))
					{
						_log.Debug($"Starting new native drop operation {drop.Id}");
					}

					_pendingNativeDrop = drop;
					info.RegisterCompletedCallback(result =>
					{
						if (_log.IsEnabled(LogLevel.Debug))
						{
							_log.Debug($"Completed native drop operation #{drop.Id}: {result}");
						}

						if (_pendingNativeDrop == drop)
						{
							_pendingNativeDrop = null;
						}
					});
					_manager.DragStarted(info);
					break;

				case "dragover" when _pendingNativeDrop != null:
					_pendingNativeDrop.Update(args);
					acceptedOperation = _manager.ProcessMoved(_pendingNativeDrop);
					break;

				case "dragleave" when _pendingNativeDrop != null:
					_pendingNativeDrop.Update(args);
					acceptedOperation = _manager.ProcessAborted(_pendingNativeDrop.Id);
					_pendingNativeDrop = null;
					break;

				case "drop" when _pendingNativeDrop != null:
					_pendingNativeDrop.Update(args);
					acceptedOperation = _manager.ProcessDropped(_pendingNativeDrop);
					_pendingNativeDrop = null;
					break;
			}

			var result = new DragDropExtensionEventArgs
			{
				id = args.id,
				eventName = "result",
				acceptedOperation = acceptedOperation.HasValue
					? ToNativeOperation(acceptedOperation.Value)
					: args.acceptedOperation,
				allowedOperations = "",
				dataItems = "",
			};

			return result;
		}

		private DataPackage CreateDataPackage(string dataItems)
		{
			if (dataItems is null)
			{
				throw new ArgumentNullException(nameof(dataItems), "The dataItems is full-filled only for selected events!");
			}

			// Note about images:
			//		There is no common type for image drag and drop in browsers.
			//		Only FireFox provides data of the kind "string" ("other" when coming from the same page) and type "application/x-moz-nativeimage",
			//		but the content marshalling doesn't seem to work properly, and there are no equivalent custom type for chromium :(
			//		Consequently the only way to get a "Bitmap" data in the package is by D&Ding a file with a MIME type starting by "image/"
			// Note about unknown types:
			//		we don't support any other "kind" (we can have an "other" when D&Ding an image within the same page on FF),
			//		as we don't have any way to properly retrieve the data.
			//		We however support to propagate any "string" that does not have a standard MIME type so we don't restrict too much applications.

			var package = new DataPackage();
			var entries = JsonHelper.Deserialize<DataEntry[]>(dataItems);

			var files = entries.Where(entry => entry.kind.Equals("file", StringComparison.OrdinalIgnoreCase)).ToList();
			var texts = entries.Where(entry => entry.kind.Equals("string", StringComparison.OrdinalIgnoreCase)).ToList();

			if (files.Any())
			{
				var ids = files
					.Select(item => item.id)
					.ToArray();
				package.SetDataProvider(
					StandardDataFormats.StorageItems,
					async ct => await RetrieveFiles(ct, ids));

				// There is no kind for image, but when we drag and drop an image from a browser to another one, we sometimes get it as a file.
				var image = files.FirstOrDefault(file => file.type.StartsWith("image/", StringComparison.OrdinalIgnoreCase));
				if (image.type != null)
				{
					package.SetDataProvider(
						StandardDataFormats.Bitmap,
						async ct => RandomAccessStreamReference.CreateFromFile((IStorageFile)(await RetrieveFiles(ct, image.id)).Single()));
				}
			}

			if (texts.Any())
			{
				foreach (var text in texts)
				{
					var (formatId, provider) = GetTextProvider(text.id, text.type);

					package.SetDataProvider(formatId, provider);
				}
			}

			return package;
		}

		private static (string formatId, FuncAsync<object> provider) GetTextProvider(int id, string type)
			=> type switch
			{
				"text/uri-list" => // https://datatracker.ietf.org/doc/html/rfc2483#section-5
					(StandardDataFormats.WebLink,
					async ct => new Uri((await RetrieveText(ct, id))
						.Split(_newLineChars, StringSplitOptions.RemoveEmptyEntries)
						.Where(line => !line.StartsWith('#'))
						.First())),
				"text/plain" => (StandardDataFormats.Text, async ct => await RetrieveText(ct, id)),
				"text/html" => (StandardDataFormats.Html, async ct => await RetrieveText(ct, id)),
				"text/rtf" => (StandardDataFormats.Rtf, async ct => await RetrieveText(ct, id)),
				_ => (type, async ct => await RetrieveText(ct, id))
			};

		private static async Task<IReadOnlyList<IStorageItem>> RetrieveFiles(CancellationToken ct, params int[] itemsIds)
		{
			var infosRaw = await NativeMethods.RetrieveFilesAsync(itemsIds);
			var infos = JsonHelper.Deserialize<NativeStorageItemInfo[]>(infosRaw);
			var items = infos.Select(StorageFile.GetFromNativeInfo).ToList();

			return items;
		}

		private static async Task<string> RetrieveText(CancellationToken ct, int itemId)
		{
			return await NativeMethods.RetrieveTextAsync(itemId);
		}

		private static DataPackageOperation ToDataPackageOperation(string allowedOperations)
			=> allowedOperations?.ToLowerInvariant() switch
			{
				// https://developer.mozilla.org/en-US/docs/Web/API/DataTransfer/effectAllowed#values
				"none" => DataPackageOperation.None,
				"copy" => DataPackageOperation.Copy,
				"copyLink" => DataPackageOperation.Copy | DataPackageOperation.Link,
				"copyMove" => DataPackageOperation.Copy | DataPackageOperation.Move,
				"link" => DataPackageOperation.Link,
				"linkMove" => DataPackageOperation.Link | DataPackageOperation.Move,
				"move" => DataPackageOperation.Move,
				"all" => DataPackageOperation.Copy | DataPackageOperation.Move | DataPackageOperation.Link,
				"uninitialized" => DataPackageOperation.Copy | DataPackageOperation.Move | DataPackageOperation.Link,
				null => DataPackageOperation.Copy | DataPackageOperation.Move | DataPackageOperation.Link,
				_ => DataPackageOperation.None
			};

		private static string ToNativeOperation(DataPackageOperation acceptedOperation)
		{
			// If multiple flags set (which should not!), the UWP precedence is Link > Copy > Move
			// This is the same logic used in the DragView.ToGlyph
			if (acceptedOperation.HasFlag(DataPackageOperation.Link))
			{
				return "link";
			}
			else if (acceptedOperation.HasFlag(DataPackageOperation.Copy))
			{
				return "copy";
			}
			else if (acceptedOperation.HasFlag(DataPackageOperation.Move))
			{
				return "move";
			}
			else // None
			{
				return "none";
			}
		}

		private class NativeDrop : IDragEventSource
		{
			private DragDropExtensionEventArgs _args;

			public NativeDrop(DragDropExtensionEventArgs args)
			{
				_args = args;
			}

			/// <inheritdoc />
			public long Id => _args.id;

			/// <inheritdoc />
			public uint FrameId => PointerRoutedEventArgs.ToFrameId(_args.timestamp);

			/// <inheritdoc />
			public (Point location, DragDropModifiers modifier) GetState()
			{
				var position = new Point(_args.x, _args.y);
				var modifier = DragDropModifiers.None;

				var buttons = (WindowManagerInterop.HtmlPointerButtonsState)_args.buttons;
				if (buttons.HasFlag(WindowManagerInterop.HtmlPointerButtonsState.Left))
				{
					modifier |= DragDropModifiers.LeftButton;
				}
				if (buttons.HasFlag(WindowManagerInterop.HtmlPointerButtonsState.Middle))
				{
					modifier |= DragDropModifiers.MiddleButton;
				}
				if (buttons.HasFlag(WindowManagerInterop.HtmlPointerButtonsState.Right))
				{
					modifier |= DragDropModifiers.RightButton;
				}

				if (_args.shift)
				{
					modifier |= DragDropModifiers.Shift;
				}
				if (_args.ctrl)
				{
					modifier |= DragDropModifiers.Control;
				}
				if (_args.alt)
				{
					modifier |= DragDropModifiers.Alt;
				}

				return (position, modifier);
			}

			/// <inheritdoc />
			public Point GetPosition(object? relativeTo)
				=> PointerRoutedEventArgs.ToRelativePosition(new Point(_args.x, _args.y), relativeTo as UIElement);

			public void Update(DragDropExtensionEventArgs args)
			{
				if (_log.IsEnabled(LogLevel.Trace))
				{
					_log.Trace($"Updating native drop operation #{Id} ({args.eventName})");
				}

				_args = args;
			}
		}

		[TSInteropMessage]
		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct DragDropExtensionEventArgs
		{
			[MarshalAs(TSInteropMarshaller.LPUTF8Str)]
			public string eventName;
			[MarshalAs(TSInteropMarshaller.LPUTF8Str)]
			public string allowedOperations;
			[MarshalAs(TSInteropMarshaller.LPUTF8Str)]
			public string acceptedOperation;

			// Note: This should be an array, but it's currently not supported by marshaling for return values.
			[MarshalAs(TSInteropMarshaller.LPUTF8Str)]
			public string dataItems; // Filled only for eventName == dragenter

			public double timestamp;
			public double x;
			public double y;

			public int id;
			public int buttons; // HtmlPointerButtonsState

			public bool shift;
			public bool ctrl;
			public bool alt;


			/// <inheritdoc />
			public override string ToString()
			{
				return $"[{eventName}] {timestamp:F0} @({x:F2},{y:F2})"
					+ $" | buttons: {(WindowManagerInterop.HtmlPointerButtonsState)buttons}"
					+ $" | modifiers: {string.Join(", ", GetModifiers(this))}"
					+ $" | allowed: {allowedOperations} ({ToDataPackageOperation(allowedOperations)})"
					+ $" | accepted: {acceptedOperation}"
					+ $" | entries: {dataItems} ({(!dataItems.IsNullOrWhiteSpace() ? string.Join(", ", JsonHelper.Deserialize<DataEntry[]>(dataItems)) : "")})";

				IEnumerable<string> GetModifiers(DragDropExtensionEventArgs that)
				{
					if (that.shift)
					{
						yield return "shift";
					}
					if (that.ctrl)
					{
						yield return "ctrl";
					}
					if (that.alt)
					{
						yield return "alt";
					}

					if (!that.shift && !that.ctrl && !that.alt)
					{
						yield return "none";
					}
				}
			}
		}

		private struct DataEntry
		{
#pragma warning disable CS0649 // error CS0649: Field 'DragDropExtension.DataEntry.kind' is never assigned to, and will always have its default value null
			[global::System.Text.Json.Serialization.JsonIncludeAttribute]
			public int id;

			[global::System.Text.Json.Serialization.JsonIncludeAttribute]
			public string kind;

			[global::System.Text.Json.Serialization.JsonIncludeAttribute]
			public string type;
#pragma warning restore CS0649 // error CS0649: Field 'DragDropExtension.DataEntry.kind' is never assigned to, and will always have its default value null

			/// <inheritdoc />
			public override string ToString()
				=> $"[#{id}: {kind} {type}]";
		}
	}
}
