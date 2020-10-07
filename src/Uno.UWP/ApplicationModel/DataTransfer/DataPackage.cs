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
using Uno;
using Uno.Extensions;
using Uno.Logging;

namespace Windows.ApplicationModel.DataTransfer
{
	public partial class DataPackage
	{
		public event TypedEventHandler<DataPackage, OperationCompletedEventArgs>? OperationCompleted;

		private ImmutableDictionary<string, object> _data = ImmutableDictionary<string, object>.Empty;

		public DataPackageOperation RequestedOperation { get; set; }

		public IDictionary<string, RandomAccessStreamReference> ResourceMap { get; } = new Dictionary<string, RandomAccessStreamReference>();

		public void SetData(string formatId, object value)
		{
			ImmutableInterlocked.Update(ref _data, SetDataCore, (formatId, value));

			ImmutableDictionary<string, object> SetDataCore(ImmutableDictionary<string, object> current, (string id, object v) arg)
				=> current.SetItem(arg.id, arg.v);
		}

		internal void SetDataProvider(string formatId, FuncAsync<object> delayRenderer)
		{
			SetData(formatId, new DataProviderHandler(SetDataCore));

			async void SetDataCore(DataProviderRequest request)
			{
				var deferral = request.GetDeferral();
				try
				{
					await delayRenderer(request.CancellationToken);
				}
				catch (Exception e)
				{
					this.Log().Error($"Failed to asynchronously retrieve the data for od '{formatId}'", e);
				}
				finally
				{
					deferral.Complete();
				}
			}
		}

		public void SetDataProvider(string formatId, DataProviderHandler delayRenderer)
			=> SetData(formatId, delayRenderer);

		public void SetWebLink(Uri value)
			=> SetData(StandardDataFormats.WebLink, value ?? throw new ArgumentNullException("Cannot set DataPackage.WebLink to null"));

		public void SetApplicationLink(global::System.Uri value)
			=> SetData(StandardDataFormats.ApplicationLink, value ?? throw new ArgumentNullException("Cannot set DataPackage.ApplicationLink to null"));

		public void SetText(string value)
			=> SetData(StandardDataFormats.Text, value ?? throw new ArgumentNullException("Text can't be null"));

		public void SetUri(Uri value)
			=> SetData(StandardDataFormats.Uri, value ?? throw new ArgumentNullException("Cannot set DataPackage.Uri to null"));

		public void SetHtmlFormat(string value)
			=> SetData(StandardDataFormats.Html, value ?? throw new ArgumentNullException("Cannot set DataPackage.Html to null"));

		public void SetRtf(string value)
			=> SetData(StandardDataFormats.Rtf, value ?? throw new ArgumentNullException("Cannot set DataPackage.Rtf to null"));

		public void SetBitmap(RandomAccessStreamReference value)
			=> SetData(StandardDataFormats.Bitmap, value ?? throw new ArgumentNullException("Cannot set DataPackage.Bitmap to null"));

		public void SetStorageItems(IEnumerable<IStorageItem> value)
			=> SetData(StandardDataFormats.StorageItems, (value ?? throw new ArgumentNullException("Cannot set DataPackage.StorageItems to null")).ToList() as IReadOnlyList<IStorageItem>);

		public void SetStorageItems(IEnumerable<IStorageItem> value, bool readOnly)
			=> SetData(StandardDataFormats.StorageItems, (value ?? throw new ArgumentNullException("Cannot set DataPackage.StorageItems to null")).ToList() as IReadOnlyList<IStorageItem>);

		public DataPackageView GetView()
			=> new DataPackageView(
				RequestedOperation,
				_data,
				ResourceMap.ToImmutableDictionary(),
				(id, op) => OperationCompleted?.Invoke(this, new OperationCompletedEventArgs(id, op)));
	}
}
#endif
