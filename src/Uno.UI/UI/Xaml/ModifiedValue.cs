using System.Diagnostics;

namespace Windows.UI.Xaml;

internal sealed class ModifiedValue
{
	private object _animatedValue = DependencyProperty.UnsetValue;
	private object _baseValue = DependencyProperty.UnsetValue;

	internal bool LocalValueNewerThanAnimatedValue { get; private set; }

	private static int _localCanDefeatAnimationSuppressed;

	internal object CoercedValue { get; set; } = DependencyProperty.UnsetValue;

	internal bool IsAnimated => _animatedValue != DependencyProperty.UnsetValue;
	internal bool IsCoerced => CoercedValue != DependencyProperty.UnsetValue;

	internal static void SuppressLocalCanDefeatAnimations()
		=> _localCanDefeatAnimationSuppressed++;

	internal static void ContinueLocalCanDefeatAnimations()
		=> _localCanDefeatAnimationSuppressed--;

	public void SetAnimatedValue(object value)
	{
		LocalValueNewerThanAnimatedValue = false;
		_animatedValue = value;
	}

	public object GetAnimatedValue()
		=> _animatedValue;

	public void SetBaseValue(object value, DependencyPropertyValuePrecedences baseValueSource)
	{
		if (_localCanDefeatAnimationSuppressed == 0 &&
			baseValueSource == DependencyPropertyValuePrecedences.Local &&
			value != DependencyProperty.UnsetValue &&
			DependencyObjectStore.AreDifferent(_baseValue, value))
		{
			LocalValueNewerThanAnimatedValue = true;
		}
		else
		{
			// This might not make much sense, but this is what we are seeing in WinUI code.
			// See https://github.com/unoplatform/uno/issues/5168#issuecomment-1948115761
			// If it turned out there is more complexity going on in WinUI, we can adjust as needed.
			LocalValueNewerThanAnimatedValue = false;
		}

		_baseValue = value;
	}

	public object GetEffectiveValue()
	{
		if (CoercedValue != DependencyProperty.UnsetValue)
		{
			return CoercedValue;
		}
		else if (_animatedValue != DependencyProperty.UnsetValue)
		{
			// Comment originates from WinUI source code (CModifiedValue::GetEffectiveValue)
			// If a local value has been set after an animated value, the local
			// value has precedence. This is different from WPF and is done because
			// some legacy SL apps depend on this and because SL Animation thinks that
			// it is better design for an animation in filling period to be trumped by a
			// local value. In the active period of an animation, the next animated
			// value will take precedence over the old local value.
			if (LocalValueNewerThanAnimatedValue)
			{
				return _baseValue;
			}

			return _animatedValue;
		}

		return _baseValue;
	}

	public object GetBaseValue()
		=> _baseValue;
}
