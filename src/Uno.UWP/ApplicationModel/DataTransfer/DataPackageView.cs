#if !NET461
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Windows.ApplicationModel.DataTransfer
{
	public partial class DataPackageView
	{
		private readonly Dictionary<string, Task> _formatTasks = new Dictionary<string, Task>();

		public IReadOnlyList<string> AvailableFormats => _formatTasks.Keys.ToArray();

		public IAsyncOperation<string> GetTextAsync() =>
			GetFormatTask<string>(StandardDataFormats.Text).AsAsyncOperation();

		public IAsyncOperation<Uri> GetUriAsync() =>
			GetFormatTask<Uri>(StandardDataFormats.Uri).AsAsyncOperation();

		public IAsyncOperation<Uri> GetWebLinkAsync() => GetUriAsync();

		public IAsyncOperation<string> GetHtmlFormatAsync() =>
			GetFormatTask<string>(StandardDataFormats.Html).AsAsyncOperation();

		internal void SetFormatTask(string format, Task retrieverTask)
		{
			_formatTasks[format] = retrieverTask;
		}

		private Task<TResult> GetFormatTask<TResult>(string format)
		{
			if (_formatTasks.TryGetValue(format, out var task))
			{
				return (Task<TResult>)task;
			}
			else
			{
				throw new InvalidOperationException($"DataPackage does not contain {format} data.");
			}
		}

		public bool Contains(string formatId)
		{
			if (formatId is null)
			{
				throw new ArgumentNullException(nameof(formatId));
			}

			return _formatTasks.ContainsKey(formatId);
		}
	}
}
#endif
