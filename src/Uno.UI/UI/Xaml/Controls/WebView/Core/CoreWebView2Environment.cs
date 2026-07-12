#nullable enable

using System.Threading.Tasks;
using Windows.Foundation;

namespace Microsoft.Web.WebView2.Core;

public partial class CoreWebView2Environment
{
	internal CoreWebView2Environment(string? browserExecutableFolder, string? userDataFolder, CoreWebView2EnvironmentOptions? options)
	{
		BrowserExecutableFolder = browserExecutableFolder;
		UserDataFolder = userDataFolder ?? string.Empty;
		Options = options;
	}

	internal string? BrowserExecutableFolder { get; }

	internal CoreWebView2EnvironmentOptions? Options { get; }

	public string BrowserVersionString { get; internal set; } = string.Empty;

	public string UserDataFolder { get; }

	public static IAsyncOperation<CoreWebView2Environment> CreateAsync() =>
		CreateWithOptionsAsync(browserExecutableFolder: null, userDataFolder: null, options: null);

	public static IAsyncOperation<CoreWebView2Environment> CreateWithOptionsAsync(
		string? browserExecutableFolder,
		string? userDataFolder,
		CoreWebView2EnvironmentOptions? options) =>
		AsyncOperation.FromTask(
			ct => Task.FromResult(new CoreWebView2Environment(browserExecutableFolder, userDataFolder, options)));

	public CoreWebView2ControllerOptions CreateCoreWebView2ControllerOptions() => new();

	public CoreWebView2PrintSettings CreatePrintSettings() => new();
}