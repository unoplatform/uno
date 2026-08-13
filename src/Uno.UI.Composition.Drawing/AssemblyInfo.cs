using System.Runtime.CompilerServices;

// The low-level drawing-backend registration mutators (DrawingFactory.Register, GraphicsRegistry.Register + context
// factories, FontProvider/ImageDecoder setters, DrawingRegistration.DefaultRenderer) are internal: app-side
// registration goes through the host builder. These framework assemblies register on the app's behalf — the builder
// (Uno.UI), the managed backend (Uno.UI.Composition), and the Skia/WebGPU backends (their module initializers /
// providers) — so they need access to the internal registrars.
[assembly: InternalsVisibleTo("Uno.UI")]
[assembly: InternalsVisibleTo("Uno.UI.Wasm")]
[assembly: InternalsVisibleTo("Uno.UI.Composition")]
[assembly: InternalsVisibleTo("Uno.UI.Composition.Skia")]
[assembly: InternalsVisibleTo("Uno.UI.Composition.WebGpu")]
