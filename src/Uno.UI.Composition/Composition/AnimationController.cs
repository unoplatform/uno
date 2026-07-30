#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.UI.Composition;

public partial class AnimationController
{
	private CompositionObject? _ownerObject;
	private string? _propertyName;
	private KeyFrameAnimation? _animation;

	private float? _progress;

	// TODO: Support multiple KeyFrameAnimation association like on Windows

	internal AnimationController(CompositionObject ownerObject, string propertyName, KeyFrameAnimation animation) : base(ownerObject.Compositor)
	{
		_ownerObject = ownerObject;
		_propertyName = propertyName;
		_animation = animation;

		_animation.Stopped += Animation_Stopped;
	}

	internal AnimationController(Compositor compositor) : base(compositor) { }

	internal void Initialize(CompositionObject ownerObject, string propertyName, KeyFrameAnimation animation)
	{
		if (_animation is not null)
		{
			_animation.Stopped -= Animation_Stopped;
		}

		_ownerObject = ownerObject;
		_propertyName = propertyName;
		_animation = animation;
		_progress = null;

		_animation.Stopped += Animation_Stopped;
	}

	public void Resume()
	{
		_progress = null;
		var animation = EnsureAnimation();
		// Re-arm the compositor's frame-driven evaluation so the animation continues advancing.
		_ownerObject?.ResumeAnimation(animation);
	}

	public void Pause()
	{
		var animation = EnsureAnimation();
		// Detach the compositor's frame-driven re-evaluation so it doesn't auto-stop or
		// overwrite externally-seeked progress while the controller is in charge.
		_ownerObject?.PauseAnimation(animation);
	}

	public float Progress
	{
		get => _progress is not null ? _progress.Value : EnsureAnimation().Progress;
		set
		{
			var animation = EnsureAnimation();
			_progress = value;
			OnPropertyChanged(nameof(Progress), false);
			_ownerObject?.SeekAnimation(animation, value);
		}
	}

	internal TimeSpan Remaining => EnsureAnimation().Remaining;

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
		_animation = null;
		_progress = null;
	}

	private KeyFrameAnimation EnsureAnimation()
	{
		if (_ownerObject is null || _propertyName is null)
		{
			throw new InvalidOperationException("The AnimationController has not been associated with a target object or animation");
		}

		if (_animation == null)
		{
			_animation = _ownerObject.GetKeyFrameAnimation(_propertyName);
		}

		if (_animation == null)
		{
			throw new InvalidOperationException($"No animation is running on the target object for property {_propertyName}");
		}

		return _animation;
	}
}
