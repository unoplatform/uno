using global::System.Runtime.CompilerServices;

// The Skia backend stands on the public neutral seam: the framework (Uno.UI) and every Runtime.Skia host wire it up
// through the public SkiaGraphicsProvider/SkiaBackend surface or the host builder's reflective resolution — none of
// them touch a backend internal, so no IVT is granted to them. The only grant is white-box test access: RuntimeTests
// constructs concrete backend types (SkiaFont/SkiaImage/SkiaDrawingSession/SkiaGeometrySource2D) to prove the impl
// matches the neutral seam.
[assembly: InternalsVisibleTo("Uno.UI.RuntimeTests")]
