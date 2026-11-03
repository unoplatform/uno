#if UNO_REFERENCE_API
#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using System.Xml.Linq;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Dispatching;

namespace Uno.UI.NativeElementHosting;

/// <summary>
/// A managed handle to a DOM HTMLElement.
/// </summary>
public sealed partial class BrowserHtmlElement : IDisposable
{
	// Handlers need to be wrapped in a class to be passed to JSInterop
	private record class EventWrapper(EventHandler<JSObject> handler);

	private readonly Dictionary<EventHandler<JSObject>, EventWrapper> _eventMap = new();

	private static void CreateHtmlElementNative(string id, nint unoElementId, string tagName)
	{
		NativeMethods.CreateHtmlElement(id, tagName);
	}

	private void SetCssStyleNative(string name, string value)
	{
		NativeMethods.SetStyleString(ElementId, name, value);
	}

	private void SetCssStyleNative(params (string name, string value)[] styles)
	{
		foreach (var pair in styles)
		{
			NativeMethods.SetStyleString(ElementId, pair.name, pair.value);
		}
	}

	private void ClearCssStyleNative(params string[] names)
	{
		NativeMethods.ResetStyle(ElementId, names);
	}

	private void SetCssClassNative(string[] classes, int index)
	{
		NativeMethods.SetClasses(ElementId, classes, index);
	}

	private void SetCssClassNative(params string[] classesToSet)
	{
		NativeMethods.SetUnsetCssClasses(ElementId, classesToSet);
	}

	private void UnsetCssClassNative(params string[] classesToUnset)
	{
		NativeMethods.SetUnsetCssClasses(ElementId, classesToUnset);
	}

	private void SetHtmlAttributeNative(string name, string value)
	{
		NativeMethods.SetAttribute(ElementId, name, value);
	}

	private void SetHtmlAttributeNative(params (string name, string value)[] attributes)
	{
		foreach (var pair in attributes)
		{
			NativeMethods.SetAttribute(ElementId, pair.name, pair.value);
		}
	}

	private string GetHtmlAttributeNative(string name)
	{
		return NativeMethods.GetAttribute(ElementId, name);
	}

	private void ClearHtmlAttributeNative(string name)
	{
		NativeMethods.RemoveAttribute(ElementId, name);
	}

	private void RemoveAttributeNative(string name)
	{
		NativeMethods.RemoveAttribute(ElementId, name);
	}

	private string ExecuteJavascriptNative(string jsCode)
	{
		var js = $$"""
			(function(element) {
			{{jsCode}}
			})(document.getElementById("{{ElementId}}"));
			""";
		return NativeMethods.InvokeJS(js);
	}

	private Task<string> ExecuteJavascriptNativeAsync(string asyncJsCode)
	{
		var js = $$"""
			(function(element) {
			const __f = () => {{asyncJsCode}};
			return __f(element);
			})(document.getElementById("{{ElementId}}"));
			""";
		return NativeMethods.InvokeAsync(js);
	}

	private void SetHtmlContentNative(string html)
	{
		NativeMethods.SetContentHtml(ElementId, html);
	}

	partial void OnInputPolicyChanged(BrowserHtmlElementInputPolicy value)
		=> NativeMethods.SetInputPolicy(ElementId, UnoElementId, (int)value);

	[JSExport]
	private static bool DispatchEventNativeElementMethod(
		[JSMarshalAs<JSType.Any>] object owner,
		string eventName,
		[JSMarshalAs<JSType.Any>] object eventWrapper,
		JSObject payload)
	{
		if (eventWrapper is EventWrapper wrapper)
		{
			wrapper.handler(owner, payload);

			return true;
		}
		else
		{
			return false;
		}
	}

	private void RegisterHtmlEventHandlerNative(string eventName, EventHandler<JSObject> handler)
	{
		var wrapper = new EventWrapper(handler);
		_eventMap[handler] = wrapper;

		NativeMethods.RegisterNativeHtmlEvent(this, ElementId, eventName, wrapper);
	}

	private void UnregisterHtmlEventHandlerNative(string eventName, EventHandler<JSObject> handler)
	{
		if (_eventMap.TryGetValue(handler, out var wrapper))
		{
			_eventMap.Remove(handler);
			NativeMethods.UnregisterNativeHtmlEvent(ElementId, eventName, wrapper);
		}
	}

	private DomRemovalOnCollect? _domRemovalOnCollect;

	/// <summary>
	/// Removes the element from the DOM once this wrapper is garbage collected. Used for elements the framework creates
	/// on behalf of a control, which application code cannot dispose (e.g. the WebView2 iframe).
	/// </summary>
	/// <remarks>
	/// The deferred removal finds the element by <see cref="ElementId"/>, so it relies on ids never being reused: the weak
	/// handle they are built from is never freed. It never happens while <see cref="RegisterHtmlEventHandler"/> handlers
	/// are registered, because their JS proxy keeps this wrapper alive.
	/// </remarks>
	internal void RemoveFromDomWhenCollected()
		=> _domRemovalOnCollect ??= new DomRemovalOnCollect(ElementId);

