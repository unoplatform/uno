using System.Diagnostics;

namespace Microsoft.UI.Xaml;

internal sealed class ModifiedValue
{
	private object _animatedValue = DependencyProperty.UnsetValue;
	private object _baseValue = DependencyProperty.UnsetValue;

	internal bool LocalValueNewerThanAnimatedValue { get; private set; }

	private static int _localCanDefeatAnimationSuppressed;

	internal object CoercedValue { get; set; } = DependencyProperty.UnsetValue;

	internal bool IsAnimated => _animatedValue != DependencyProperty.UnsetValue;
	internal bool IsCoerced => CoercedValue != DependencyProperty.UnsetValue;

	// MUX Reference: CModifiedValue::HasModifiers — ModifiedValue.cpp:25-28. fvsModifiersMask covers
	// fvsIsAnimated and fvsIsExpression, but fvsIsExpression is only ever set on the DXaml-layer
	// EffectiveValueEntry (EffectiveValueEntry.cpp:117) — on CModifiedValue it is exactly IsAnimated.
	// CoercedValue is Uno-only: WinUI does not keep coercion in CModifiedValue, so it must not count here.
	internal bool HasModifiers => IsAnimated;

	// MUX Reference: CModifiedValue::IsModifierValueBeingSet / SetModifierValueBeingSet — ModifiedValue.h:62-63.
	// Armed per property while the engine re-applies a theme-resolved value (Theming.cpp:355-362).
	internal bool IsModifierValueBeingSet { get; set; }

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
		// MUX: PropertySystem.cpp:1649-1652 — with the modifier-being-set gate armed WinUI skips this call
		// outright, so the local-newer bit keeps whatever it already held. It can also skip the value because
		// m_baseValue may hold the LIVE CThemeResource, re-resolved on read (CModifiedValue::GetBaseValue,
		// ModifiedValue.cpp:120-138). Uno stores the resolved snapshot, so the base value must still be
		// refreshed here or it would freeze at the pre-change theme once the animation is cleared.
		if (!IsModifierValueBeingSet)
		{
			if (_localCanDefeatAnimationSuppressed == 0 &&
				baseValueSource == DependencyPropertyValuePrecedences.Local &&
				value != DependencyProperty.UnsetValue &&
				DependencyObject.AreDifferent(_baseValue, value))
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
