using System.Collections.Generic;

namespace Microsoft.Windows.ApplicationModel.Resources;

/// <summary>
/// The interface that is implemented by the ResourceContext class,
/// which encapsulates all of the factors that might affect resource selection.
/// </summary>
public partial interface IResourceContext
{
	/// <summary>
	/// Gets a map of all supported qualifiers, indexed by name.
	/// </summary>
	IDictionary<string, string> QualifierValues { get; }
}
