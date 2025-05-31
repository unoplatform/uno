﻿#nullable enable

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

	internal event Action<CompositionAnimation>? AnimationFrame;

	internal virtual bool IsTrackedByCompositor => false;

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

#if __SKIA__
	// TODO: This should not be here as it is possible to re-use animations across
	// CompositionObjects.
	CompositionObject? _currentCompositionObject;
#endif

	internal virtual object? Start(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> subPropertyName, CompositionObject compositionObject)
	{
#if __SKIA__
		_currentCompositionObject = compositionObject;
		Compositor.RegisterAnimation(this, compositionObject);
#endif
		return null;
	}

	internal virtual object? Evaluate() => null;

	internal virtual void Stop()
	{
#if __SKIA__
		if (_currentCompositionObject is not null)
		{
			Compositor.UnregisterAnimation(this, _currentCompositionObject);
		}
#endif
	}

	internal void RaiseAnimationFrame() => AnimationFrame?.Invoke(this);
}
