#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;

namespace Uno.UI.Xaml.Controls;

internal interface ISupportsBrowsingDataClearing
{
	/// <param name="dataKinds">null clears every kind, matching CoreWebView2Profile.ClearBrowsingDataAsync().</param>
	/// <param name="startTime">null is unbounded in the past.</param>
	/// <param name="endTime">null is unbounded in the future.</param>
	/// <remarks>
	/// An implementer that cannot honour a bounded range must throw <see cref="NotSupportedException"/> rather
	/// than widen it: reporting a narrower deletion than was performed is a privacy defect. Win32 maps all three
	/// shapes onto distinct native calls, so that path is unreachable there.
	/// </remarks>
	Task ClearBrowsingDataAsync(CoreWebView2BrowsingDataKinds? dataKinds, DateTimeOffset? startTime, DateTimeOffset? endTime);
}
