#nullable enable

#if !NET461
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
			Action<string?, DataPackageOperation> reportCompleted)
		{
			_data = data;
			_resourceMap = resourceMap;
			_reportCompleted = reportCompleted;
			RequestedOperation = requestedOperation;
		}

		public DataPackageOperation RequestedOperation { get; }

		public IReadOnlyList<string> AvailableFormats => _data.Keys.ToArray();

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

		public IAsyncOperation<object> GetDataAsync(string formatId)
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

		private static async Task<TResult> GetDataAsync<TResult>(string format, DataProviderHandler asyncData)
		{
			var timeout = TimeSpan.FromSeconds(30); // Arbitrary value that should be validated against UWP
			var request = new DataProviderRequest(format, DateTimeOffset.Now + timeout); // We create teh request before the ct, so we ensure that Deadline will be before the actual timeout

			using var ct = new CancellationTokenSource(timeout);
			using (ct.Token.Register(request.Abort))
			{
				asyncData(request);

				var data = await request.GetData();

				return (TResult)data;
			}
		}

		public void ReportOperationCompleted(DataPackageOperation value)
			=> _reportCompleted(_acceptedFormatId, value); // The event can be raised multiple times!

		public void SetAcceptedFormatId(string formatId)
			=> _acceptedFormatId = formatId;
	}
}
#endif
