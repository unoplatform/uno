namespace Uno.TemplateTests;

// Stands in for code that registers the WebGPU backend. Naming the type is what puts the backend assembly into
// the reference table of whichever assembly compiles this, which is the whole signal the detection reads.
internal static class WebGpuProbe
{
	public static string BackendName => typeof(global::Uno.UI.Composition.WebGpu.WebGpuGraphicsProvider).FullName!;
}
