#nullable enable

using System;
using System.Collections.Generic;
using Uno;

namespace Microsoft.UI.Composition;

public partial class CompositionAnimation
{
	internal CompositionAnimation() => throw new NotSupportedException("Use the ctor with Compositor");

	internal CompositionAnimation(Compositor compositor) : base(compositor)
	{
	}

	internal Dictionary<string, CompositionObject> ReferenceParameters { get; } = new();

	public void SetReferenceParameter(string key, CompositionObject compositionObject)
	{
		ReferenceParameters[key] = compositionObject;
	}
}
