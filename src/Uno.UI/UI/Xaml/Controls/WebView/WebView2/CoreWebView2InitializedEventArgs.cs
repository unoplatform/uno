using System;

namespace Microsoft.UI.Xaml.Controls;

public sealed partial class CoreWebView2InitializedEventArgs
{
	public Exception Exception { get; internal set; }
}
