using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Uno.ApplicationModel.DataTransfer;
using Uno.Extensions;
using Uno.Foundation;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;

namespace Windows.ApplicationModel.DataTransfer
{
	public static partial class Clipboard
	{
		private static readonly Lazy<IClipboardExtension?> _clipboardExtension = new Lazy<IClipboardExtension?>(() =>
		{
			if (ApiExtensibility.CreateInstance<IClipboardExtension>(typeof(Clipboard), out var clipboardExtension))
			{
				return clipboardExtension;
			}
			if (typeof(Clipboard).Log().IsEnabled(LogLevel.Error))
			{
				typeof(Clipboard).Log().LogError("Clipboard is not implemented on this platform.");
			}
			return null;
		});

		public static void Flush()
		{
			_clipboardExtension.Value?.Flush();
		}

		public static void Clear()
		{
			_clipboardExtension.Value?.Clear();
		}

		public static DataPackageView? GetContent()
		{
			return _clipboardExtension.Value?.GetContent();
		}

		public static void SetContent(DataPackage content)
		{
			_clipboardExtension.Value?.SetContent(content);
		}

		private static void StartContentChanged()
		{
			if (_clipboardExtension.Value == null)
			{
				return;
			}
			_clipboardExtension.Value.ContentChanged += ClipboardExtension_ContentChanged;
			_clipboardExtension.Value.StartContentChanged();
		}

		private static void StopContentChanged()
		{
			if (_clipboardExtension.Value == null)
			{
				return;
			}
			_clipboardExtension.Value.StopContentChanged();
			_clipboardExtension.Value.ContentChanged -= ClipboardExtension_ContentChanged;
		}

		private static void ClipboardExtension_ContentChanged(object? sender, object? args)
		{
			OnContentChanged();
		}
	}
}
