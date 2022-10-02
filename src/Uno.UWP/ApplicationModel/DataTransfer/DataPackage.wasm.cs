#if __WASM__
#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno.Foundation;
using Uno.Helpers.Serialization;

namespace Windows.ApplicationModel.DataTransfer
{
	public partial class DataPackage
	{
		private const string ClipboardJsType = "Uno.Utils.Clipboard";
		private Task? _initializeFromClipboardTask;
		private object _initializeFromClipboardTaskLock = new object();

		internal Task InitializeFromNativeClipboardAsync(CancellationToken? cancellationToken = null)
		{
			lock (_initializeFromClipboardTaskLock)
			{
				_initializeFromClipboardTask = _initializeFromClipboardTask ?? InitializeFromNativeClipboardCoreAsync();
				return _initializeFromClipboardTask;
			}

			async Task InitializeFromNativeClipboardCoreAsync()
			{
				var mimesJson = await WebAssemblyRuntime.InvokeAsync(
					$"(async () => JSON.stringify(await {ClipboardJsType}.getDataMimes()))()",
					cancellationToken ?? CancellationToken.None);

				var hasPlain = false;
				var hasHtml = false;
				var hasRtf = false;
				var hasPng = false;

				if (mimesJson != null)
				{
					var mimes = JsonHelper.Deserialize<string[]>(mimesJson);
					foreach (var mime in mimes)
					{
						switch (mime)
						{
							case "text/plain":
								hasPlain = true;
								break;
							case "text/html":
								hasHtml = true;
								break;
							case "text/rtf":
								hasRtf = true;
								break;
							case "image/png":
								hasPng = true;
								break;
						}
					}
				}

				if (!hasPlain)
				{
					ImmutableInterlocked.TryRemove(ref _data, StandardDataFormats.Text, out _);
				}
				if (!hasHtml)
				{
					ImmutableInterlocked.TryRemove(ref _data, StandardDataFormats.Html, out _);
				}
				if (!hasRtf)
				{
					ImmutableInterlocked.TryRemove(ref _data, StandardDataFormats.Rtf, out _);
				}
				if (!hasPng)
				{
					ImmutableInterlocked.TryRemove(ref _data, StandardDataFormats.Bitmap, out _);
				}
			}
		}
	}
}

#endif
