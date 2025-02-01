#nullable enable

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;

namespace Uno.UI.Xaml.Controls;

internal interface ISupportsVirtualHostMapping
{
	void ClearVirtualHostNameToFolderMapping(string hostName);
	void SetVirtualHostNameToFolderMapping(string hostName, string folderPath, CoreWebView2HostResourceAccessKind accessKind);
}
