using System;
using System.Collections.Generic;

namespace Uno.UI.Composition.Composition.Uno;

// very similar to an IObservable, but uses events instead (and leaves us open to extend if we need)
internal interface ISignal<T>
{
	event EventHandler<SignalChangedEventArgs<T>> Changed;

	T Value { get; }

	internal struct SignalChangedEventArgs<S>(S oldValue, S newValue)
	{
		public S OldValue { get; } = oldValue;
		public S NewValue { get; } = newValue;
	}
}

internal struct LeafOnlySignal<T> : ISignal<T>
{
	private T _field;

	T ISignal<T>.Value => _field;
	public event EventHandler<ISignal<T>.SignalChangedEventArgs<T>> Changed;

	public void Set(T value)
	{
		if (!EqualityComparer<T>.Default.Equals(_field, value))
		{
			var oldValue = value;
			_field = value;
			Changed?.Invoke(this, new ISignal<T>.SignalChangedEventArgs<T>(oldValue, value));
		}
	}

	public LeafOnlySignal(T initialValue)
	{
		_field = initialValue;
	}
}
