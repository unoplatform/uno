#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.UI.Composition;

public partial class AnimationController : CompositionObject
{
	// A single AnimationController can drive MANY property animations at once. LottieGen output
	// (WinUIVersion 3.0) creates ONE controller, registers every keyframe animation against it via
	// CompositionObject.StartAnimation(property, animation, controller), then scrubs the whole
	// animation by expression-binding the controller's Progress to a root Progress property. Storing
	// a single association would freeze every animation except the last one registered.
	private readonly List<(CompositionObject Owner, string PropertyName, KeyFrameAnimation Animation)> _associations = new();

	private float? _progress;
	private float _playbackRate = 1.0f;
	// True after an explicit Pause(): setting Progress then HOLDS (scrub). While not paused, setting
	// Progress only re-positions and clock-driven playback (e.g. reverse PlaybackRate) continues.
	private bool _isPaused;

	internal AnimationController(CompositionObject ownerObject, string propertyName, KeyFrameAnimation animation) : base(ownerObject.Compositor)
	{
		Associate(ownerObject, propertyName, animation);
	}

	internal AnimationController(Compositor compositor) : base(compositor) { }

	internal void Initialize(CompositionObject ownerObject, string propertyName, KeyFrameAnimation animation)
	{
		Associate(ownerObject, propertyName, animation);

		// A newly-attached animation must inherit the controller's current state so all animations it
		// drives stay in sync (they are registered one after another, and Progress may already be set).
		animation.SetPlaybackRate(_playbackRate);
		if (_isPaused)
		{
			ownerObject.PauseAnimation(animation);
		}

		if (_progress is { } progress)
		{
			ownerObject.SeekAnimation(animation, progress);
		}
	}

	private void Associate(CompositionObject ownerObject, string propertyName, KeyFrameAnimation animation)
	{
		animation.Stopped += Animation_Stopped;
		_associations.Add((ownerObject, propertyName, animation));
	}

	public void Resume()
	{
		_isPaused = false;
		_progress = null;
		foreach (var (owner, _, animation) in _associations)
		{
			// Re-arm the compositor's frame-driven evaluation so the animation continues advancing.
			owner.ResumeAnimation(animation);
		}
	}

	public void Pause()
	{
		_isPaused = true;
		foreach (var (owner, _, animation) in _associations)
		{
			// Detach the compositor's frame-driven re-evaluation so it doesn't auto-stop or overwrite
			// externally-seeked progress while the controller is in charge.
			owner.PauseAnimation(animation);
		}
	}

	/// <summary>
	/// Gets the minimum playback rate supported by the controller.
	/// </summary>
	public static float MinPlaybackRate => -16f;

	/// <summary>
	/// Gets the maximum playback rate supported by the controller.
	/// </summary>
	public static float MaxPlaybackRate => 16f;

	public float PlaybackRate
	{
		get => _playbackRate;
		set
		{
			if (_playbackRate == value)
			{
				return;
			}

			_playbackRate = Math.Clamp(value, MinPlaybackRate, MaxPlaybackRate);
			OnPropertyChanged(nameof(PlaybackRate), false);
			foreach (var (_, _, animation) in _associations)
			{
				animation.SetPlaybackRate(_playbackRate);
			}
		}
	}

	public float Progress
	{
		get => _progress ?? (_associations.Count > 0 ? _associations[0].Animation.Progress : 0f);
		set
		{
			var clampedProgress = Math.Clamp(value, 0.0f, 1.0f);
			_progress = clampedProgress;
			OnPropertyChanged(nameof(Progress), false);
			foreach (var (owner, _, animation) in _associations)
			{
				// Paused controller = scrubbing (hold). Running controller = reposition and keep playing
				// (this is how AnimatedVisualPlayer starts a reverse play at the end).
				if (_isPaused)
				{
					owner.SeekAnimation(animation, clampedProgress);
				}
				else
				{
					owner.SeekAnimationProgress(animation, clampedProgress);
				}
			}
		}
	}

	internal TimeSpan Remaining => _associations.Count > 0 ? _associations[0].Animation.Remaining : TimeSpan.Zero;

	internal override object GetAnimatableProperty(string propertyName, string subPropertyName)
	{
		if (propertyName.Equals(nameof(Progress), StringComparison.OrdinalIgnoreCase))
		{
			return Progress;
		}
		else
		{
			return base.GetAnimatableProperty(propertyName, subPropertyName);
		}
	}

	private protected override void SetAnimatableProperty(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> subPropertyName, object? propertyValue)
	{
		if (propertyName.Equals(nameof(Progress), StringComparison.OrdinalIgnoreCase))
		{
			Progress = SubPropertyHelpers.ValidateValue<float>(propertyValue);
		}
		else
		{
			base.SetAnimatableProperty(propertyName, subPropertyName, propertyValue);
		}
	}

	private void Animation_Stopped(object? sender, EventArgs e)
	{
		if (sender is KeyFrameAnimation animation)
		{
			for (var i = _associations.Count - 1; i >= 0; i--)
			{
				if (ReferenceEquals(_associations[i].Animation, animation))
				{
					_associations.RemoveAt(i);
				}
			}
		}

		if (_associations.Count == 0)
		{
			_progress = null;
		}
	}
}
