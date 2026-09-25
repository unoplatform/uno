using System;

namespace Microsoft.VisualStudio.TestTools.UnitTesting;

internal static class RuntimeTestsBackendHelper
{
	private static readonly Lazy<RuntimeTestBackends> _currentBackend = new Lazy<RuntimeTestBackends>(GetCurrentBackend);

	/// <summary>
	/// Returns the drawing backend the current run negotiated, or <see cref="RuntimeTestBackends.None"/> on a
	/// target that has no drawing seam.
	/// </summary>
	public static RuntimeTestBackends CurrentBackend => _currentBackend.Value;

	// The negotiated factory, not the environment variable that asked for it: a backend that fails to initialize
	// falls back, and a test conditioned on WebGPU must follow what actually rendered.
	private static RuntimeTestBackends GetCurrentBackend()
	{
#if __SKIA__
		try
		{
			// The main window's negotiated backend. There is no process-wide holder to ask: a factory belongs to
			// the window that negotiated it.
			var renderer = Private.Infrastructure.TestServices.WindowHelper.XamlRoot?.VisualTree.RootElement?.Visual.CompositionTarget?.Renderer;
			return renderer?.GetType().Assembly.GetName().Name switch
			{
				"Uno.UI.Composition.WebGpu" => RuntimeTestBackends.WebGpu,
				null => RuntimeTestBackends.None,
				_ => RuntimeTestBackends.Skia,
			};
		}
		catch (InvalidOperationException)
		{
			// Read before negotiation ran; no backend is in effect yet.
			return RuntimeTestBackends.None;
		}
#else
		return RuntimeTestBackends.None;
#endif
	}
}
