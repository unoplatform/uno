using System;

namespace Uno.UI.Runtime.Skia
{
	/// <summary>
	/// Public shim used by runtime tests to interact with platform-specific accessibility helpers.
	/// On platforms that provide a concrete implementation (e.g. WebAssembly), tests may call
	/// methods on the real implementation. This shim ensures tests compile and can be executed
	/// on non-WASM platforms by providing a safe no-op implementation.
	/// </summary>
	public sealed class WebAssemblyAccessibility
	{
		public static WebAssemblyAccessibility Instance { get; } = new WebAssemblyAccessibility();

		private WebAssemblyAccessibility() { }

		public void AnnouncePolite(string message)
		{
			// No-op shim for non-WASM platforms
		}

		public void AnnounceAssertive(string message)
		{
			// No-op shim for non-WASM platforms
		}
	}
}
