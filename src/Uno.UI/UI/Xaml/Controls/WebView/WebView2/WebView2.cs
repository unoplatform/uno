#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Uno.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.UI.Core;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Represents an object that enables the hosting of web content.
/// </summary>
public partial class WebView2 : Control, IWebView
{
	private bool _sourceChangeFromCore;
	private bool _coreWebView2Initialized;

	/// <summary>
	/// Initializes a new instance of the WebView2 class.
	/// </summary>
	public WebView2()
	{
		DefaultStyleKey = typeof(WebView2);

		CoreWebView2 = new CoreWebView2(this);
		CoreWebView2.HistoryChanged += CoreWebView2_HistoryChanged;
		CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;
		CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
		CoreWebView2.SourceChanged += CoreWebView2_SourceChanged;
		CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

		Loaded += WebView2_Loaded;
#if __SKIA__
		Unloaded += WebView2_Unloaded;
#endif
	}

	public CoreWebView2 CoreWebView2 { get; }

	bool IWebView.IsLoaded => IsLoaded;

	bool IWebView.SwitchSourceBeforeNavigating => false; // WebView2 switches source only when navigation completes.

	CoreDispatcher IWebView.Dispatcher => Dispatcher;

	protected override void OnApplyTemplate() => CoreWebView2.OnOwnerApplyTemplate();

	private void WebView2_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
	{
		if (!_coreWebView2Initialized)
		{
			EnsureCoreWebView2();
		}

#if __SKIA__
		CoreWebView2.OnLoaded();
#endif
	}

#if __SKIA__
	private void WebView2_Unloaded(object sender, RoutedEventArgs e) => CoreWebView2?.OnUnloaded();
#endif

	public IAsyncAction EnsureCoreWebView2Async() =>
		AsyncAction.FromTask(async ct =>
		{
			if (!_coreWebView2Initialized)
			{
				EnsureCoreWebView2();
			}

			await CoreWebView2.EnsureNativeWebViewAsync();
		});

	/// <summary>
	/// Initializes the underlying <see cref="Microsoft.Web.WebView2.Core.CoreWebView2"/> with <paramref name="environment"/>.
	/// </summary>
	/// <remarks>
	/// <see cref="Microsoft.Web.WebView2.Core.CoreWebView2.Environment"/> returns <paramref name="environment"/>, unless an environment was already
	/// chosen. The native WebView is not created from it: platforms ignore its options unless they read them, and Win32
	/// creates its own environment from <c>FeatureConfiguration.WebView2</c>.
	/// </remarks>
	/// <exception cref="ArgumentNullException"><paramref name="environment"/> is null.</exception>
	public IAsyncAction EnsureCoreWebView2Async(CoreWebView2Environment environment)
	{
		ArgumentNullException.ThrowIfNull(environment);

		CoreWebView2.AdoptEnvironment(environment);

		return EnsureCoreWebView2Async();
	}

	/// <summary>
	/// Initializes the underlying <see cref="Microsoft.Web.WebView2.Core.CoreWebView2"/> with <paramref name="environment"/>.
	/// </summary>
	/// <remarks>
	/// Behaves like <see cref="EnsureCoreWebView2Async(CoreWebView2Environment)"/>; <paramref name="controllerOptions"/> is ignored.
	/// </remarks>
	/// <exception cref="ArgumentNullException"><paramref name="environment"/> is null.</exception>
	public IAsyncAction EnsureCoreWebView2Async(CoreWebView2Environment environment, CoreWebView2ControllerOptions? controllerOptions)
		=> EnsureCoreWebView2Async(environment);

	public IAsyncOperation<string?> ExecuteScriptAsync(string javascriptCode) =>
		CoreWebView2.ExecuteScriptAsync(javascriptCode);

	public void Reload() => CoreWebView2.Reload();

	public void GoForward() => CoreWebView2.GoForward();

	public void GoBack() => CoreWebView2.GoBack();

	public void NavigateToString(string htmlContent) => CoreWebView2.NavigateToString(htmlContent);

	private void EnsureCoreWebView2()
	{
		if (!_coreWebView2Initialized)
		{
			CoreWebView2Initialized?.Invoke(this, new());
			_coreWebView2Initialized = true;
		}
	}

	private void CoreWebView2_NavigationStarting(CoreWebView2 sender, CoreWebView2NavigationStartingEventArgs args) =>
		NavigationStarting?.Invoke(this, args);

	private void CoreWebView2_NavigationCompleted(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs args) =>
		NavigationCompleted?.Invoke(this, args);

	private void CoreWebView2_HistoryChanged(CoreWebView2 sender, object args) =>
		(CanGoBack, CanGoForward) = (sender.CanGoBack, sender.CanGoForward);

	private void CoreWebView2_WebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args) =>
		WebMessageReceived?.Invoke(this, args);

	private void CoreWebView2_SourceChanged(CoreWebView2 sender, CoreWebView2SourceChangedEventArgs args)
	{
		_sourceChangeFromCore = true;
		Source = Uri.TryCreate(sender.Source, UriKind.Absolute, out var uri) ? uri : CoreWebView2.BlankUri;
		_sourceChangeFromCore = false;
	}

	protected override Automation.Peers.AutomationPeer OnCreateAutomationPeer()
		=> new Automation.Peers.WebView2AutomationPeer(this);
}
