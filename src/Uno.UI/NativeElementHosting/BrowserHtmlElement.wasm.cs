#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using System.Transactions;
using Uno.Extensions;
using Uno.Foundation;
using Uno.UI.Xaml;
using static Microsoft.UI.Xaml.Controls.CollectionChangedOperation;

namespace Uno.UI.NativeElementHosting;

partial class BrowserHtmlElement
{
	// Handlers need to be wrapped in a class to be passed to JSInterop
	private record class EventWrapper(EventHandler<JSObject> handler);

	private readonly Dictionary<EventHandler<JSObject>, EventWrapper> _eventMap = new();

	private static void CreateHtmlElementNative(string id, nint unoElementId, string tagName)
	{
		NativeMethods.CreateNativeElement(id, unoElementId, tagName);
	}

	private static void RegisterExistingElementNative(string elementId)
	{
	}

	internal void AttachToElement(nint ownerId)
	{
		NativeMethods.AttachNativeElement(ownerId, UnoElementId);
	}

	internal void DetachFromElement(nint ownerId)
	{
		NativeMethods.DetachNativeElement(UnoElementId);
	}

	private void SetCssStyleNative(string name, string value)
	{
		WindowManagerInterop.SetStyleString(UnoElementId, name, value);
	}

	private void SetCssStyleNative(params (string name, string value)[] styles)
	{
		WindowManagerInterop.SetStyles(UnoElementId, styles);
	}

	private void ClearCssStyleNative(params string[] names)
	{
		WindowManagerInterop.ResetStyle(UnoElementId, names);
	}

	private void SetCssClassNative(string[] classes, int index)
	{
		WindowManagerInterop.SetClasses(UnoElementId, classes, index);
	}

	private void SetCssClassNative(params string[] classesToSet)
	{
		WindowManagerInterop.SetUnsetCssClasses(UnoElementId, classesToSet, null);
	}

	private void UnsetCssClassNative(params string[] classesToUnset)
	{
		WindowManagerInterop.SetUnsetCssClasses(UnoElementId, null, classesToUnset);
	}

	private void SetHtmlAttributeNative(string name, string value)
	{
		WindowManagerInterop.SetAttribute(UnoElementId, name, value);
	}

	private void SetHtmlAttributeNative(params (string name, string value)[] attributes)
	{
		WindowManagerInterop.SetAttributes(UnoElementId, attributes);
	}

	private string GetHtmlAttributeNative(string name)
	{
		return WindowManagerInterop.GetAttribute(UnoElementId, name);
	}

	private void ClearHtmlAttributeNative(string name)
	{
		WindowManagerInterop.RemoveAttribute(UnoElementId, name);
	}

	private void RemoveAttributeNative(string name)
	{
		WindowManagerInterop.RemoveAttribute(UnoElementId, name);
	}

	private string ExecuteJavascriptNative(string jsCode)
	{
		var js = $$"""
			(function(element) {
			{{jsCode}}
			})(Uno.UI.WindowManager.current.getView({{UnoElementId}}));
			""";
		return WebAssemblyRuntime.InvokeJS(js);
	}

	private Task<string> ExecuteJavascriptNativeAsync(string asyncJsCode)
	{
		var js = $$"""
			(function(element) {
			const __f = () => {{asyncJsCode}};
			return __f(element);
			})(Uno.UI.WindowManager.current.getView({{UnoElementId}}));
			""";
		return WebAssemblyRuntime.InvokeAsync(js);
	}

	private void SetHtmlContentNative(string html)
	{
		WindowManagerInterop.SetContentHtml(UnoElementId, html);
	}

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

		NativeMethods.RegisterNativeHtmlEvent(this, UnoElementId, eventName, wrapper);
	}

	private void UnregisterHtmlEventHandlerNative(string eventName, EventHandler<JSObject> handler)
	{
		if (_eventMap.TryGetValue(handler, out var wrapper))
		{
			_eventMap.Remove(handler);
			NativeMethods.UnregisterNativeHtmlEvent(UnoElementId, eventName, wrapper);
		}
	}

	partial void DisposeNative()
	{
		NativeMethods.DisposeHtmlElement(UnoElementId);
	}

	private static unsafe partial class NativeMethods
	{
		[JSImport($"globalThis.Uno.UI.WindowManager.current.createNativeElement")]
		internal static partial void CreateNativeElement(string elementId, nint unoElementId, string tagName);

		[JSImport($"globalThis.Uno.UI.WindowManager.current.attachNativeElement")]
		internal static partial void AttachNativeElement(nint ownerId, nint unoElementId);

		[JSImport($"globalThis.Uno.UI.WindowManager.current.detachNativeElement")]
		internal static partial void DetachNativeElement(nint unoElementId);

		[JSImport($"globalThis.Uno.UI.WindowManager.current.disposeNativeElement")]
		internal static partial bool DisposeHtmlElement(nint unoElementId);

		[JSImport($"globalThis.Uno.UI.WindowManager.current.registerNativeHtmlEvent")]
		internal static partial void RegisterNativeHtmlEvent(
			[JSMarshalAs<JSType.Any>] object browserHtmlElement,
			nint unoElementId,
			string eventName,
			[JSMarshalAs<JSType.Any>] object handler);

		[JSImport($"globalThis.Uno.UI.WindowManager.current.unregisterNativeHtmlEvent")]
		internal static partial void UnregisterNativeHtmlEvent(
			nint unoElementId,
			string eventName,
			[JSMarshalAs<JSType.Any>] object handler);
	}
}
