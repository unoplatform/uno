using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Uno.UI")]
[assembly: InternalsVisibleTo("SamplesApp")]
[assembly: InternalsVisibleTo("SamplesApp.Droid")]

// https://github.com/dotnet/linker/issues/3112
[assembly: AssemblyMetadata("IsTrimmable", "False")]
