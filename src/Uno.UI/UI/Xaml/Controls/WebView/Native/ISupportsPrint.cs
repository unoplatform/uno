#nullable enable

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// Implemented by native WebView providers that can capture the current page as
/// a PDF stream or trigger the system print UI.
/// </summary>
internal interface ISupportsPrint
{
	Task<Stream> PrintToPdfStreamAsync(CoreWebView2PrintSettings? settings, CancellationToken ct);

	Task<CoreWebView2PrintStatus> ShowPrintUIAsync(CoreWebView2PrintDialogKind dialogKind, CancellationToken ct);
}
