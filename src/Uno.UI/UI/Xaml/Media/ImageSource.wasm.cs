using Uno.Extensions;
using Uno.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls.Primitives;
using Uno;
using Uno.Diagnostics.Eventing;
using Windows.UI.Xaml.Media.Imaging;
using Uno.Disposables;

#if !IS_UNO
using Uno.Web.Query;
using Uno.Web.Query.Cache;
#endif

namespace Windows.UI.Xaml.Media
{
	partial class ImageSource
	{
		private static readonly string UNO_BOOTSTRAP_APP_BASE = global::System.Environment.GetEnvironmentVariable(nameof(UNO_BOOTSTRAP_APP_BASE));

		partial void InitFromResource(Uri uri)
		{
			WebUri = new Uri(uri.PathAndQuery.TrimStart("/"), UriKind.Relative);
		}

		// TODO: All those should be implemented by sub-classes in TryOpenSource<Sync|Async> overloads!
		private bool TryOpenSourceLegacy(out ImageData img)
		{
			var stream = Stream;
			if (stream != null)
			{
				stream.Position = 0;
				var encodedBytes = Convert.ToBase64String(stream.ReadBytes());

				img = new ImageData
				{
					Kind = ImageDataKind.Base64,
					Value = "data:application/octet-stream;base64," + encodedBytes
				};
				return true;
			}

			img = default;
			return false;
		}
	}
}
