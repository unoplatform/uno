#if UNO_REFERENCE_API
#nullable enable

using System;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using System.Xml.Linq;
using Uno.Extensions;
using Uno.Foundation;
using Uno.UI.Xaml;
using System.Collections.Generic;

namespace Uno.UI.NativeElementHosting;

/// <summary>
/// A managed handle to a DOM HTMLElement.
/// </summary>
public sealed partial class BrowserHtmlElement : IDisposable
{
	private GCHandle? _gcHandle;

	internal nint UnoElementId { get; }

	/// <summary>
	/// The native HTMLElement id.
	/// </summary>
	public string ElementId { get; }

	internal bool IsOwner { get; }

	private BrowserHtmlElement(bool isOwner)
	{
		if (!OperatingSystem.IsBrowser())
		{
			throw new NotSupportedException($"{nameof(BrowserHtmlElement)} is only supported on WebAssembly.");
		}

		_gcHandle = GCHandle.Alloc(this, GCHandleType.Weak);
		var handle = GCHandle.ToIntPtr(_gcHandle.Value);
		UnoElementId = handle;
		ElementId = "uno-" + handle;
		IsOwner = isOwner;
	}

	private BrowserHtmlElement(string elementId)
		: this(false)
	{
		if (!OperatingSystem.IsBrowser())
		{
			throw new NotSupportedException($"{nameof(BrowserHtmlElement)} is only supported on WebAssembly.");
		}

		ElementId = elementId;
	}

	/// <summary>
	/// Creates an HTML element and wraps it in a <see cref="BrowserHtmlElement"/>
	/// instance to be managed by Uno's native element hosting logic. After this call,
	/// the HTML element is considered owned by the returned <see cref="BrowserHtmlElement"/>
	/// instance and will handle the dimensions and placement of the element in the DOM.
	/// </summary>
	/// <param name="elementId">The id that will be set to the created element. This id must be globally unique.</param>
	/// <param name="tagName">The HTML tag name of the created element.</param>
	public static BrowserHtmlElement CreateHtmlElement(string elementId, string tagName)
	{
		var element = new BrowserHtmlElement(elementId);
		CreateHtmlElementNative(element.ElementId, element.UnoElementId, tagName);
		return element;
	}

	/// <summary>
	/// Creates an element with the given tag name and a random id.
	/// </summary>
	/// <param name="tagName">Tag name.</param>
	/// <returns>Element instance.</returns>
	public static BrowserHtmlElement CreateHtmlElement(string tagName)
	{
		var element = new BrowserHtmlElement(isOwner: true);
		CreateHtmlElementNative(element.ElementId, element.UnoElementId, tagName);
		return element;
	}

	/// <summary>
	/// Wraps a preexisting HTML element in the DOM in a <see cref="BrowserHtmlElement"/>
	/// instance to be managed by Uno's native element hosting logic. After this call,
	/// the HTML element is considered owned by the returned <see cref="BrowserHtmlElement"/>
	/// instance and will handle the dimensions and placement of the element in the DOM.
	/// </summary>
	/// <param name="elementId">The id of the element. The DOM must contain an element with this id.</param>
	public static BrowserHtmlElement OwnHtmlElement(string elementId)
	{
		return new BrowserHtmlElement(elementId);
	}

	/// <summary>
	/// Get the Id of the corresponding element in the HTML DOM
	/// </summary>
	/// <remarks>Compatibility method with previous version od .NET</remarks>
	public string GetHtmlId()
		=> ElementId.ToString(CultureInfo.InvariantCulture);

	/// <summary>
	/// Set one CSS style on a HTML element.
	/// </summary>
	/// <remarks>
	/// The style is using the CSS syntax format, not the DOM syntax.
	/// Ex: for font size, use "font-size", not "fontSize".
	/// </remarks>
	public void SetCssStyle(string name, string value)
		=> SetCssStyleNative(name, value);

