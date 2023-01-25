using System.Collections;
using System.Collections.Generic;

namespace Windows.ApplicationModel.Resources.Core;

/// <summary>
/// Represents a collection of ResourceCandidate objects.
/// </summary>
public partial class ResourceCandidateVectorView : IReadOnlyList<ResourceCandidate>, IEnumerable<ResourceCandidate>
{
	private readonly IReadOnlyList<ResourceCandidate> _candidates;

	internal ResourceCandidateVectorView(IReadOnlyList<ResourceCandidate> candidates)
	{
		_candidates = candidates;
	}

	public ResourceCandidate this[int index] => _candidates[index];
	
	public int Count => _candidates.Count;

	public uint Size => (uint)Count;
	
	public IEnumerator<ResourceCandidate> GetEnumerator() => _candidates.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
