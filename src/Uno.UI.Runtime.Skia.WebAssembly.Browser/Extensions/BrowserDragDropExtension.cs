using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
using Microsoft.UI.Xaml;

using Uno.Extensions;
using Uno.Helpers.Serialization;
using Uno.Foundation.Logging;
using Uno.Storage.Internal;

using System.Runtime.InteropServices.JavaScript;
using _HtmlPointerButtonsState = Uno.UI.Runtime.Skia.BrowserPointerInputSource.HtmlPointerButtonsState;
using System.Text.Json.Serialization;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.DragDrop;
using Windows.ApplicationModel.DataTransfer.DragDrop.Core;

namespace Uno.UI.Runtime.Skia
{
	internal partial class BrowserDragDropExtension : IDragDropExtension
	{
		private static readonly Logger _log = typeof(BrowserDragDropExtension).Log();
		private static readonly char[] _newLineChars = ['\r', '\n'];

		public static BrowserDragDropExtension Instance { get; } = new();

		private readonly CoreDragDropManager _manager;

		private NativeDrop? _pendingNativeDrop;
		private DataPackage? _pendingDataPackage;

		private BrowserDragDropExtension()
		{
			_manager = CoreDragDropManager.GetForCurrentView()
				?? throw new InvalidOperationException("No CoreDragDropManager available for current thread.");
			NativeMethods.Init();
		}

		/// <inheritdoc />
		void IDragDropExtension.StartNativeDrag(CoreDragInfo info, Action<DataPackageOperation> onCompleted)
		{
			// There is no way to programmatically initiate a DragAndDrop in browsers.
			// We instead have to rely on the native drag and drop support.
		}

		[Preserve]
		[EditorBrowsable(EditorBrowsableState.Never)]
		[JSExport]
		public static string OnNativeDropEvent(
			string eventName,
			string allowedOperations,
			string acceptedOperation,
			string dataItems,
			double timestamp,
			double x,
			double y,
			int id,
			int buttons,
			bool shift,
			bool ctrl,
			bool alt)
		{
			try
			{
				if (_log.IsEnabled(LogLevel.Trace))
				{
					_log.Trace($"Received native drop event: id={id}"
						+ $" event={eventName} "
						+ $"timestamp={timestamp:F0} @({x:F2},{y:F2}) "
						+ $"buttons={(_HtmlPointerButtonsState)buttons} "
						+ $"modifiers={(shift ? "shift " : "")}{(ctrl ? "ctrl " : "")}{(alt ? "alt" : "")} "
						+ $"allowed={allowedOperations} accepted={acceptedOperation} "
						+ $"dataItems={(dataItems.IsNullOrWhiteSpace() ? "<none>" : string.Join(", ", JsonHelper.Deserialize<DataEntry[]>(dataItems, DragDropSerializationContext.Default)))}");
				}

				var drop = new NativeDrop(id, timestamp, x, y, buttons, ctrl, shift, alt);
				switch (eventName)
				{
					case "dragenter":
						if (Instance._pendingNativeDrop is { Id: var pendingId })
						{
							if (pendingId == id)
							{
								_log.Error(
									$"The native drop operation (#{pendingId}) has already been started in managed code "
									+ "and should have been ignored by native code. Ignoring that redundant dragenter.");

								// We are ignoring that event, we don't want to change the currently accepted operation

								break;
							}
							else
							{
								_log.Error(
									$"A native drop operation (#{pendingId}) is already pending. "
									+ "Only one native drop operation is supported on wasm currently."
									+ "Aborting previous operation and beginning a new one.");

								Instance._manager.ProcessAborted(pendingId);
							}
						}

						var allowed = ToDataPackageOperation(allowedOperations);
						var data = CreateDataPackage(id, dataItems);
						Instance._pendingDataPackage = data;
						var info = new CoreDragInfo(drop, data.GetView(), allowed);

						if (_log.IsEnabled(LogLevel.Debug))
						{
							_log.Debug($"Starting new native drop operation {drop.Id}");
						}

						Instance._pendingNativeDrop = drop;
						info.RegisterCompletedCallback(result =>
						{
							if (_log.IsEnabled(LogLevel.Debug))
							{
								_log.Debug($"Completed native drop operation #{drop.Id}: {result}");
							}

							if (Instance._pendingNativeDrop?.Id == drop.Id)
							{
								Instance._pendingNativeDrop = null;
								Instance._pendingDataPackage = null;
							}
						});
						Instance._manager.DragStarted(info);
						break;

					case "dragover" when Instance._pendingNativeDrop != null:
						Instance._pendingNativeDrop = drop;
						acceptedOperation = ToNativeOperation(Instance._manager.ProcessMoved(Instance._pendingNativeDrop));
						break;

					case "dragleave" when Instance._pendingNativeDrop != null:
						Instance._pendingNativeDrop = drop;
						acceptedOperation = ToNativeOperation(Instance._manager.ProcessAborted(Instance._pendingNativeDrop.Value.Id));
						break;

					case "drop" when Instance._pendingNativeDrop != null:
						var view = Instance._pendingDataPackage!.GetView();
						foreach (var availableFormat in view.AvailableFormats)
						{
							// start all async fetchers before the drop event returns since the browser
							// will destroy the DataTransfer object and the file details, etc. at that point.
							_ = view.GetDataAsync(availableFormat);
						}
						Instance._pendingNativeDrop = drop;
						acceptedOperation = ToNativeOperation(Instance._manager.ProcessDropped(Instance._pendingNativeDrop));
						break;
				}

				return acceptedOperation;
			}
			catch (Exception error)
			{
				if (_log.IsEnabled(LogLevel.Error))
				{
					_log.Error($"Failed to dispatch native drop event: {error}");
				}

				return ToNativeOperation(DataPackageOperation.None);
			}
		}

