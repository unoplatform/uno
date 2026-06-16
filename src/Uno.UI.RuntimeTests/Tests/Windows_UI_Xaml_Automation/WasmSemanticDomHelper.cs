#if HAS_UNO
using System;
using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation
{
	/// <summary>
	/// Shared helpers for the Skia-WASM accessibility runtime tests. Centralizes the small surface
	/// that every <c>Given_Accessible*</c> class used to duplicate (enable-a11y click, semantic-node
	/// id lookup, semantic-DOM attribute reads, and the reflection-based call into
	/// <c>Uno.Foundation.WebAssemblyRuntime.InvokeJS</c>) so a future change to the runtime entry
	/// point or to the semantic id scheme is a one-file edit.
	/// </summary>
	internal static class WasmSemanticDomHelper
	{
		/// <summary>
		/// Clicks the in-app <c>uno-enable-accessibility</c> button to opt the runtime into building
		/// the parallel semantic DOM. The semantic tree is gated behind this opt-in to avoid the
		/// cost when no AT is active; every DOM-level assertion has to flip it on first.
		/// </summary>
		public static void EnableAccessibilityThroughDom()
		{
			InvokeBrowserJs("(function(){const button = document.getElementById('uno-enable-accessibility'); if (button) { button.click(); } return 'ok';})()");
		}

		/// <summary>
		/// Returns the deterministic id assigned to a UIElement's semantic node. Targets the exact
		/// element under test via its Visual.Handle; a generic role selector would match the first
		/// semantic node of that kind in the document, which is usually not the element under test
		/// (e.g. the test runner header).
		/// </summary>
		public static string GetSemanticElementId(UIElement element)
			=> "uno-semantics-" + ((long)element.Visual.Handle).ToString(CultureInfo.InvariantCulture);

		/// <summary>Returns true when the element's semantic node currently exists in the DOM.</summary>
		public static bool SemanticElementExists(UIElement element)
			=> InvokeBrowserJs($"(function(){{return document.getElementById('{GetSemanticElementId(element)}') ? '1' : '0';}})()") == "1";

		/// <summary>
		/// Returns the value of an attribute on the element's semantic node, or empty string when the
		/// node or attribute is absent. Use <see cref="SemanticElementHasAttribute"/> to distinguish
		/// "attribute absent" from "attribute present with empty value".
		/// </summary>
		public static string GetSemanticAttribute(UIElement element, string attribute)
			=> InvokeBrowserJs($"(function(){{const e = document.getElementById('{GetSemanticElementId(element)}'); if (!e) {{ return ''; }} return e.getAttribute('{attribute}') || ''; }})()");

		/// <summary>
		/// Returns true when the element's semantic node has the named attribute (even if its value
		/// is empty). Lets tests assert the omit-when-empty contract (FR-030 hygiene) — empty values
		/// like <c>aria-label=""</c> are worse than omitting the attribute entirely.
		/// </summary>
		public static bool SemanticElementHasAttribute(UIElement element, string attribute)
			=> InvokeBrowserJs($"(function(){{const e = document.getElementById('{GetSemanticElementId(element)}'); if (!e) {{ return '0'; }} return e.hasAttribute('{attribute}') ? '1' : '0'; }})()") == "1";

		/// <summary>Returns the lower-cased tag name of the element's semantic node (e.g. <c>"button"</c>, <c>"h1"</c>).</summary>
		public static string GetSemanticElementTagName(UIElement element)
			=> InvokeBrowserJs($"(function(){{const e = document.getElementById('{GetSemanticElementId(element)}'); return e ? e.tagName.toLowerCase() : ''; }})()");

		/// <summary>Returns the <c>type</c> attribute of the element's semantic node (e.g. <c>"checkbox"</c>, <c>"range"</c>); empty when absent.</summary>
		public static string GetSemanticInputType(UIElement element)
			=> InvokeBrowserJs($"(function(){{const e = document.getElementById('{GetSemanticElementId(element)}'); return e ? (e.getAttribute('type') || '') : ''; }})()");

		/// <summary>
		/// Invokes <c>Uno.Foundation.WebAssemblyRuntime.InvokeJS</c> via reflection. The runtime
		/// assembly isn't a direct reference of the runtime-tests project, so the lookup is kept
		/// here in one place — and intentionally fails fast (<see cref="Assert.IsNotNull"/>) so a
		/// runtime rename surfaces with a clear test failure instead of silently returning empty.
		/// </summary>
		public static string InvokeBrowserJs(string javascript)
		{
			var runtimeType = Type.GetType("Uno.Foundation.WebAssemblyRuntime, Uno.Foundation.Runtime.WebAssembly", throwOnError: false);
			Assert.IsNotNull(runtimeType, "Unable to locate Uno.Foundation.WebAssemblyRuntime at runtime.");

			var invokeJs = runtimeType.GetMethod("InvokeJS", new[] { typeof(string) });
			Assert.IsNotNull(invokeJs, "Unable to locate Uno.Foundation.WebAssemblyRuntime.InvokeJS(string).");

			return invokeJs.Invoke(obj: null, parameters: new object[] { javascript }) as string ?? string.Empty;
		}
	}
}
#endif
