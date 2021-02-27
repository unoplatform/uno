#if __WASM__

using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Uno.Foundation;
using System.Net.Http;
using System.IO;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Uno.UI.Xaml;
using Uno.UI.Web;

namespace Windows.UI.Xaml.Controls
{
	public partial class NativeWebView : FrameworkElement
	{
		static readonly Guid SessionGuid = Guid.NewGuid();
		static readonly string Location;
		static readonly string PackageLocation;

		static NativeWebView()
		{
			WebAssemblyRuntime.InvokeJS($"sessionStorage.setItem('Uno.WebView.Session','{SessionGuid}');");
			Location = WebAssemblyRuntime.InvokeJS("window.location.href");
			PackageLocation = WebAssemblyRuntime.InvokeJS("window.scriptDirectory");
		}

		static Dictionary<string, WeakReference<NativeWebView>> Instances = new Dictionary<string, WeakReference<NativeWebView>>();
		static Dictionary<string, TaskCompletionSource<string>> TCSs = new Dictionary<string, TaskCompletionSource<string>>();

		static NativeWebView InstanceForGuid(string guid)
		{
			if (Instances.TryGetValue(guid, out var weakRef))
			{
				if (weakRef.TryGetTarget(out var nativeWebView))
					return nativeWebView;
			}
			return null;
		}

		// Called on every page load ... even if the page isn't bridged
		public static void OnFrameLoaded(string guid)
		{
			if (InstanceForGuid(guid) is NativeWebView nativeWebView)
			{
				if (nativeWebView.Parent is WebView parent)
				{
					parent.InternalSetCanGoBack(false);
					parent.InternalSetCanGoForward(false);
				}
				WindowManagerInterop.ResetStyle(nativeWebView.HtmlId, new[] { "pointer-events" });
			}
		}

		public static void OnMessageReceived(string json)
		{
			var message = JObject.Parse(json);
			if (message.TryGetValue("Target", out var target) && target.ToString() == SessionGuid.ToString())
			{
				if (message.TryGetValue("Method", out var method))
				{
					switch (method.ToString())
					{
						case nameof(InvokeScriptAsync):
							{
								if (message.TryGetValue("TaskId", out var taskId) &&
									TCSs.TryGetValue(taskId.ToString(), out var tcs))
								{
									TCSs.Remove(taskId.ToString());
									if (message.TryGetValue("Result", out var result))
										tcs.SetResult(result.ToString());
									else if (message.TryGetValue("Error", out var error))
										tcs.SetException(new Exception("Javascript Error: " + error.ToString()));
									else
										tcs.SetException(new Exception("Javascript failed for unknown reason"));
								}
							}
							break;
						case "OnBridgeLoaded":
							// called after bridged page is loaded
							{
								if (message.TryGetValue("Source", out var source))
								{
									if (Instances.TryGetValue(source.ToString(), out var weakReference) &&
									weakReference.TryGetTarget(out var nativeWebView))
									{
										if (!nativeWebView._bridgeConnected)
										{
											nativeWebView._bridgeConnected = true;
											nativeWebView.UpdateFromInternalSource();
										}
										if (nativeWebView.Parent is WebView parent &&
											message.TryGetValue("Pages", out var pages) && int.TryParse(pages.ToString(), out var pageCount) &&
											message.TryGetValue("Page", out var page) && int.TryParse(page.ToString(), out var pageIndex))
										{
											parent.InternalSetCanGoBack(pageIndex > 1);
											parent.InternalSetCanGoForward(pageCount > pageIndex);
											if (message.TryGetValue("Href", out var hrefJObject))
											{
												var href = hrefJObject.ToString();
												Uri uri = null;
												if (href.StartsWith("http") || href.StartsWith("file"))
													uri = new Uri(href);
												else if (href.StartsWith("data"))
													uri = new Uri("data:");
												if (href == WebViewBridgeRootPage)
												{
													nativeWebView._activated = true;
													nativeWebView.UpdateFromInternalSource();
												}
												else
												{
													parent.OnNavigationCompleted(true, uri, WebErrorStatus.Found);
												}
											}
										}
									}
								}
							}
							break;
					}
				}
			}
		}


		static string WebViewBridgeRootPage => PackageLocation + "Assets/UnoWebViewBridge.html";
		internal static string WebViewBridgeScriptUrl => PackageLocation + "UnoWebViewBridge.js";

		public readonly string Id;
		readonly Guid InstanceGuid;

		private object _internalSource;
		private bool _bridgeConnected;
		internal bool _activated;

