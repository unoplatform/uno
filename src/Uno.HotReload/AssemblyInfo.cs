using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Uno.UI.SourceGenerators.Tests")]
[assembly: InternalsVisibleTo("Uno.UI.RemoteControl.Server.Processors")]
[assembly: InternalsVisibleTo("Uno.HotReload.Tests")]

// The assembly has never been audited for CLS compliance and is consumed only from C#.
// Declaring it explicitly is CA1014's documented resolution.
[assembly: CLSCompliant(false)]
