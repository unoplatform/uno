using System;

namespace Microsoft.UI.Xaml.Controls
{
	public sealed partial class WebViewUnsupportedUriSchemeIdentifiedEventArgs
	{
		public WebViewUnsupportedUriSchemeIdentifiedEventArgs(Uri uri)
		{
			Uri = uri;
		}

		public bool Handled
		{
			get;
			set;
		}

		public Uri Uri
		{
			get; private set;
		}
	}
}
