namespace Uno.TemplateTests;

// Stands in for an app that registers the WebGPU backend. Naming the type is what puts the backend assembly in
// the app's reference table, which is the whole signal the native-payload detection reads.
internal static class WebGpuProbe
{
	public static string BackendName => typeof(global::Uno.UI.Composition.WebGpu.WebGpuGraphicsProvider).FullName!;
}
