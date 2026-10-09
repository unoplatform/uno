using System.Runtime.CompilerServices;

// The managed drawing impls are internal: apps reach them through ManagedBackend's factories.
// The test projects exercise the internal engines directly (ManagedFont, ManagedGeometry, ManagedImageEncoder/Decoder),
// so they need access — mirrors the IVT they previously had on Uno.UI.Composition.
[assembly: InternalsVisibleTo("Uno.UI.RuntimeTests")]
[assembly: InternalsVisibleTo("Uno.UI.UnitTests")]