		public NativeWebView() : base("iframe")
		{
			InstanceGuid = Guid.NewGuid();
			Id = WindowManagerInterop.GetAttribute(HtmlId, "id");
			Instances.Add(InstanceGuid.ToString(), new WeakReference<NativeWebView>(this));
			WindowManagerInterop.SetStyles(HtmlId, new[] { ("border", "none") });
			WindowManagerInterop.SetAttribute(HtmlId, "name", $"{SessionGuid}:{InstanceGuid}");
			WindowManagerInterop.SetAttribute(HtmlId, "onLoad", $"UnoWebView_OnLoad('{InstanceGuid}')");
			WindowManagerInterop.SetAttribute(HtmlId, "src", WebViewBridgeRootPage);
		}


		void Navigate(Uri uri)
		{
			_bridgeConnected = false;
			_internalSource = null;
			WebAssemblyRuntime.InvokeJS(new Message<Uri>(this, uri));
		}

		void NavigateToText(string text)
		{
			text = InjectWebBridge(text);
			var valueBytes = Encoding.UTF8.GetBytes(text);
			var base64 = Convert.ToBase64String(valueBytes);
			_bridgeConnected = false;
			_internalSource = null;
			WebAssemblyRuntime.InvokeJS(new Message<string>(this, "data:text/html;charset=utf-8;base64," + base64));
		}

		string InjectWebBridge(string text)
		{
			var script = "<script src='" +
				WebViewBridgeScriptUrl +
				"'></script>";
			var edited = false;
			var index = text.IndexOf("</body>", StringComparison.OrdinalIgnoreCase);
			if (index > -1)
			{
				text = text.Insert(index, script);
				edited = true;
			}
			if (!edited)
			{
				index = text.IndexOf("</head>", StringComparison.OrdinalIgnoreCase);
				if (index > -1)
				{
					text = text.Insert(index, script);
					edited = true;
				}
			}
			if (!edited)
			{
				text = script + text;
			}
			return text;
		}

		internal void GoBack()
		{
			if (_bridgeConnected)
				WebAssemblyRuntime.InvokeJS(new Message(this));
		}

		internal void GoForward()
		{
			if (_bridgeConnected)
				WebAssemblyRuntime.InvokeJS(new Message(this));
		}

		void NavigateWithHttpRequestMessage(HttpRequestMessage message)
		{
			throw new NotSupportedException();
		}


		internal async Task<string> InvokeScriptAsync(string functionName, string[] arguments)
		{
			var tcs = new TaskCompletionSource<string>();
			var taskId = Guid.NewGuid().ToString();
			TCSs.Add(taskId, tcs);
			WebAssemblyRuntime.InvokeJS(new ScriptMessage(this, taskId, functionName, arguments));
			return await tcs.Task;
		}



		internal void SetInternalSource(object source)
		{
			_internalSource = source;
			UpdateFromInternalSource();
		}

		private void UpdateFromInternalSource()
		{
			if (_bridgeConnected && _activated)
			{
				if (_internalSource is Uri uri)
				{
					Navigate(uri);
					return;
				}
				if (_internalSource is string html)
				{
					NavigateToText(html);
				}
				if (_internalSource is HttpRequestMessage message)
				{
					NavigateWithHttpRequestMessage(message);
				}
			}
		}


		class Message
		{
			public string Source { get; private set; }

			public string Method { get; private set; }

			public string Target { get; private set; }

			[JsonIgnore]
			public string Id { get; private set; }

			public Message(NativeWebView nativeWebView, [global::System.Runtime.CompilerServices.CallerMemberName] string callerName = null)
			{
				Source = SessionGuid.ToString();
				Id = nativeWebView.Id;
				Target = nativeWebView.InstanceGuid.ToString();
				Method = callerName;
			}

			public override string ToString() => JsonConvert.SerializeObject(this);

			public static implicit operator string(Message m) => $"UnoWebView_PostMessage('{m.Id}','{m}');";

		}

		class Message<T> : Message
		{
			public T Payload { get; private set; }

			public Message(NativeWebView nativeWebView, T payload, [global::System.Runtime.CompilerServices.CallerMemberName] string callerName = null)
				: base(nativeWebView, callerName)
				=> Payload = payload;
		}

		class ScriptMessage : Message<string[]>
		{
			public string FunctionName { get; private set; }

			public string TaskId { get; private set; }

			public ScriptMessage(NativeWebView nativeWebView, string taskId, string functionName, string[] arguments, [global::System.Runtime.CompilerServices.CallerMemberName] string callerName = null)
				: base(nativeWebView, arguments, callerName)
			{
				FunctionName = functionName;
				TaskId = taskId;
			}
		}


	}
}
#endif
