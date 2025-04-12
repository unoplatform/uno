#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using System.Xml.Linq;
using Uno.Extensions;

namespace Uno.UI.NativeElementHosting;

/// <summary>
/// A managed handle to a DOM HTMLElement.
/// </summary>
public sealed partial class BrowserHtmlElement : IDisposable
{
	private static void CreateHtmlElementNative(string id, nint unoElementId, string tagName)
		=> throw new PlatformNotSupportedException();

	private static void RegisterExistingElementNative(string elementId)
		=> throw new PlatformNotSupportedException();

	private void SetCssStyleNative(string name, string value)
		=> throw new PlatformNotSupportedException();

	private void SetCssStyleNative(params (string name, string value)[] styles)
		=> throw new PlatformNotSupportedException();

	private void ClearCssStyleNative(params string[] names)
		=> throw new PlatformNotSupportedException();

	private void SetCssClassNative(string[] classes, int index)
		=> throw new PlatformNotSupportedException();

	private void SetCssClassNative(params string[] classesToSet)
		=> throw new PlatformNotSupportedException();

	private void UnsetCssClassNative(params string[] classesToUnset)
		=> throw new PlatformNotSupportedException();

	private void SetHtmlAttributeNative(string name, string value)
		=> throw new PlatformNotSupportedException();

	private void SetHtmlAttributeNative(params (string name, string value)[] attributes)
		=> throw new PlatformNotSupportedException();

	private string GetHtmlAttributeNative(string name)
		=> throw new PlatformNotSupportedException();

	private void ClearHtmlAttributeNative(string name)
		=> throw new PlatformNotSupportedException();

	private void RemoveAttributeNative(string name)
		=> throw new PlatformNotSupportedException();

	private string ExecuteJavascriptNative(string jsCode)
		=> throw new PlatformNotSupportedException();

	private Task<string> ExecuteJavascriptNativeAsync(string asyncJsCode)
		=> throw new PlatformNotSupportedException();

	private void SetHtmlContentNative(string html)
		=> throw new PlatformNotSupportedException();

	[JSExport]
	private static bool DispatchEventNativeElementMethod(
		[JSMarshalAs<JSType.Any>] object owner,
		string eventName,
		[JSMarshalAs<JSType.Any>] object eventHandler,
		JSObject payload)
		=> throw new PlatformNotSupportedException();

	private void RegisterHtmlEventHandlerNative(string eventName, EventHandler<JSObject> handler)
		=> throw new PlatformNotSupportedException();

	private void UnregisterHtmlEventHandlerNative(string eventName, EventHandler<JSObject> handler)
		=> throw new PlatformNotSupportedException();

	partial void DisposeNative()
		=> throw new PlatformNotSupportedException();

	internal static void Initialize()
		=> throw new PlatformNotSupportedException();
}
