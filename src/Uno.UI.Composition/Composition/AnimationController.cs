#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.UI.Composition;

public partial class AnimationController
{
	private readonly CompositionObject _ownerObject;
	private readonly string _propertyName;
	private KeyFrameAnimation? _animation;

	private float? _progress;

	internal AnimationController(CompositionObject ownerObject, string propertyName, KeyFrameAnimation animation) : base(ownerObject.Compositor)
	{
		_ownerObject = ownerObject;
		_propertyName = propertyName;
		_animation = animation;

		_animation.Stopped += Animation_Stopped;
	}

	public void Resume()
	{
		_progress = null;
		EnsureAnimation().Resume();
	}

	public void Pause() => EnsureAnimation().Pause();

	public float Progress
	{
		get => _progress is not null ? _progress.Value : EnsureAnimation().Progress;
		set
		{
			var animation = EnsureAnimation();
			_progress = value;
			OnPropertyChanged(nameof(Progress), false);
			_ownerObject.SeekAnimation(animation, value);
		}
	}

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
