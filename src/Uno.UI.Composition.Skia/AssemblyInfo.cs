using global::System.Runtime.CompilerServices;

// The framework and hosts consume this backend only through its public seam; tests exercise concrete backends.
[assembly: InternalsVisibleTo("Uno.UI.RuntimeTests")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.Wayland.Tests")]
