#nullable enable

using System.Threading.Tasks;

namespace Uno.UI.Xaml.Controls;

internal interface IAsyncNativeWebView
{
	Task InitializeAsync();
}