	partial void DisposeNative()
	{
		_domRemovalOnCollect?.Cancel();
		NativeMethods.DisposeHtmlElement(ElementId);
	}

	// The finalizer lives on this sentinel rather than on BrowserHtmlElement, so only the elements the framework opts in
	// are removed on collection: application-created elements keep their explicit Dispose() contract.
	private sealed class DomRemovalOnCollect(string elementId)
	{
		public void Cancel() => GC.SuppressFinalize(this);

		~DomRemovalOnCollect()
		{
			try
			{
				// The DOM is only reachable from the UI thread, and finalizers may run on another one.
				NativeDispatcher.Main.Enqueue(Remove, NativeDispatcherPriority.Idle);
			}
			catch (Exception)
			{
				// An exception escaping a finalizer ends the process, while a missed removal only leaks the element.
			}
		}

		private void Remove()
		{
			try
			{
				NativeMethods.DisposeHtmlElement(elementId);
				typeof(BrowserHtmlElement).LogTrace()?.Trace($"Removed the collected element {elementId} from the DOM.");
			}
			catch (JSException e)
			{
				typeof(BrowserHtmlElement).LogWarn()?.Warn($"Failed to remove the collected element {elementId} from the DOM: {e.Message}");
			}
		}
	}

	internal static void Initialize()
		=> NativeMethods.Initialize();

	private static partial class NativeMethods
	{
		[JSImport($"globalThis.Uno.UI.NativeElementHosting.BrowserHtmlElement.initialize")]
		internal static partial void Initialize();

		[JSImport($"globalThis.Uno.UI.NativeElementHosting.BrowserHtmlElement.createHtmlElement")]
		internal static partial void CreateHtmlElement(string id, string tagName);

		[JSImport($"globalThis.Uno.UI.NativeElementHosting.BrowserHtmlElement.disposeHtmlElement")]
		internal static partial bool DisposeHtmlElement(string id);

		[JSImport($"globalThis.Uno.UI.NativeElementHosting.BrowserHtmlElement.setStyleString")]
		internal static partial void SetStyleString(string elementId, string name, string value);

		[JSImport($"globalThis.Uno.UI.NativeElementHosting.BrowserHtmlElement.resetStyle")]
		internal static partial void ResetStyle(string elementId, string[] names);

		[JSImport($"globalThis.Uno.UI.NativeElementHosting.BrowserHtmlElement.setClasses")]
		internal static partial void SetClasses(string elementId, string[] classes, int index);

		[JSImport($"globalThis.Uno.UI.NativeElementHosting.BrowserHtmlElement.setUnsetCssClasses")]
		internal static partial void SetUnsetCssClasses(string elementId, string[] classesToUnset);

		[JSImport($"globalThis.Uno.UI.NativeElementHosting.BrowserHtmlElement.setAttribute")]
		internal static partial void SetAttribute(string elementId, string name, string value);

		[JSImport($"globalThis.Uno.UI.NativeElementHosting.BrowserHtmlElement.getAttribute")]
		internal static partial string GetAttribute(string elementId, string name);

		[JSImport($"globalThis.Uno.UI.NativeElementHosting.BrowserHtmlElement.removeAttribute")]
		internal static partial void RemoveAttribute(string elementId, string name);

		[JSImport($"globalThis.Uno.UI.NativeElementHosting.BrowserHtmlElement.setContentHtml")]
		internal static partial void SetContentHtml(string elementId, string html);

		[JSImport($"globalThis.Uno.UI.NativeElementHosting.BrowserHtmlElement.setInputPolicy")]
		internal static partial void SetInputPolicy(string elementId, nint unoElementId, int value);

		[JSImport($"globalThis.Uno.UI.NativeElementHosting.BrowserHtmlElement.registerNativeHtmlEvent")]
		internal static partial void RegisterNativeHtmlEvent(
			[JSMarshalAs<JSType.Any>] object browserHtmlElement,
			string elementId,
			string eventName,
			[JSMarshalAs<JSType.Any>] object handler);

		[JSImport($"globalThis.Uno.UI.NativeElementHosting.BrowserHtmlElement.unregisterNativeHtmlEvent")]
		internal static partial void UnregisterNativeHtmlEvent(
			string elementId,
			string eventName,
			[JSMarshalAs<JSType.Any>] object handler);

		[JSImport($"globalThis.Uno.UI.NativeElementHosting.BrowserHtmlElement.invokeJS")]
		internal static partial string InvokeJS(string js);

		[JSImport($"globalThis.Uno.UI.NativeElementHosting.BrowserHtmlElement.invokeAsync")]
		internal static partial Task<string> InvokeAsync(string js);
	}
}
#endif
