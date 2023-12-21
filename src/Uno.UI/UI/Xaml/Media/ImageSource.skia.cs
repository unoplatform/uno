using Uno.Extensions;
using Uno.Foundation.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Uno;
using Uno.Diagnostics.Eventing;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Microsoft.UI.Xaml.Media
{
	partial class ImageSource
	{
		protected ImageSource()
		{

		}

		partial void InitFromResource(Uri uri)
		{
			AbsoluteUri = uri;
		}

		partial void CleanupResource()
		{
			AbsoluteUri = null;
		}
	}
}
