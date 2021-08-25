#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Uno;
using Uno.Extensions;
using Uno.Foundation;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Interop;
using Uno.Helpers.Serialization;
using Uno.Logging;
using Uno.Storage.Internal;
using Uno.UI;
using Uno.UI.Xaml;

// As IDragDropExtension is internal, the generated registration cannot be used.
// [assembly: ApiExtension(typeof(Windows.ApplicationModel.DataTransfer.DragDrop.Core.IDragDropExtension), typeof(Windows.ApplicationModel.DataTransfer.DragDrop.Core.DragDropExtension))]

namespace Windows.ApplicationModel.DataTransfer.DragDrop.Core
{
	internal class DragDropExtension : IDragDropExtension
	{
		private const string _jsType = "Windows.ApplicationModel.DataTransfer.DragDrop.Core.DragDropExtension";

		private static DragDropExtension? _current;

		private readonly CoreDragDropManager _manager;

		private int _isInitialized;
		private TSInteropMarshaller.HandleRef<DragDropExtensionEventArgs>? _args;
		private NativeDrop? _pendingNativeDrop;

		public DragDropExtension()
		{
			_manager = CoreDragDropManager.GetForCurrentView()
				?? throw new InvalidOperationException("No CoreDragDropManager available for current thread.");

			if (Interlocked.CompareExchange(ref _current, null, this) != null)
			{
				throw new InvalidOperationException(
					"Multi-window (multi-threading) is not supported yet by DragDropExtension. "
					+ "Only one instance is allowed per app.");
			}

			// For now we enable the D&DExtension sync at creation and we don't support disable.
			// This allow us to prevent a drop of a content on an app which actually don't support D&D
			// (would drive the browser to open the dragged file and "dismiss" the app).
			Enable();
		}

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

		/// <inheritdoc />
		void IDragDropExtension.StartNativeDrag(CoreDragInfo info)
		{
			// There is no way to programmatically initiate a DragAndDrop in browsers.
			// We instead have to rely on the native drag and drop support.
		}

		[Preserve]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static string OnNativeDragAndDrop()
		{
			if (_current?._args is {} args)
			{
				args.Value = _current.OnNativeDragAndDrop(args.Value);
				return "true";
			}
			else
			{
				return "false";
			}
		}

		private DragDropExtensionEventArgs OnNativeDragAndDrop(DragDropExtensionEventArgs args)
		{
			var acceptedOperation = DataPackageOperation.None;
			switch (args.eventName)
			{
				case "dragenter":
					_pendingNativeDrop = new NativeDrop(args);

					var allowed = ToDataPackageOperation(args.allowedOperations);
					var data = CreateDataPackage(args.dataItems);
					var info = new CoreDragInfo(_pendingNativeDrop, data.GetView(), allowed);

					_manager.DragStarted(info);
					break;

				case "dragover" when _pendingNativeDrop != null:
					_pendingNativeDrop.Update(args);
					acceptedOperation = _manager.ProcessMoved(_pendingNativeDrop);
					break;

				case "dragleave" when _pendingNativeDrop != null:
					_pendingNativeDrop.Update(args);
					acceptedOperation = _manager.ProcessAborted(_pendingNativeDrop);
					_pendingNativeDrop = null;
					break;

				case "drop" when _pendingNativeDrop != null:
					_pendingNativeDrop.Update(args);
					acceptedOperation = _manager.ProcessDropped(_pendingNativeDrop);
					_pendingNativeDrop = null;
					break;
			}

			return new DragDropExtensionEventArgs { acceptedOperation = ToNativeOperation(acceptedOperation) };
		}

