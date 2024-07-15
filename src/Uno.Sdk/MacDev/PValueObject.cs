// Original source: https://github.com/xamarin/Xamarin.MacDev

using System;

namespace Uno.Sdk.MacDev;

public abstract class PValueObject<T> : PObject, IPValueObject
{
	T val;
	public T Value
	{
		get => val;
		set
		{
			val = value;
			OnChanged(EventArgs.Empty);
		}
	}

	object IPValueObject.Value
	{
		get => Value!;
		set => Value = (T)value;
	}

	protected PValueObject(T value)
	{
		// Make sure the compiler understands that all fields have been assigned at the end of the constructor
		val = value;
		// This calls the OnChanged virtual method.
		Value = value;
	}

	public static implicit operator T(PValueObject<T> pObj) => pObj is not null ? pObj.Value : default!;

	public abstract bool TrySetValueFromString(string text, IFormatProvider formatProvider);
}