	/// <summary>
	/// Set one or many CSS styles on a HTML element.
	/// </summary>
	/// <remarks>
	/// The style is using the CSS syntax format, not the DOM syntax.
	/// Ex: for font size, use "font-size", not "fontSize".
	/// </remarks>
	public void SetCssStyle(params (string name, string value)[] styles)
		=> SetCssStyleNative(styles);

	/// <summary>
	/// Clear one or many CSS styles from a HTML element.
	/// </summary>
	public void ClearCssStyle(params string[] names)
		=> ClearCssStyleNative(names);

	/// <summary>
	/// Set one of the predefined class
	/// </summary>
	/// <remarks>
	/// Useful to switch a control from different modes when each mode is bound
	/// to a specific class.
	/// </remarks>
	public void SetCssClass(string[] classes, int index)
		=> SetCssClassNative(classes, index);

	/// <summary>
	/// Add one or many CSS classes to a HTML element, if not present.
	/// </summary>
	public void SetCssClass(params string[] classesToSet)
		=> SetCssClassNative(classesToSet);

	/// <summary>
	/// Remove one or many CSS classes from a HTML element, if defined.
	/// </summary>
	public void UnsetCssClass(params string[] classesToUnset)
		=> UnsetCssClassNative(classesToUnset);

	/// <summary>
	/// Set a HTML attribute to an element.
	/// </summary>
	public void SetHtmlAttribute(string name, string value)
		=> SetHtmlAttributeNative(name, value);

	/// <summary>
	/// Set multiple HTML attributes to an element at the same time.
	/// </summary>
	public void SetHtmlAttribute(params (string name, string value)[] attributes)
		=> SetHtmlAttributeNative(attributes);

	/// <summary>
	/// Get the HTML attribute value of an element
	/// </summary>
	public string GetHtmlAttribute(string name)
		=> GetHtmlAttributeNative(name);

	/// <summary>
	/// Clear/remove a HTML attribute from an element.
	/// </summary>
	public void ClearHtmlAttribute(string name)
		=> ClearHtmlAttributeNative(name);

	/// <summary>
	/// Clear/remove a HTML attribute from an element.
	/// </summary>
	public void RemoveAttribute(string name)
		=> RemoveAttributeNative(name);

	/// <summary>
	/// Run javascript in the context of a DOM element.
	/// This one is available in the scope as "element".
	/// </summary>
	/// <remarks>
	/// Will work even if the element is not yet loaded into the DOM.
	/// </remarks>
	public string ExecuteJavascript(string jsCode)
		=> ExecuteJavascriptNative(jsCode);

	/// <summary>
	/// Asynchronously run javascript on a DOM element.
	/// This one is available in the scope as "element".
	/// The called code is expected to return something awaitable (a Promise).
	/// </summary>
	/// <remarks>
	/// Will work even if the element is not yet loaded into the DOM.
	/// </remarks>
	public Task<string> ExecuteJavascriptAsync(string asyncJsCode)
		=> ExecuteJavascriptNativeAsync(asyncJsCode);

	/// <summary>
	/// Set raw HTML Content for this element.
	/// Don't use this when there's child elements managed by Uno or you'll
	/// get expected results.
	/// </summary>
	public void SetHtmlContent(string html)
		=> SetHtmlContentNative(html);

	/// <summary>
	/// Will invoke `addEventListener(<paramref name="eventName"/>)` on the corresponding HTML element.
	/// </summary>
	public void RegisterHtmlEventHandler(string eventName, EventHandler<JSObject> handler)
		=> RegisterHtmlEventHandlerNative(eventName, handler);

	/// <summary>
	/// Unregister previously registered event with RegisterHtmlEventHandler.
	/// </summary>
	public void UnregisterHtmlEventHandler(string eventName, EventHandler<JSObject> handler)
		=> UnregisterHtmlEventHandlerNative(eventName, handler);

	public void Dispose()
		=> DisposeNative();

	partial void DisposeNative();

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

	partial void DisposeNative()
	{
		NativeMethods.DisposeHtmlElement(ElementId);
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
