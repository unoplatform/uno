using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CefSharp;
using CefSharp.Wpf;
using Windows.UI.Xaml;

namespace Uno.UI.WpfHost
{
	public class UnoHostView : Control
	{
		private static Action _appInitializer;
		private static string _distFolder;
		private ChromiumWebBrowser _browser;
		private DefaultResourceHandlerFactory _handlerFactory;

		public UnoHostView()
		{
			DefaultStyleKey = typeof(UnoHostView);
		}

		public static void Init(
			Action appInitializer,
			string distFolder,
			Action<CefSettings> settingsBuilder = null,
			bool performDependencyCheck = true,
			IBrowserProcessHandler browserProcessHandler = null
		)
		{
			_appInitializer = appInitializer;
			_distFolder = distFolder;

			CefSharpSettings.WcfEnabled = true;

			var settings = new CefSettings()
			{
				//By default CefSharp will use an in-memory cache, you need to specify a Cache Folder to persist data
				CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CefSharp\\Cache")
			};

			settingsBuilder?.Invoke(settings);

			//Perform dependency check to make sure all relevant resources are in our output directory.
			Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);
		}

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			if (GetTemplateChild("PART_Browser") is ChromiumWebBrowser browser)
			{
				_browser = browser;

				_browser.FrameLoadEnd += Browser_FrameLoadEnd;
				_browser.IsBrowserInitializedChanged += Browser_IsBrowserInitializedChanged;
				_browser.ConsoleMessage += Browser_ConsoleMessage;
				_browser.DownloadHandler = new UnoDownloadHandler();
				_browser.ResourceHandlerFactory = _handlerFactory = new UnoResourceHandlerFactory();

				RegisterResourceHandlers();

				_browser.JavascriptObjectRepository.ResolveObject += (sender, e) =>
				{
					var repo = e.ObjectRepository;

					if (e.ObjectName == "UnoDispatch")
					{
						repo.Register("UnoDispatch", new UnoDispatch(), options: BindingOptions.DefaultBinder);
					}
				};

				var loop = new System.Reactive.Concurrency.EventLoopScheduler();
				Windows.UI.Core.CoreDispatcher.DispatchOverride = d => loop.Schedule(d);

				Uno.Foundation.WebAssemblyRuntime.InvokeJSOverride = InvokeJS;
			}
			else
			{
				throw new InvalidOperationException("Unable to find PART_Browser");
			}
		}

		private class UnoResourceHandlerFactory : DefaultResourceHandlerFactory
		{
			public override IResourceHandler GetResourceHandler(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request)
			{
				Debug.WriteLine($"Requesting handler for {request.Url}");

				return base.GetResourceHandler(browserControl, browser, frame, request);
			}
		}

		private class UnoDownloadHandler : IDownloadHandler
		{
			public void OnBeforeDownload(IWebBrowser chromiumWebBrowser, IBrowser browser, DownloadItem downloadItem, IBeforeDownloadCallback callback)
			{
				Debug.WriteLine("Downloading: " + downloadItem.FullPath);
			}

			public void OnDownloadUpdated(IWebBrowser chromiumWebBrowser, IBrowser browser, DownloadItem downloadItem, IDownloadItemCallback callback)
			{
				Debug.WriteLine("Downloaded: " + downloadItem);
			}
		}

		private void RegisterResourceHandlers()
		{
			var validResources = new[] { "WasmScripts", "WasmCSS" };

			foreach (var file in Directory.GetFiles(_distFolder, "*.*"))
			{
				if (file.EndsWith("mono.js"))
				{
					_handlerFactory.RegisterHandler(
						"http://localhost/mono.js",
						ResourceHandler.FromString("")
					);
				}
				else
				{
					string url = "http://localhost/" + file.Replace(_distFolder, "").TrimStart('\\');
					_handlerFactory.RegisterHandler(
						url,
						ResourceHandler.FromFilePath(file, autoDisposeStream: true)
					);

					Debug.WriteLine($"Registered handler for {url}");
				}
			}
		}

		private void Browser_ConsoleMessage(object sender, ConsoleMessageEventArgs e)
		{
			Debug.WriteLine(e.Message);
		}

		private void Browser_IsBrowserInitializedChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
		{
			_browser.LoadHtml(File.ReadAllText(Path.Combine(_distFolder, "index.html")), "http://localhost/index.html");
		}

		private async void Browser_FrameLoadEnd(object sender, CefSharp.FrameLoadEndEventArgs e)
		{
			await Task.Yield();

			var dependencies = Directory.GetFiles(_distFolder, "*.js").Select(f => $"\"{Path.GetFileName(f)}\"");
			await _browser.EvaluateScriptAsync("eval", $"require ([{string.Join(",", dependencies)}]);");

			_appInitializer();

			var ret = await _browser.EvaluateScriptAsync("eval", "CefSharp.BindObjectAsync({ NotifyIfAlreadyBound: false }, \"UnoDispatch\")");

		}

		private string InvokeJS(string s2)
		{
			var tcs = new TaskCompletionSource<string>();

			async Task Invoke()
			{
				try
				{
					// Console.WriteLine("Eval: " + s2);
					var ret = await _browser.EvaluateScriptAsync(s2);

					if (ret.Success)
					{
						// Console.WriteLine("EvalSucess: " + ret.Message);
						tcs.TrySetResult(ret.Result?.ToString());
					}
					else
					{
						// Console.WriteLine("EvalFail: " + ret.Message);
						tcs.TrySetException(new Exception(ret.Result?.ToString()));
					}
				}
				catch (Exception ie)
				{
					// Console.WriteLine(ie);
					tcs.TrySetException(ie);
				}
			}

			if (!_browser.Dispatcher.CheckAccess())
			{
				_browser.Dispatcher.Invoke(
					(Action)(async () =>
					{
						await Invoke();
					})
				);
			}
			else
			{
				var d = Invoke();
			}

			return tcs.Task.Result;
		}
	}

	public class UnoDispatch
	{
		public UnoDispatch()
		{
		}

		public void Resize(string newSize)
		{
			var d = Windows.UI.Core.CoreDispatcher.Main.RunAsync(
				Windows.UI.Core.CoreDispatcherPriority.Normal,
				() =>
				{
					var sizeParts = newSize.Split(';');

					// Console.WriteLine($"Resize {size}");
					Windows.UI.Xaml.Window.Resize(double.Parse(sizeParts[0]), double.Parse(sizeParts[1]));
				});
		}

		public void Dispatch(string htmlIdStr, string eventNameStr, string eventPayloadStr)
		{
			var d = Windows.UI.Core.CoreDispatcher.Main.RunAsync(
				Windows.UI.Core.CoreDispatcherPriority.Normal,
				() =>
				{
                    // parse htmlId to IntPtr
                    if (int.TryParse(htmlIdStr, out var handle))
                    {
                        // Console.WriteLine($"Dispatch {htmlIdStr} {eventNameStr} {eventPayloadStr}");
                        Windows.UI.Xaml.UIElement.DispatchEvent(handle, eventNameStr, eventPayloadStr);
                    }
                    else
                    {
                        Console.WriteLine("Failed to parse htmlIdStr");
                    }
				});
		}

	}
}
