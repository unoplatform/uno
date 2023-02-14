using System;
namespace Microsoft.UI.Xaml.Controls
{
	public sealed partial class WebViewNewWindowRequestedEventArgs
	{
		internal WebViewNewWindowRequestedEventArgs(Uri referrer, Uri uri)
		{
			Referrer = referrer;
			Uri = uri;
		}

		public bool Handled
		{
			get; set;
		}

		public Uri Referrer
		{
			get; private set;
		}

		public Uri Uri
		{
			get; private set;
		}
	}
}
