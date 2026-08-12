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

	public void ClearAllParameters()
	{
		ReferenceParameters.Clear();
		ScalarParameters.Clear();
		Vector2Parameters.Clear();
		Vector3Parameters.Clear();
	}

	public string Target
	{
		get => _target;
		set => _target = value ?? throw new ArgumentException();
	}

	// Targets this animation is currently started on. The same animation instance can be
	// shared across multiple CompositionObjects (e.g. LottieGen's shared progress
	// ExpressionAnimation started on many controllers), so we track every active target
	// rather than only the last one — otherwise teardown only ever unregisters the last.
	private readonly List<CompositionObject> _startedObjects = new();

	// The object this animation was last started on; resolves 'this.Target' in expression keyframes.
	internal CompositionObject? AnimationTargetObject { get; private set; }

	// Number of targets this animation is currently started on. Start/Stop are balanced one-to-one
	// per target, so this reaches zero only once every target has stopped.
	internal int StartedObjectCount => _startedObjects.Count;

	internal virtual object? Start(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> subPropertyName, CompositionObject compositionObject)
	{
		AnimationTargetObject = compositionObject;
		_startedObjects.Add(compositionObject);
#if __SKIA__
		Compositor.RegisterAnimation(this, compositionObject);
#endif
		return null;
	}

	internal virtual object? Evaluate() => null;

	internal virtual void Stop()
	{
		if (_startedObjects.Count == 0)
		{
			return;
		}

		// Stop() carries no target — CompositionObject.StopAnimation calls it without one — and
		// Start/Stop are balanced one-to-one per target, so unwind the most recent registration.
		var index = _startedObjects.Count - 1;
#if __SKIA__
		Compositor.UnregisterAnimation(this, _startedObjects[index]);
#endif
		_startedObjects.RemoveAt(index);
	}

	internal void RaiseAnimationFrame() => AnimationFrame?.Invoke(this);

	// WinUI snapshots an animation's state when it is started on a target, so a single instance can be
	// reconfigured and started on many targets (e.g. LottieGen's _reusableExpressionAnimation, which
	// sets a different Expression + reference parameters per shape). Animations with immutable state
	// (keyframe animations) share the instance; ExpressionAnimation overrides this to snapshot.
	internal virtual CompositionAnimation CloneAnimation() => this;

	private protected void CopyParametersTo(CompositionAnimation other)
	{
		foreach (var (key, value) in ReferenceParameters)
		{
			other.ReferenceParameters[key] = value;
		}

		foreach (var (key, value) in ScalarParameters)
		{
			other.ScalarParameters[key] = value;
		}

		foreach (var (key, value) in Vector2Parameters)
		{
			other.Vector2Parameters[key] = value;
		}

		foreach (var (key, value) in Vector3Parameters)
		{
			other.Vector3Parameters[key] = value;
		}

		other.Target = Target;
	}
}
