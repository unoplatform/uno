#if UNO_REFERENCE_API
#nullable enable

using System;
using System.Runtime.InteropServices.JavaScript;
namespace Uno.UI.Runtime.Skia;

public partial class SkiaWasmHtmlElement : IDisposable
{
	public string ElementId { get; }

	private SkiaWasmHtmlElement(string elementId)
	{
#if __SKIA__
		if (!OperatingSystem.IsBrowser())
#endif
		{
			throw new NotSupportedException($"{nameof(SkiaWasmHtmlElement)} is only supported on the Wasm target with the skia backend.");
		}
#if __SKIA__
		ElementId = elementId;
#endif
	}

	/// <summary>
	/// Creates an HTML element and wraps it in a <see cref="SkiaWasmHtmlElement"/>
	/// instance to be managed by Uno's native element hosting logic. After this call,
	/// the HTML element is considered owned by the returned <see cref="SkiaWasmHtmlElement"/>
	/// instance and will handle the dimensions and placement of the element in the DOM.
	/// </summary>
	/// <param name="elementId">The id that will be set to the created element. This id must be globally unique.</param>
	/// <param name="tagName">The HTML tag name of the created element.</param>
	public static SkiaWasmHtmlElement CreateHtmlElement(string elementId, string tagName)
	{
		CreateHtmlElementAndAddToStore(elementId, tagName);
		return new SkiaWasmHtmlElement(elementId);
	}

	/// <summary>
	/// Wraps a preexisting HTML element in the DOM in a <see cref="SkiaWasmHtmlElement"/>
	/// instance to be managed by Uno's native element hosting logic. After this call,
	/// the HTML element is considered owned by the returned <see cref="SkiaWasmHtmlElement"/>
	/// instance and will handle the dimensions and placement of the element in the DOM.
	/// </summary>
	/// <param name="elementId">The id of the element. The DOM must contain an element with this id.</param>
	public static SkiaWasmHtmlElement OwnHtmlElement(string elementId)
	{
		AddToStore(elementId);
		return new SkiaWasmHtmlElement(elementId);
	}

	[JSImport($"globalThis.Uno.UI.Runtime.Skia.BrowserNativeElementHostingExtension.createHtmlElementAndAddToStore")]
	private static partial void CreateHtmlElementAndAddToStore(string id, string tagName);

	[JSImport($"globalThis.Uno.UI.Runtime.Skia.BrowserNativeElementHostingExtension.addToStore")]
	private static partial void AddToStore(string id);

	[JSImport($"globalThis.Uno.UI.Runtime.Skia.BrowserNativeElementHostingExtension.disposeHtmlElement")]
	private static partial bool DisposeHtmlElement(string id);

	public void Dispose() => DisposeHtmlElement(ElementId);
}
#endif
