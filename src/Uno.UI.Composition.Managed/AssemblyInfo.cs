using System.Runtime.CompilerServices;

// The managed drawing impls are mostly internal (only the seam entry points — ManagedGeometryFactory,
// ManagedFontProvider, ManagedImageDecoderBackend, ManagedSvgRenderer — are public, for app-head registration).
// The test projects exercise the internal engines directly (ManagedFont, ManagedGeometry, ManagedImageEncoder/Decoder),
// so they need access — mirrors the IVT they previously had on Uno.UI.Composition.
[assembly: InternalsVisibleTo("Uno.UI.RuntimeTests")]
[assembly: InternalsVisibleTo("Uno.UI.UnitTests")]