		private static DataPackage CreateDataPackage(int id, string dataItems)
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
			var entries = JsonHelper.Deserialize<DataEntry[]>(dataItems, DragDropSerializationContext.Default);

			var files = entries.Where(entry => entry.kind.Equals("file", StringComparison.OrdinalIgnoreCase)).ToList();
			var texts = entries.Where(entry => entry.kind.Equals("string", StringComparison.OrdinalIgnoreCase)).ToList();

			if (files.Any())
			{
				var ids = files
					.Select(item => item.id)
					.ToArray();
				package.SetDataProvider(
					StandardDataFormats.StorageItems,
					async ct => await RetrieveFiles(ct, id, ids));

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
					var (formatId, provider) = GetTextProvider(id, text.id, text.type);

					package.SetDataProvider(formatId, provider);
				}
			}

			package.Destroyed += (sender, args) =>
			{
				NativeMethods.RemoveId(id);
			};

			return package;
		}

		private static (string formatId, FuncAsync<object> provider) GetTextProvider(int id, int textId, string type)
			=> type switch
			{
				"text/uri-list" => // https://datatracker.ietf.org/doc/html/rfc2483#section-5
					(StandardDataFormats.WebLink,
					async ct => new Uri((await NativeMethods.RetrieveTextAsync(id, textId))
						.Split(_newLineChars, StringSplitOptions.RemoveEmptyEntries)
						.Where(line => !line.StartsWith('#'))
						.First())),
				"text/plain" => (StandardDataFormats.Text, async ct => await NativeMethods.RetrieveTextAsync(id, textId)),
				"text/html" => (StandardDataFormats.Html, async ct => await NativeMethods.RetrieveTextAsync(id, textId)),
				"text/rtf" => (StandardDataFormats.Rtf, async ct => await NativeMethods.RetrieveTextAsync(id, textId)),
				_ => (type, async ct => await NativeMethods.RetrieveTextAsync(id, textId))
			};

		private static async Task<IReadOnlyList<IStorageItem>> RetrieveFiles(CancellationToken ct, int id, params int[] itemsIds)
		{
			var infosRaw = await NativeMethods.RetrieveFilesAsync(id, itemsIds);
			var infos = JsonHelper.Deserialize<NativeStorageItemInfo[]>(infosRaw, DragDropSerializationContext.Default);
			var items = infos.Select(StorageFile.GetFromNativeInfo).ToList();

			return items;
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

		private readonly struct NativeDrop(
			int id,
			double timestamp,
			double x,
			double y,
			int buttons,
			bool ctrl,
			bool shift,
			bool alt)
			: IDragEventSource
		{
			private readonly _HtmlPointerButtonsState _buttons = (_HtmlPointerButtonsState)buttons;

			/// <inheritdoc />
			public long Id => id;

			/// <inheritdoc />
			public uint FrameId => BrowserPointerInputSource.ToFrameId(timestamp);

			/// <inheritdoc />
			public (Point location, DragDropModifiers modifier) GetState()
			{
				var position = new Point(x, y);
				var modifier = DragDropModifiers.None;

				if (_buttons.HasFlag(_HtmlPointerButtonsState.Left))
				{
					modifier |= DragDropModifiers.LeftButton;
				}
				if (_buttons.HasFlag(_HtmlPointerButtonsState.Middle))
				{
					modifier |= DragDropModifiers.MiddleButton;
				}
				if (_buttons.HasFlag(_HtmlPointerButtonsState.Right))
				{
					modifier |= DragDropModifiers.RightButton;
				}

				if (shift)
				{
					modifier |= DragDropModifiers.Shift;
				}
				if (ctrl)
				{
					modifier |= DragDropModifiers.Control;
				}
				if (alt)
				{
					modifier |= DragDropModifiers.Alt;
				}

				return (position, modifier);
			}

			/// <inheritdoc />
			public Point GetPosition(object? relativeTo)
				=> relativeTo == null
					? new Point(x, y)
					: ((UIElement)relativeTo).TransformToVisual(null).Inverse.TransformPoint(new Point(x, y));
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

		[JsonSerializable(typeof(DataEntry[]))]
		[JsonSerializable(typeof(DataEntry))]
		[JsonSerializable(typeof(NativeStorageItemInfo[]))]
		[JsonSerializable(typeof(NativeStorageItemInfo))]
		private partial class DragDropSerializationContext : JsonSerializerContext;

		private static partial class NativeMethods
		{
			[JSImport($"globalThis.Windows.ApplicationModel.DataTransfer.DragDrop.Core.{nameof(BrowserDragDropExtension)}.init")]
			internal static partial void Init();

			[JSImport($"globalThis.Windows.ApplicationModel.DataTransfer.DragDrop.Core.{nameof(BrowserDragDropExtension)}.retrieveFiles")]
			internal static partial Task<string> RetrieveFilesAsync(int pendingDropId, int[] itemIds);

			[JSImport($"globalThis.Windows.ApplicationModel.DataTransfer.DragDrop.Core.{nameof(BrowserDragDropExtension)}.retrieveText")]
			internal static partial Task<string> RetrieveTextAsync(int pendingDropId, int itemId);

			[JSImport($"globalThis.Windows.ApplicationModel.DataTransfer.DragDrop.Core.{nameof(BrowserDragDropExtension)}.removeId")]
			internal static partial Task<string> RemoveId(int pendingDropId);
		}
	}
}
