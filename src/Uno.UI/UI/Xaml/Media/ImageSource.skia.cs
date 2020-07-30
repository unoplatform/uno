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
using System.Runtime.CompilerServices;
using Uno;
using Uno.Diagnostics.Eventing;
using Windows.UI.Xaml.Media.Imaging;

namespace Windows.UI.Xaml.Media
{
	partial class ImageSource 
	{
		protected ImageSource()
		{

		}

		partial void InitFromResource(Uri uri)
		{
			WebUri = uri;
		}

		internal void UnloadImageData() { }
	}
}
