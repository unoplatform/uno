#nullable enable

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media;
using Microsoft.Web.WebView2.Core;
using Uno;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Helpers.WinUI;
using Uno.UI.Xaml.Controls;
using Windows.UI.Core;

namespace Microsoft.UI.Xaml.Controls;

partial class WebView2 : IWebView
{
	private CoreWebView2? _nativeCore;
	private ContentPresenter? _nativePresenter;
	private readonly SerialDisposable _nativeHistoryRevoker = new();
	private WeakReference<CompositionTarget>? _renderedTarget;

	/// <summary>Gets or sets whether scrolling is enabled in the native web content.</summary>
	[UnoOnly]
	public bool IsScrollEnabled
	{
		get => (bool)GetValue(IsScrollEnabledProperty);
		set => SetValue(IsScrollEnabledProperty, value);
	}

	/// <summary>Identifies the IsScrollEnabled dependency property.</summary>
	[UnoOnly]
	public static DependencyProperty IsScrollEnabledProperty { get; } =
		DependencyProperty.Register(nameof(IsScrollEnabled), typeof(bool), typeof(WebView2),
			new FrameworkPropertyMetadata(true,
				(sender, args) => ((WebView2)sender)._nativeCore?.OnScrollEnabledChanged((bool)args.NewValue)));

	bool IWebView.IsLoaded => IsLoaded;
	bool IWebView.RequiresExplicitInitialization => true;
	bool IWebView.SwitchSourceBeforeNavigating => false;
	CoreDispatcher IWebView.Dispatcher => Dispatcher;

	private void InitializeNativeHost()
	{
		this.SetDefaultStyleKey();
		Loaded += OnNativeHostLoaded;
	}

	private void OnNativeHostLoaded(object sender, RoutedEventArgs args)
	{
		// Hosts without a temporary-window facility need the visual-tree window before native creation.
		if (IsLoaded)
		{
			_nativeCore?.OnLoaded();
		}
	}

	/// <inheritdoc />
	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();
		if (GetTemplateChild("WebViewTemplateRoot") is ContentPresenter presenter)
		{
			if (_nativePresenter is null)
			{
				_nativePresenter = presenter;
			}
			else if (!ReferenceEquals(presenter, _nativePresenter))
			{
				if (VisualTreeHelper.GetParent(_nativePresenter) is ContentPresenter previous)
				{
					previous.Content = null;
				}
				presenter.Content = _nativePresenter;
			}
		}
		_nativeCore?.OnOwnerApplyTemplate();
	}

	internal ContentPresenter? GetNativePresenter()
	{
		EnsureTemplate();
		return _nativePresenter;
	}

	private async Task<CoreWebView2> CreateNativeCoreWebViewAsync(
		CoreWebView2Environment environment, CoreWebView2ControllerOptions? controllerOptions)
	{
		if (m_isClosed)
		{
			throw new ObjectDisposedException(nameof(WebView2));
		}
		var core = _nativeCore = new CoreWebView2(this);
		core.SetCustomEnvironment(environment, controllerOptions);
		core.OnScrollEnabledChanged(IsScrollEnabled);
		await core.EnsureNativeWebViewAsync();
		if (m_isClosed)
		{
			core.Close();
			throw new ObjectDisposedException(nameof(WebView2));
		}
		return core;
	}

	private static bool IsInvalidCoreState(Exception error) =>
		error.HResult == unchecked((int)0x8007139F)
		|| error is Win32Exception { NativeErrorCode: unchecked((int)0x8007139F) or 5023 };

	private static Uri? GetWinRTSourceUri(object? value)
	{
		var uri = (Uri?)value;
		// System.Uri can represent a relative URI; the WinRT Uri projection cannot.
		if (uri is { IsAbsoluteUri: false })
		{
			throw new ArgumentException("The WebView2 Source must be an absolute URI.", nameof(value));
		}
		return uri;
	}

	private static void ValidateNativeCreationArguments(CoreWebView2Environment? environment, CoreWebView2ControllerOptions? options)
	{
		if (environment is not null || options is not null)
		{
			var validationEnvironment = environment is { IsDefaultEnvironment: false }
				? environment
				: new CoreWebView2Environment(null, null, null);
			CoreWebView2.ValidateEnvironmentForCurrentPlatform(validationEnvironment, options);
		}
	}

	private void CloseNativeCore()
	{
		var core = _nativeCore;
		_nativeCore = null;
		core?.Close();
	}

	private void ReleaseFailedNativeCore()
	{
		m_coreWebView = null;
		m_coreWebViewController = null;
		CoreWebView2RunIgnoreInvalidStateSync(CloseNativeCore);
	}

	private void AttachNativeCore() => _nativeCore?.OnLoaded();
	private void DetachNativeCore() => _nativeCore?.OnUnloaded();

	private void RegisterNativeHistoryHandler(CoreWebView2 core, WeakReference<WebView2> weakThis)
	{
		void HistoryChanged(CoreWebView2 sender, object args)
		{
			if (weakThis.TryGetTarget(out var owner) && ReferenceEquals(owner.m_coreWebView, sender))
			{
				owner.SetCanGoBack(sender.CanGoBack);
				owner.SetCanGoForward(sender.CanGoForward);
			}
		}
		core.HistoryChanged += HistoryChanged;
		_nativeHistoryRevoker.Disposable = Disposable.Create(() => core.HistoryChanged -= HistoryChanged);
	}

	private static void ApplyNativeDefaultEnvironmentOptions(CoreWebView2EnvironmentOptions options)
	{
		options.AllowSingleSignOnUsingOSPrimaryAccount = Uno.UI.FeatureConfiguration.WebView2.AllowSingleSignOnUsingOSPrimaryAccount;
		options.AdditionalBrowserArguments = Uno.UI.FeatureConfiguration.WebView2.AdditionalBrowserArguments;
	}

	private async void ObserveSourceChange(Task navigation)
	{
		try
		{
			await navigation;
		}
		catch (Exception error)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error("Unable to navigate WebView2 to its Source.", error);
			}
		}
	}

	private void ClearMissingRuntimeWarning()
	{
		m_shouldShowMissingAnaheimWarning = false;
		if (_nativePresenter?.Content is TextBlock)
		{
			_nativePresenter.Content = null;
		}
	}

	private void ShowMissingRuntimeWarning(TextBlock warning)
	{
		if (GetNativePresenter() is { } presenter)
		{
			presenter.Content = warning;
		}
	}

	private void UpdateNativeLayout()
	{
		// ContentPresenter's native-element host owns HWND/WebKit bounds and window positioning.
	}

	private bool IsRenderedSubscriptionCurrent() =>
		_renderedTarget?.TryGetTarget(out var target) is true && ReferenceEquals(Visual.CompositionTarget, target);

	private IDisposable? SubscribeToRendered()
	{
		if (Visual.CompositionTarget is not CompositionTarget target)
		{
			return null;
		}

		var weakThis = new WeakReference<WebView2>(this);
		void Rendered()
		{
			if (weakThis.TryGetTarget(out var owner))
			{
				owner.HandleRendered(owner, EventArgs.Empty);
			}
		}
		_renderedTarget = new WeakReference<CompositionTarget>(target);
		target.FrameRendered += Rendered;
		return Disposable.Create(() => target.FrameRendered -= Rendered);
	}
}
