#nullable enable

using System;
using System.Runtime.InteropServices.JavaScript;
namespace Uno.UI.Runtime.Skia;

public partial class SkiaWasmHtmlElement : IDisposable
{
	public string ElementId { get; }

	public SkiaWasmHtmlElement(string elementId, string tagName)
	{
#if __SKIA__
		if (!OperatingSystem.IsBrowser())
		{
			throw new InvalidOperationException($"{nameof(SkiaWasmHtmlElement)} is only usable on the Wasm target with the skia backend.");
		}
#endif
		CreateHtmlElementAndAddToStore(elementId, tagName);
		ElementId = elementId;
	}

	public SkiaWasmHtmlElement(string elementId)
	{
#if __SKIA__
		if (!OperatingSystem.IsBrowser())
		{
			throw new InvalidOperationException($"{nameof(SkiaWasmHtmlElement)} is only usable on the Wasm target with the skia backend.");
		}
#endif
		AddToStore(elementId);
		ElementId = elementId;
	}

	[JSImport($"eval")]
	public static partial void Eval(string script);

	[JSImport($"globalThis.Uno.UI.Runtime.Skia.BrowserNativeElementHostingExtension.createHtmlElementAndAddToStore")]
	private static partial void CreateHtmlElementAndAddToStore(string id, string tagName);

	[JSImport($"globalThis.Uno.UI.Runtime.Skia.BrowserNativeElementHostingExtension.addToStore")]
	private static partial void AddToStore(string id);

	[JSImport($"globalThis.Uno.UI.Runtime.Skia.BrowserNativeElementHostingExtension.disposeHtmlElement")]
	private static partial bool DisposeHtmlElement(string id);

	public void Dispose() => DisposeHtmlElement(ElementId);
}
