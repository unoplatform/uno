#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Windows.ApplicationModel.DataTransfer
{
	public partial class DataPackageView
	{
		private readonly ImmutableDictionary<string, object> _data;
		private readonly ImmutableDictionary<string, RandomAccessStreamReference> _resourceMap;
		private readonly Action<string?, DataPackageOperation> _reportCompleted;

		private string? _acceptedFormatId;

		internal DataPackageView(
			DataPackageOperation requestedOperation,
			ImmutableDictionary<string, object> data,
			ImmutableDictionary<string, RandomAccessStreamReference> resourceMap,
			Action<string?, DataPackageOperation> reportCompleted,
			DataPackagePropertySet properties)
		{
			_data = data;
			_resourceMap = resourceMap;
			_reportCompleted = reportCompleted;
			RequestedOperation = requestedOperation;
			Properties = new DataPackagePropertySetView(properties);
		}

		public DataPackagePropertySetView Properties { get; }

		public DataPackageOperation RequestedOperation { get; }

		public IReadOnlyList<string> AvailableFormats => _data.Keys.Where(k => !k.StartsWith(DataPackage.UnoPrivateDataPrefix)).ToArray();

		public IAsyncOperation<IReadOnlyDictionary<string, RandomAccessStreamReference>> GetResourceMapAsync()
			=> Task.FromResult(_resourceMap as IReadOnlyDictionary<string, RandomAccessStreamReference>).AsAsyncOperation();

		public bool Contains(string formatId)
		{
			if (formatId is null)
			{
				throw new ArgumentNullException(nameof(formatId));
			}

			return _data.ContainsKey(formatId);
		}

		internal object? FindRawData(string formatId)
			=> _data.TryGetValue(formatId, out var data) ? data : null;

		public IAsyncOperation<object> GetDataAsync(string formatId)
			// Note: Using this, application can gain access to the data prefixed with DataPackage.UnoPrivateDataKey ... which is acceptable
			=> GetData<object>(formatId);

		public IAsyncOperation<Uri> GetWebLinkAsync()
			=> GetData<Uri>(StandardDataFormats.WebLink);

		public IAsyncOperation<Uri> GetApplicationLinkAsync()
			=> GetData<Uri>(StandardDataFormats.ApplicationLink);

		public IAsyncOperation<string> GetTextAsync()
			=> GetData<string>(StandardDataFormats.Text);

		public IAsyncOperation<Uri> GetUriAsync()
			=> GetData<Uri>(StandardDataFormats.Uri);

		public IAsyncOperation<string> GetHtmlFormatAsync()
			=> GetData<string>(StandardDataFormats.Html);

		public IAsyncOperation<string> GetRtfAsync()
			=> GetData<string>(StandardDataFormats.Rtf);

		public IAsyncOperation<RandomAccessStreamReference> GetBitmapAsync()
			=> GetData<RandomAccessStreamReference>(StandardDataFormats.Bitmap);

		public IAsyncOperation<IReadOnlyList<IStorageItem>> GetStorageItemsAsync()
			=> GetData<IReadOnlyList<IStorageItem>>(StandardDataFormats.StorageItems);

		private IAsyncOperation<TResult> GetData<TResult>(string format)
		{
			if (_data.TryGetValue(format, out var data))
			{
				return data switch
				{
					DataProviderHandler provider => GetDataAsync<TResult>(format, provider).AsAsyncOperation(),
					_ => Task.FromResult((TResult)data).AsAsyncOperation()
				};
			}
			else
			{
				throw new InvalidOperationException($"DataPackage does not contain {format} data.");
			}
		}

		private async Task<TResult> GetDataAsync<TResult>(string format, DataProviderHandler asyncData)
		{
			var request = new DataProviderRequest(format);
			try
			{
				asyncData(request);

				var data = await request.GetData();
				return (TResult)data;
			}
			catch (Exception e)
			{
				throw new InvalidOperationException($"Failed to asynchronously load the data of id '{format}'", e);
			}
			finally
			{
				request.Dispose();
			}
		}

		public void ReportOperationCompleted(DataPackageOperation value)
			=> _reportCompleted(_acceptedFormatId, value); // The event can be raised multiple times!

		public void SetAcceptedFormatId(string formatId)
			=> _acceptedFormatId = formatId;
	}
}
