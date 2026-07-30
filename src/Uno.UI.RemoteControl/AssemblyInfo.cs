using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Uno.UI.RuntimeTests")]
[assembly: InternalsVisibleTo("Uno.UI.RuntimeTests.HRApp.Skia")]
[assembly: InternalsVisibleTo("Uno.UI.RemoteControl.DevServer.Tests")]

// Tool & Resource Registry consumers/publishers: in-process add-ins that read the registry
// (Uno.UI.RemoteControl.Tools) and bridge it to their own transport. Access is granted here as
// part of wiring each integration (see spec 044, Appendix B).
[assembly: InternalsVisibleTo("Uno.UI.App.Mcp.Client")]
[assembly: InternalsVisibleTo("Uno.UI.HotDesign.Client")]
