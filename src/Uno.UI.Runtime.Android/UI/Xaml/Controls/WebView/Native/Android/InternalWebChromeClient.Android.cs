#nullable disable

using System;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.OS;
using Android.Webkit;
using Microsoft.Web.WebView2.Core;
using Uno.Disposables;
using Uno.Foundation.Logging;

namespace Uno.UI.Xaml.Controls;

internal class InternalWebChromeClient : WebChromeClient
{
	private const string ActivityLaunchIdExtra = "Uno.WebView.FileChooserLaunchId";
	private readonly CoreWebView2 _coreWebView;

	public InternalWebChromeClient(CoreWebView2 coreWebView)
	{
		_coreWebView = coreWebView;
	}

	private IValueCallback _filePathCallback;

	readonly SerialDisposable _fileChooserTaskDisposable = new SerialDisposable();

	public override bool OnShowFileChooser(
		Android.Webkit.WebView webView,
		IValueCallback filePathCallback,
		FileChooserParams fileChooserParams)
	{
		_filePathCallback = filePathCallback;

		var cancellationDisposable = new CancellationDisposable();
		_fileChooserTaskDisposable.Disposable = cancellationDisposable;
		var cancellationToken = cancellationDisposable.Token;

		Task.Run(async () =>
		{
			try
			{
				await StartFileChooser(cancellationToken, fileChooserParams);
			}
			catch (System.OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
			}
			catch (Exception e)
			{
				this.Log().Error(e.Message, e);
			}
		});

		return true;
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_fileChooserTaskDisposable.Dispose();
			Interlocked.Exchange(ref _filePathCallback, null)?.OnReceiveValue(null);
		}
		base.Dispose(disposing);
	}

	public override void OnPermissionRequest(PermissionRequest request) => request.Grant(request.GetResources());

	public override bool OnCreateWindow(WebView view, bool isDialog, bool isUserGesture, Message resultMsg)
	{
		if (view is null)
		{
			return false;
		}

		var hitTest = view.GetHitTestResult();
		string targetUrl = null;

		if (hitTest != null &&
			(hitTest.Type == Android.Webkit.HitTestResult.SrcAnchorType ||
			hitTest.Type == Android.Webkit.HitTestResult.SrcImageAnchorType))
		{
			targetUrl = hitTest.Extra;
		}

		if (!string.IsNullOrEmpty(targetUrl))
		{
			Uri.TryCreate(view.Url, UriKind.Absolute, out var refererUri);

			_coreWebView.RaiseNewWindowRequested(
				targetUrl,
				refererUri ?? CoreWebView2.BlankUri,
				out bool handled);

			if (handled)
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Uses the Activity Tracker to start, then return an Activity
	/// </summary>
	/// <typeparam name="T">A BaseActivity to start</typeparam>
	/// <param name="ct">CancellationToken</param>
	/// <returns>The BaseActivity that just started (OnResume called)</returns>
	private async Task<T> StartActivity<T>(CancellationToken ct) where T : BaseActivity
	{
		ct.ThrowIfCancellationRequested();
		var currentActivity = BaseActivity.Current
			?? throw new InvalidOperationException("A current Android activity is required to open the WebView file chooser.");
		var launch = new WebViewActivityLaunch<T>(ct);
		void OnCurrentActivityChanged(object sender, CurrentActivityChangedEventArgs args)
		{
			if (args.Current is T activity)
			{
				launch.OnActivityCreated(activity, activity.Intent?.GetStringExtra(ActivityLaunchIdExtra), static arrived => arrived.Finish());
			}
		}

		BaseActivity.CurrentChanged += OnCurrentActivityChanged;
		try
		{
			return await launch.StartAsync(id =>
			{
				using var intent = new Intent(currentActivity, typeof(T));
				intent.PutExtra(ActivityLaunchIdExtra, id);
				currentActivity.StartActivity(intent);
			});
		}
		finally
		{
			BaseActivity.CurrentChanged -= OnCurrentActivityChanged;
		}
	}

	private async Task StartFileChooser(CancellationToken ct, FileChooserParams fileChooserParams)
	{
		var intent = fileChooserParams.CreateIntent();
		//Get an invisible (Transparent) Activity to handle the Intent
		var delegateActivity = await StartActivity<DelegateActivity>(ct);

		var result = await delegateActivity.GetActivityResult(ct, intent);

		Interlocked.Exchange(ref _filePathCallback, null)?.OnReceiveValue(FileChooserParams.ParseResult((int)result.ResultCode, result.Intent));
	}
}
