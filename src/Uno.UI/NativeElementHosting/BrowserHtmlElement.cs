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
}
#endif
