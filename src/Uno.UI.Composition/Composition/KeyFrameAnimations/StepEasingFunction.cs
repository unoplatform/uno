using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Composition;

namespace Microsoft.UI.Composition;

public partial class StepEasingFunction
{
	private int _stepCount;
	private int _finalStep;
	private int _initialStep;
	private bool _isFinalStepSingleFrame;
	private bool _isInitialStepSingleFrame;

	private int _userFinalStep;
	private int _userInitialStep;
	private bool _initialStepAdjusted;
	private bool _finalStepAdjusted;
	private float _length;

	internal StepEasingFunction(Compositor owner, int stepCount = 1) : base(owner)
	{
		StepCount = stepCount;
		InitialStep = 0;
		FinalStep = stepCount;
		IsFinalStepSingleFrame = false;
		IsInitialStepSingleFrame = false;

		EnsureAnimationParameters();
	}

	public int StepCount
	{
		get => _stepCount;
		set
		{
			_stepCount = value;
			EnsureAnimationParameters();
		}
	}

	public int FinalStep
	{
		get => _userFinalStep;
		set
		{
			_finalStep = value;

			_finalStepAdjusted = false;
			EnsureAnimationParameters();
		}
	}

	public int InitialStep
	{
		get => _userInitialStep;
		set
		{
			_initialStep = value;

			_initialStepAdjusted = false;
			EnsureAnimationParameters();
		}
	}

	public bool IsFinalStepSingleFrame
	{
		get => _isFinalStepSingleFrame;
		set
		{
			_isFinalStepSingleFrame = value;
			EnsureAnimationParameters();
		}
	}

	public bool IsInitialStepSingleFrame
	{
		get => _isInitialStepSingleFrame;
		set
		{
			_isInitialStepSingleFrame = value;
			EnsureAnimationParameters();
		}
	}

	private void EnsureAnimationParameters()
	{
		if (_stepCount < 1)
		{
			_stepCount = 1;
		}

		_initialStep = Math.Clamp(_initialStep, 0, _stepCount);
		_finalStep = Math.Clamp(_finalStep, 0, _stepCount);

		if (((_finalStep - _initialStep) == 1) && _isInitialStepSingleFrame && _isFinalStepSingleFrame)
		{
			_isFinalStepSingleFrame = false;
		}
		else if (((_finalStep - _initialStep) == 0) && (_isInitialStepSingleFrame || _isFinalStepSingleFrame))
		{
			_isInitialStepSingleFrame = false;
			_isFinalStepSingleFrame = false;
		}

		_userInitialStep = _initialStep;
		_userFinalStep = _finalStep;

		if (_isInitialStepSingleFrame && !_initialStepAdjusted)
		{
			_initialStep++;
			_initialStepAdjusted = true;
		}

		if (_isFinalStepSingleFrame && !_finalStepAdjusted)
		{
			_finalStep--;
			_finalStepAdjusted = true;
		}

		_length = 1.0f / (_finalStep - _initialStep + 1);
	}

	internal override float Ease(float t)
	{
		var segment = (int)MathF.Floor(t / _length);

		if (t == 1.0f)
		{
			segment--;
		}

		var step = _initialStep + segment;

		if ((t == 0.0f) && _isInitialStepSingleFrame)
		{
			step--;
		}
		else if ((t == 1.0f) && _isFinalStepSingleFrame)
		{
			step++;
		}

		return (float)step / (float)_stepCount;
	}
}
