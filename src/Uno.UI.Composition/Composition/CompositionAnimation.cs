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
	internal Dictionary<string, float> ScalarParameters { get; } = new();

	internal event Action<CompositionAnimation>? PropertyChanged;

	public void SetReferenceParameter(string key, CompositionObject compositionObject)
	{
		ReferenceParameters[key] = compositionObject;
	}

	public void SetScalarParameter(string key, float value)
	{
		ScalarParameters[key] = value;
	}

	internal virtual object? Start() => null;
	internal virtual object? Evaluate() => null;
	internal virtual void Stop() { }

	private protected void RaisePropertyChanged() => PropertyChanged?.Invoke(this);
}
