using System.Runtime.CompilerServices;

// The low-level registration mutators are internal (app-side registration goes through the host builder); these
// framework assemblies get IVT so the builder can read the internal factory holders and register defaults.
// The managed backend (Uno.UI.Composition.Managed) deliberately gets NO grant: it stands entirely on the public
// seam (factories injected into its BuildGlyphRun / Parse operations), like the Skia backend.
[assembly: InternalsVisibleTo("Uno.UI")]
[assembly: InternalsVisibleTo("Uno.UI.Wasm")]
[assembly: InternalsVisibleTo("Uno.UI.Composition")]
[assembly: InternalsVisibleTo("Uno.UI.Composition.WebGpu.Init")]

// The graphics negotiator (GraphicsRegistry, GraphicsInitialization, GraphicsContextFactory) is framework
// host-plumbing, not third-party API, so it is internal + IVT'd to the Skia hosts; the third-party backend SPI
// (IGraphicsProvider/IGraphicsContext/GraphicsContextKind/IDrawingFactory) stays public.
[assembly: InternalsVisibleTo("Uno.UI.Runtime.Skia.X11")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.Skia.Win32")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.Skia.MacOS")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.Skia.WebAssembly.Browser")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.Skia.Linux.FrameBuffer")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.Skia.AppleUIKit")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.Skia.Android")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.Skia.Headless")]
