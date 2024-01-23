using Uno.Extensions;
using Uno.Foundation.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Uno;
using Uno.Diagnostics.Eventing;
using Uno.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

#if !IS_UNO
using Uno.Web.Query;
using Uno.Web.Query.Cache;
#endif

namespace Microsoft.UI.Xaml.Media
{
	partial class ImageSource
	{
		partial void InitFromResource(Uri uri)
		{
			AbsoluteUri = uri;
		}

		partial void CleanupResource()
		{
			AbsoluteUri = null;
		}

		#region Implementers API
		/// <summary>
		/// Override to provide the capability of concrete ImageSource to open synchronously.
		/// </summary>
		/// <param name="targetWidth">The width of the image that will render this ImageSource.</param>
		/// <param name="targetHeight">The width of the image that will render this ImageSource.</param>
		/// <param name="image">Returned image data.</param>
		/// <returns>True if opening synchronously is possible.</returns>
		/// <remarks>
		/// <paramref name="targetWidth"/> and <paramref name="targetHeight"/> can be used to improve performance by fetching / decoding only the required size.
		/// Depending on stretching, only one of each can be provided.
		/// </remarks>
		private protected virtual bool TryOpenSourceSync(int? targetWidth, int? targetHeight, out ImageData image)
		{
			image = default;
			return false;
		}

		/// <summary>
		/// Override to provide the capability of concrete ImageSource to open asynchronously.
		/// </summary>
		/// <param name="targetWidth">The width of the image that will render this ImageSource.</param>
		/// <param name="targetHeight">The width of the image that will render this ImageSource.</param>
		/// <param name="asyncImage">Async task for image data retrieval.</param>
		/// <returns>True if opening asynchronously is possible.</returns>
		/// <remarks>
		/// <paramref name="targetWidth"/> and <paramref name="targetHeight"/> can be used to improve performance by fetching / decoding only the required size.
		/// Depending on stretching, only one of each can be provided.
		/// </remarks>
		private protected virtual bool TryOpenSourceAsync(CancellationToken ct, int? targetWidth, int? targetHeight, [NotNullWhen(true)] out Task<ImageData> asyncImage)
		{
			asyncImage = default;
			return false;
		}
		#endregion
	}
}
