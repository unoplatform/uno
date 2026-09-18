using System;
using System.Reflection;
using SystemVersion = global::System.Version;

namespace Windows.ApplicationModel;

public sealed partial class PackageId
{
	public string Name { get; internal set; } = "";

	public PackageVersion Version { get; internal set; }

	public string Publisher { get; internal set; } = "";
}
