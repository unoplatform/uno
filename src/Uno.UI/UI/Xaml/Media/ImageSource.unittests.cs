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
using Windows.UI.Xaml.Media.Imaging;

#if !IS_UNO
using Uno.Web.Query;
using Uno.Web.Query.Cache;
#endif

namespace Windows.UI.Xaml.Media;

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
