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
using Uno;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Dispatching;

namespace Windows.ApplicationModel.DataTransfer
{
	public partial class DataPackage
	{
		/// <summary>
		/// FormatId prefix for internal data that won't be visible to the application
		/// (cf. <see cref="DataPackageView.AvailableFormats"/>).
		/// </summary>
		internal const string UnoPrivateDataPrefix = "__uno__private__data__";

		public event TypedEventHandler<DataPackage, OperationCompletedEventArgs>? OperationCompleted;

#pragma warning disable CS0067
		public event TypedEventHandler<DataPackage, object?>? Destroyed;

		public event TypedEventHandler<DataPackage, object?>? ShareCanceled;

		public event TypedEventHandler<DataPackage, ShareCompletedEventArgs>? ShareCompleted;
#pragma warning restore CS00067

		private ImmutableDictionary<string, object> _data = ImmutableDictionary<string, object>.Empty;

		public DataPackageOperation RequestedOperation { get; set; }

		public IDictionary<string, RandomAccessStreamReference> ResourceMap { get; } = new Dictionary<string, RandomAccessStreamReference>();

		public DataPackagePropertySet Properties { get; } = new DataPackagePropertySet();

		internal bool Contains(string formatId)
		{
			if (formatId is null)
			{
				throw new ArgumentNullException(nameof(formatId));
			}

			return _data.ContainsKey(formatId);
		}

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
					var data = await delayRenderer(request.CancellationToken);
					request.SetData(data);
				}
				catch (Exception e)
				{
					this.Log().Error($"Failed to asynchronously retrieve the data for id '{formatId}'", e);
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
				(id, op) => OperationCompleted?.Invoke(this, new OperationCompletedEventArgs(id, op)),
				Properties);

		/// <summary>
		/// Determines if the given URI/URL is considered a WebLink.
		/// This determination is done based on whether the scheme starts with 'http' or 'https'.
		/// </summary>
		/// <remarks>
		/// This method is intended for use during integration with other platforms.
		/// Therefore, it uses a string instead of a Uri or native URL class.
		/// </remarks>
		/// <param name="uri">The URI to determine if it is a WebLink.</param>
		/// <returns>True if the given URI/URL is considered a WebLink; otherwise, false.</returns>
		internal static bool IsUriWebLink(string? uri)
		{
			uri = uri?.Trim();

			if (string.IsNullOrEmpty(uri) == false)
			{
				if (uri!.StartsWith("http", StringComparison.InvariantCultureIgnoreCase) ||
					uri!.StartsWith("https", StringComparison.InvariantCultureIgnoreCase))
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Separates a single URI/URL into separate URI's for WebLink and ApplicationLink (Uri format is deprecated).
		/// This is useful for converting a platform-specific URI/URL into formats supported by the <see cref="DataPackage"/>.
		/// </summary>
		/// <remarks>
		/// This method is intended for use during integration with other platforms.
		/// Therefore, it uses strings as the intermediate type instead of a Uri or native URL class.
		/// </remarks>
		/// <param name="uri">The platform-specific URI/URL.</param>
		/// <param name="webLink">
		/// The <see cref="StandardDataFormats.WebLink"/> format Uri intended for use in a <see cref="DataPackage"/>.
		/// Null will be returned if the URI/URL is not in the <see cref="StandardDataFormats.WebLink"/> format.
		/// </param>
		/// <param name="applicationLink">
		/// The <see cref="StandardDataFormats.ApplicationLink"/> format Uri intended for use in a <see cref="DataPackage"/>.
		/// Null will be returned if the URI/URL is not in the <see cref="StandardDataFormats.ApplicationLink"/> format.
		/// </param>
		internal static void SeparateUri(string? uri, out string? webLink, out string? applicationLink)
		{
			/* UWP has the following standard data formats that correspond with a URI/URL:
			 *
			 *  1. Uri, now deprecated in favor of:
			 *  2. WebLink and
			 *  3. ApplicationLink
			 *
			 * Several platforms such as macOS/iOS/Android do not differentiate between them and
			 * only use a single URI/URL class or string.
			 * 
			 * Therefore, this method is used to map between a native URI/URL and the equivalent
			 * UWP data type (since UWP's direct equivalent standard data format 'Uri' is deprecated).
			 * 
			 * The mapping is as follows:
			 * 
			 * 1. WebLink is used if the given URL/URI has a scheme of "http" or "https"
			 * 2. ApplicationLink is used if not #1
			 *
			 * For full compatibility, the Uri format within a DataPackage should still be populated
			 * regardless of #1 or #2.
			 */

			uri = uri?.Trim();

			if (string.IsNullOrEmpty(uri))
			{
				webLink = null;
				applicationLink = null;
			}
			else if (IsUriWebLink(uri))
			{
				webLink = uri;
				applicationLink = null;
			}
			else
			{
				webLink = null;
				applicationLink = uri;
			}

			return;
		}

		/// <summary>
		/// Combines separate URI's for WebLink, ApplicationLink and Uri (deprecated) into a single URI/URL.
		/// This is useful for converting formats supported by the <see cref="DataPackage"/> into a platform-specific URI/URL.
		/// </summary>
		/// <remarks>
		/// This method is intended for use during integration with other platforms.
		/// Therefore, it uses strings as the intermediate type instead of a Uri or native URL class.
		/// </remarks>
		/// <param name="webLink">
		/// Data from a <see cref="DataPackage"/> for the <see cref="StandardDataFormats.WebLink"/> format.
		/// Set to null if this format does not exist in the package.
		/// </param>
		/// <param name="applicationLink">
		/// Data from a <see cref="DataPackage"/> for the <see cref="StandardDataFormats.ApplicationLink"/> format.
		/// Set to null if this format does not exist in the package.
		/// </param>
		/// <param name="uri">
		/// Data from a <see cref="DataPackage"/> for the <see cref="StandardDataFormats.Uri"/> format.
		/// Set to null if this format does not exist in the package.
		/// </param>
		/// <returns></returns>
		internal static string CombineUri(string? webLink, string? applicationLink, string? uri)
		{
			/* UWP has the following standard data formats that correspond with a URI/URL:
			 *
			 *  1. Uri, now deprecated in favor of:
			 *  2. WebLink and
			 *  3. ApplicationLink
			 *
			 * Several platforms such as macOS/iOS/Android do not differentiate between them and
			 * only use a single URI/URL class or string.
			 *
			 * When applying data to the native clipboard or drag/drop data from a DataPackage, 
			 * only one URI/URL may be used. Therefore, all URI data formats are merged together
			 * into a single URI using the above defined priority. 
			 * 
			 * WebLink is considered more specific than ApplicationLink.
			 */

			string combinedUri = string.Empty;

			webLink = webLink?.Trim();
			applicationLink = applicationLink?.Trim();
			uri = uri?.Trim();

			if (string.IsNullOrEmpty(uri) == false)
			{
				combinedUri = uri!;
			}
			else if (string.IsNullOrEmpty(webLink) == false)
			{
				combinedUri = webLink!;
			}
			else if (string.IsNullOrEmpty(applicationLink) == false)
			{
				combinedUri = applicationLink!;
			}

			return combinedUri;
		}

		internal void OnShareCompleted() => ShareCompleted?.Invoke(this, new ShareCompletedEventArgs());

		internal void OnShareCanceled() => ShareCanceled?.Invoke(this, null);

		~DataPackage()
		{
			GC.SuppressFinalize(this);
			NativeDispatcher.Main.Enqueue(() => Destroyed?.Invoke(this, null));
		}
	}
}
