#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media;
using Microsoft.Web.WebView2.Core;
using Uno.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Core;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Represents an object that enables the hosting of web content.
/// </summary>
#if IS_UNIT_TESTS || __SKIA__ || __NETSTD_REFERENCE__
[Uno.NotImplemented("IS_UNIT_TESTS", "__SKIA__", "__NETSTD_REFERENCE__")]
#endif
public partial class WebView2 : Control, IWebView
{
	private bool _sourceChangeFromCore;
	private bool _isClosed;
	private int _coreWebView2InitializationCompleted;
	private CoreWebView2? _coreWebView2;

	/// <summary>
	/// Initializes a new instance of the WebView2 class.
	/// </summary>
	public WebView2()
	{
		DefaultStyleKey = typeof(WebView2);
		FlowDirection = FlowDirection.LeftToRight;
		IsTabStop = true;
		Background = new SolidColorBrush(Colors.Transparent);

		var coreWebView2 = _coreWebView2 = new CoreWebView2(this);
		coreWebView2.HistoryChanged += CoreWebView2_HistoryChanged;
		coreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;
		coreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
		coreWebView2.SourceChanged += CoreWebView2_SourceChanged;
		coreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

		Loaded += WebView2_Loaded;
#if __SKIA__
		Unloaded += WebView2_Unloaded;
#endif
	}

	// WinRT projections expose this as non-nullable even though WinUI clears the
	// reference after Close(). Keep the projected signature while preserving that
	// runtime behavior.
	public CoreWebView2 CoreWebView2 => _coreWebView2!;

	private CoreWebView2 CoreWebView2OrThrow =>
		_coreWebView2 ?? throw new ObjectDisposedException(nameof(WebView2));

	bool IWebView.IsLoaded => IsLoaded;

	bool IWebView.RequiresExplicitInitialization => true;

	bool IWebView.SwitchSourceBeforeNavigating => false; // WebView2 switches source only when navigation completes.

	CoreDispatcher IWebView.Dispatcher => Dispatcher;

	protected override void OnApplyTemplate() => _coreWebView2?.OnOwnerApplyTemplate();

	private void WebView2_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
	{
		if (_isClosed)
		{
			return;
		}

#if __SKIA__
		_coreWebView2?.OnLoaded();
#endif
	}

#if __SKIA__
	private void WebView2_Unloaded(object sender, RoutedEventArgs e) => CoreWebView2?.OnUnloaded();
#endif

	public IAsyncAction EnsureCoreWebView2Async() =>
		AsyncAction.FromTask(async ct =>
		{
			ThrowIfClosed();
			await EnsureCoreWebView2CoreAsync();
		});

	/// <summary>
	/// Initializes CoreWebView2 with a custom environment.
	/// </summary>
	public IAsyncAction EnsureCoreWebView2Async(CoreWebView2Environment? environment) =>
		EnsureCoreWebView2Async(environment, controllerOptions: null);

	/// <summary>
	/// Initializes CoreWebView2 with a custom environment and controller options.
	/// </summary>
	public IAsyncAction EnsureCoreWebView2Async(CoreWebView2Environment? environment, CoreWebView2ControllerOptions? controllerOptions) =>
		AsyncAction.FromTask(async ct =>
		{
			ThrowIfClosed();
			CoreWebView2OrThrow.SetCustomEnvironment(environment, controllerOptions);
			await EnsureCoreWebView2CoreAsync();
		});

	public IAsyncOperation<string?> ExecuteScriptAsync(string javascriptCode)
	{
		ThrowIfClosed();
		if (!CoreWebView2OrThrow.HasNativeWebView)
		{
			throw new InvalidOperationException("ExecuteScriptAsync requires an initialized CoreWebView2. Call EnsureCoreWebView2Async first.");
		}

		return CoreWebView2OrThrow.ExecuteScriptAsync(javascriptCode);
	}

	public void Reload()
	{
		ThrowIfClosed();
		if (!CoreWebView2OrThrow.HasNativeWebView)
		{
			throw new InvalidOperationException("Reload requires an initialized CoreWebView2. Call EnsureCoreWebView2Async first.");
		}

		CoreWebView2OrThrow.Reload();
	}

	public void GoForward() => CoreWebView2OrThrow.GoForward();

	public void GoBack() => CoreWebView2OrThrow.GoBack();

	public void NavigateToString(string htmlContent)
	{
		ThrowIfClosed();
		if (!CoreWebView2OrThrow.HasNativeWebView)
		{
			throw new InvalidOperationException("NavigateToString requires an initialized CoreWebView2. Call EnsureCoreWebView2Async first.");
		}

		CoreWebView2OrThrow.NavigateToString(htmlContent);
	}

	/// <summary>
	/// Closes this WebView2 and releases its native web-view resources.
	/// </summary>
	public void Close()
	{
		if (_isClosed)
		{
			return;
		}

		_isClosed = true;
		var coreWebView2 = _coreWebView2;
		_coreWebView2 = null;
		if (coreWebView2 is not null)
		{
			coreWebView2.NavigationStarting -= CoreWebView2_NavigationStarting;
			coreWebView2.NavigationCompleted -= CoreWebView2_NavigationCompleted;
			coreWebView2.SourceChanged -= CoreWebView2_SourceChanged;
			coreWebView2.WebMessageReceived -= CoreWebView2_WebMessageReceived;
			coreWebView2.Close();
		}
		(CanGoBack, CanGoForward) = (false, false);
	}

	private void EnsureCoreWebView2Implicitly()
	{
		_ = EnsureCoreWebView2ImplicitlyAsync();
	}

	private async Task EnsureCoreWebView2ImplicitlyAsync()
	{
		try
		{
			await EnsureCoreWebView2CoreAsync();
		}
		catch
		{
			// Initialization failures from Source-driven creation are reported through
			// CoreWebView2InitializedEventArgs.Exception.
		}
	}

	private async Task EnsureCoreWebView2CoreAsync()
	{
		try
		{
			await CoreWebView2OrThrow.EnsureNativeWebViewAsync();
			CompleteCoreWebView2Initialization(null);
		}
		catch (Exception error)
		{
			CompleteCoreWebView2Initialization(error);
			throw;
		}
	}

	private void CompleteCoreWebView2Initialization(Exception? error)
	{
		if (Interlocked.Exchange(ref _coreWebView2InitializationCompleted, 1) == 0)
		{
			CoreWebView2Initialized?.Invoke(this, new CoreWebView2InitializedEventArgs(error));
		}
	}

	private void ThrowIfClosed()
	{
		if (_isClosed)
		{
			throw new ObjectDisposedException(nameof(WebView2));
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