		private DataPackage CreateDataPackage(string dataItems)
		{
			if (dataItems is null)
			{
				throw new ArgumentNullException(nameof(dataItems), "The dataItems is full-filled only for selected events!");
			}

			var package = new DataPackage();
			var entries = JsonHelper
				.Deserialize<DataEntry[]>(dataItems)
				.GroupBy(item => item.Kind)
				.ToDictionary(group => group.Key);

			if (entries.TryGetValue("file", out var files))
			{
				entries.Remove("file"); // Remove so we can detect "other types"

				var ids = files
					.Select(item => item.Id)
					.ToArray();
				package.SetDataProvider(
					StandardDataFormats.StorageItems,
					async ct => await RetrieveFiles(ct, ids));

				// There is no kind for image, but when we drag and drop an image from a browser to another one, we sometimes get it as a file.
				var image = files.FirstOrDefault(file => file.Type.StartsWith("image/"));
				if (image.Type != null)
				{
					package.SetDataProvider(
						StandardDataFormats.Bitmap,
						async ct => RandomAccessStreamReference.CreateFromFile((IStorageFile)(await RetrieveFiles(ct, image.Id)).Single()));
				}
			}

			if (entries.TryGetValue("string", out var texts))
			{
				entries.Remove("string"); // Remove so we can detect "other types"

				foreach (var text in texts)
				{
					var (formatId, provider) = GetTextProvider(text.Id, text.Type);

					package.SetDataProvider(formatId, provider);
				}

				// There is not equivalent custom type for chromium :(
				if (!package.Contains(StandardDataFormats.Bitmap)
					&& texts.FirstOrDefault(text => text.Type.Equals("application/x-moz-nativeimage")) is {} textImage)
				{
					package.SetDataProvider(
						StandardDataFormats.Bitmap,
						async ct =>
						{
							var imageData = await RetrieveText(ct, textImage.Id);
							var imageStream = new MemoryStream(Encoding.UTF8.GetBytes(imageData));

							return RandomAccessStreamReference.CreateFromStream(imageStream.AsRandomAccessStream());
						});
				}
			}

			if (entries.Any() || this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"Types not supported for drag and drop operations: {string.Join(", ", entries.Keys)}");
			}

			return package;
		}

		private struct DataEntry
		{
			public int Id;
			public string Kind;
			public string Type;
		}

		private static (string formatId, FuncAsync<object> provider) GetTextProvider(int id, string type)
			=> type switch
			{
				"text/uri-list" => // https://datatracker.ietf.org/doc/html/rfc2483#section-5
					(StandardDataFormats.WebLink,
					async ct => new Uri((await RetrieveText(ct, id))
						.Split(new[]{'\r','\n'}, StringSplitOptions.RemoveEmptyEntries)
						.Where(line => !line.StartsWith("#"))
						.First())),
				"text/plain" => (StandardDataFormats.Text, async ct => await RetrieveText(ct, id)),
				"text/html" => (StandardDataFormats.Html, async ct => await RetrieveText(ct, id)),
				"text/rtf" => (StandardDataFormats.Rtf, async ct => await RetrieveText(ct, id)),
				_ => (type, async ct => await RetrieveText(ct, id))
			};

		private static async Task<IReadOnlyList<IStorageItem>> RetrieveFiles(CancellationToken ct, params int[] itemsIds)
		{
			var infosRaw = await WebAssemblyRuntime.InvokeAsync($"{_jsType}.retrieveFiles({string.Join(", ", itemsIds.Select(id => id.ToStringInvariant()))})", ct);
			var infos = JsonHelper.Deserialize<NativeStorageItemInfo[]>(infosRaw);
			var items = infos.Select(StorageFile.GetFromNativeInfo).ToList();

			return items;
		}

		private static async Task<string> RetrieveText(CancellationToken ct, int itemId)
		{
			using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct, new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
			var text = await WebAssemblyRuntime.InvokeAsync($"{_jsType}.retrieveText({itemId.ToStringInvariant()})", cts.Token);

			return text;
		}

		private static DataPackageOperation ToDataPackageOperation(string allowedOperations)
			=> allowedOperations.ToLowerInvariant() switch
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
			private static long _nextId = 0;
			private DragDropExtensionEventArgs _args;

			public NativeDrop(DragDropExtensionEventArgs args)
			{
				_args = args;
			}

			/// <inheritdoc />
			public long Id { get; } = Interlocked.Increment(ref _nextId);

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
				=> _args = args;
		}

		[TSInteropMessage]
		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public struct DragDropExtensionEventArgs
		{
			public string eventName;
			public double timestamp;
			public double x;
			public double y;
			public int buttons; // HtmlPointerButtonsState
			public bool shift;
			public bool ctrl;
			public bool alt;
			public string allowedOperations;
			public string acceptedOperation;

			// Note: This should be an array, but it's currently not supported by marshaling for return values.
			public string dataItems; // Filled only for eventName == dragenter
		}
	}
}
