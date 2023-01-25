using System;
using System.Collections.Generic;

namespace Windows.ApplicationModel.Resources.Core;

/// <summary>
/// Represents a single logical, named resource.
/// </summary>
public partial class NamedResource 
{
	/// <summary>
	/// Gets all possible candidate values for this named resource.
	/// </summary>
	public IReadOnlyList<ResourceCandidate> Candidates
	{
		get
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<ResourceCandidate> NamedResource.Candidates is not implemented in Uno.");
		}
	}

	public Uri Uri
	{
		get
		{
			throw new global::System.NotImplementedException("The member Uri NamedResource.Uri is not implemented in Uno.");
		}
	}

	public  ResourceCandidate Resolve()
	{
		throw new global::System.NotImplementedException("The member ResourceCandidate NamedResource.Resolve() is not implemented in Uno.");
	}

	public  ResourceCandidate Resolve( ResourceContext resourceContext)
	{
		throw new global::System.NotImplementedException("The member ResourceCandidate NamedResource.Resolve(ResourceContext resourceContext) is not implemented in Uno.");
	}

	public  global::System.Collections.Generic.IReadOnlyList<ResourceCandidate> ResolveAll()
	{
		throw new global::System.NotImplementedException("The member IReadOnlyList<ResourceCandidate> NamedResource.ResolveAll() is not implemented in Uno.");
	}

	public  global::System.Collections.Generic.IReadOnlyList<ResourceCandidate> ResolveAll( ResourceContext resourceContext)
	{
		throw new global::System.NotImplementedException("The member IReadOnlyList<ResourceCandidate> NamedResource.ResolveAll(ResourceContext resourceContext) is not implemented in Uno.");
	}
}
