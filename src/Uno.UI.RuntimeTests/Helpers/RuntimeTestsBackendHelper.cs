using System;

namespace Microsoft.VisualStudio.TestTools.UnitTesting;

internal static class RuntimeTestsBackendHelper
{
	private static RuntimeTestBackends? _currentBackend;

	/// <summary>
	/// Returns the drawing backend the current run negotiated, or <see cref="RuntimeTestBackends.None"/> on a
	/// target that has no drawing seam.
	/// </summary>
	public static RuntimeTestBackends CurrentBackend
	{
		get
		{
			if (_currentBackend is { } cached)
			{
				return cached;
			}

			// Only a resolved backend is cached: a read before negotiation must not pin None for the rest of the run.
			var backend = GetCurrentBackend();
			if (backend != RuntimeTestBackends.None)
			{
				_currentBackend = backend;
			}

			return backend;
		}
	}

	// The negotiated factory, not the environment variable that asked for it: a backend that fails to initialize
	// falls back, and a test conditioned on WebGPU must follow what actually rendered.
	private static RuntimeTestBackends GetCurrentBackend()
	{
#if __SKIA__
		try
		{
			return Uno.UI.Composition.Drawing.DrawingFactory.Current.GetType().Assembly.GetName().Name switch
			{
				"Uno.UI.Composition.WebGpu" => RuntimeTestBackends.WebGpu,
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
