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

// The graphics negotiator (GraphicsRegistry, GraphicsInitialization, the GraphicsContextFactory delegate) is
// framework host-plumbing, not third-party API — the Skia hosts set the context factory, negotiate, and read the
// resulting renderer/context. The third-party backend SPI itself (IGraphicsProvider/IGraphicsContext/Graphics/
// GraphicsContextKind/IDrawingFactory) stays public; only these host-facing negotiation types are internal + IVT'd.
[assembly: InternalsVisibleTo("Uno.UI.Runtime.Skia.X11")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.Skia.Win32")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.Skia.MacOS")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.Skia.WebAssembly.Browser")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.Skia.Linux.FrameBuffer")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.Skia.AppleUIKit")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.Skia.Android")]
