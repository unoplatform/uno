#nullable enable

using System;
using DirectN;
using Microsoft.Web.WebView2.Core;
using Uno.UI.Xaml.Controls;
using Windows.UI;

namespace Uno.UI.Runtime.Skia.Win32;

// Native controller and event-argument projection for the MUX control lifecycle.
internal sealed partial class Win32NativeAotWebView : INativeWebViewController, ICleanableNativeWebView
{
	private WebView2.EventRegistrationToken _processFailedToken;
	private WebView2.EventRegistrationToken _moveFocusRequestedToken;

	public event EventHandler<CoreWebView2MoveFocusRequestedEventArgs>? MoveFocusRequested;

	bool INativeWebViewController.IsVisible
	{
		get
		{
			BOOL visible = default;
			ThrowControllerError(_controller.get_IsVisible(ref visible));
			return visible.Value != 0;
		}
		set => ThrowControllerError(_controller.put_IsVisible(value ? BOOL.TRUE : BOOL.FALSE));
	}

	Color INativeWebViewController.DefaultBackgroundColor
	{
		get
		{
			WebView2.COREWEBVIEW2_COLOR color = default;
			ThrowControllerError(((WebView2.ICoreWebView2Controller2)_controller).get_DefaultBackgroundColor(ref color));
			return Color.FromArgb(color.A, color.R, color.G, color.B);
		}
		set => ThrowControllerError(((WebView2.ICoreWebView2Controller2)_controller).put_DefaultBackgroundColor(
			new WebView2.COREWEBVIEW2_COLOR { A = value.A, R = value.R, G = value.G, B = value.B }));
	}

	double INativeWebViewController.RasterizationScale
	{
		get
		{
			double scale = 1;
			ThrowControllerError(((WebView2.ICoreWebView2Controller3)_controller).get_RasterizationScale(ref scale));
			return scale;
		}
		set
		{
			var controller = (WebView2.ICoreWebView2Controller3)_controller;
			ThrowControllerError(controller.put_ShouldDetectMonitorScaleChanges(BOOL.FALSE));
			ThrowControllerError(controller.put_RasterizationScale(value));
		}
	}

	void INativeWebViewController.MoveFocus(CoreWebView2MoveFocusReason reason) =>
		ThrowControllerError(_controller.MoveFocus((WebView2.COREWEBVIEW2_MOVE_FOCUS_REASON)(int)reason));

	void ICleanableNativeWebView.OnLoaded() => ReleaseTemporaryParent();

	void ICleanableNativeWebView.OnUnloaded()
	{
		// The native element host detaches the HWND; keep the controller alive for reattachment.
	}

	private static void ThrowControllerError(HRESULT result)
	{
		if (result.IsError)
		{
			throw Win32WebView2Environment.GetException(result);
		}
	}

	private void NativeWebView_ProcessFailed(WebView2.ICoreWebView2ProcessFailedEventArgs args)
	{
		WebView2.COREWEBVIEW2_PROCESS_FAILED_KIND kind = default;
		args.get_ProcessFailedKind(ref kind).ThrowOnError();
		var reason = default(CoreWebView2ProcessFailedReason);
		var exitCode = 0;
		var description = string.Empty;
		if (args is WebView2.ICoreWebView2ProcessFailedEventArgs2 details)
		{
			WebView2.COREWEBVIEW2_PROCESS_FAILED_REASON nativeReason = default;
			details.get_Reason(ref nativeReason).ThrowOnError();
			details.get_ExitCode(ref exitCode).ThrowOnError();
			details.get_ProcessDescription(out var nativeDescription).ThrowOnError();
			reason = (CoreWebView2ProcessFailedReason)(int)nativeReason;
			description = nativeDescription.ToStringAndDispose() ?? string.Empty;
		}
		_coreWebView.RaiseProcessFailed(new CoreWebView2ProcessFailedEventArgs(
			(CoreWebView2ProcessFailedKind)(int)kind, reason, exitCode, description));
	}

	private void NativeController_MoveFocusRequested(WebView2.ICoreWebView2MoveFocusRequestedEventArgs args)
	{
		WebView2.COREWEBVIEW2_MOVE_FOCUS_REASON reason = default;
		args.get_Reason(ref reason).ThrowOnError();
		var projectedArgs = new CoreWebView2MoveFocusRequestedEventArgs((CoreWebView2MoveFocusReason)(int)reason);
		MoveFocusRequested?.Invoke(this, projectedArgs);
		args.put_Handled(projectedArgs.Handled ? BOOL.TRUE : BOOL.FALSE).ThrowOnError();
	}
}
