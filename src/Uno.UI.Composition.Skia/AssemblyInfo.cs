using global::System.Runtime.CompilerServices;

// The framework and hosts consume this backend only through its public seam, so they get no IVT. The single grant
// is white-box test access: RuntimeTests constructs concrete backend types to prove the impl matches the seam.
[assembly: InternalsVisibleTo("Uno.UI.RuntimeTests")]
