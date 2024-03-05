#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;
using Uno;

namespace Microsoft.UI.Composition;

public partial class CompositionAnimation
{
	private string _target = "";

	internal CompositionAnimation() => throw new NotSupportedException("Use the ctor with Compositor");

	internal CompositionAnimation(Compositor compositor) : base(compositor)
	{
	}

	// TODO: Consolidate into a single dictionary
	internal Dictionary<string, CompositionObject> ReferenceParameters { get; } = new();
	internal Dictionary<string, float> ScalarParameters { get; } = new();
	internal Dictionary<string, Vector2> Vector2Parameters { get; } = new();
	internal Dictionary<string, Vector3> Vector3Parameters { get; } = new();

	internal event Action<CompositionAnimation>? PropertyChanged;

	public void SetReferenceParameter(string key, CompositionObject compositionObject)
	{
		ReferenceParameters[key] = compositionObject;
	}

	public void SetScalarParameter(string key, float value)
	{
		ScalarParameters[key] = value;
	}

	public void SetVector2Parameter(string key, Vector2 value)
	{
		Vector2Parameters[key] = value;
	}

	public void SetVector3Parameter(string key, Vector3 value)
	{
		Vector3Parameters[key] = value;
	}

	public string Target
	{
		get => _target;
		set => _target = value ?? throw new ArgumentException();
	}

	internal virtual object? Start() => null;
	internal virtual object? Evaluate() => null;
	internal virtual void Stop() { }

	private protected void RaisePropertyChanged() => PropertyChanged?.Invoke(this);
}
