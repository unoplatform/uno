#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using Microsoft.UI.Dispatching;
using Uno.Foundation.Logging;
using Windows.Foundation.Metadata;
using Windows.UI;
using Windows.UI.Core;

namespace Microsoft.UI.Composition
{
	public partial class CompositionObject : IDisposable
	{
		private readonly ContextStore _contextStore = new ContextStore();
		private CompositionPropertySet? _properties;
		private Dictionary<string, CompositionAnimation>? _animations;

		internal CompositionObject()
		{
			ApiInformation.TryRaiseNotImplemented(GetType().FullName!, "The compositor constructor is not available, as the type is not implemented");
			Compositor = new Compositor();
		}

		internal CompositionObject(Compositor compositor)
		{
			Compositor = compositor;
		}

		public CompositionPropertySet Properties => _properties ??= GetProperties();

		public Compositor Compositor { get; }

		public CoreDispatcher Dispatcher => CoreDispatcher.Main;

		public string? Comment { get; set; }

		private CompositionPropertySet GetProperties()
		{
			if (this is CompositionPropertySet @this)
			{
				return @this;
			}

			return new CompositionPropertySet(Compositor);
		}

		private protected T ValidateValue<T>(object? value)
		{
			if (value is not T t)
			{
				if (Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture) is T changed)
				{
					return changed;
				}

				throw new ArgumentException($"Cannot convert value of type '{value?.GetType()}' to {typeof(T)}");
			}

			return t;
		}

		// Overrides are based on:
		// https://learn.microsoft.com/en-us/uwp/api/windows.ui.composition.compositionobject.startanimation?view=winrt-22621
		private protected virtual bool IsAnimatableProperty(ReadOnlySpan<char> propertyName)
		{
			// Note: We don't care about the parameter type. So, anything can be used (here we are using bool).
			// It only decides whether the return status is Succeeded or TypeMismatch, which are equally treated as we are looking for
			// anything other than NotFound.
			return _properties is not null && _properties.TryGetValue<bool>(propertyName.ToString(), out _) != CompositionGetValueStatus.NotFound;
		}

		private protected virtual void SetAnimatableProperty(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> subPropertyName, object? propertyValue)
			=> TryUpdateFromProperties(propertyName, subPropertyName, propertyValue);

		public void StartAnimation(string propertyName, CompositionAnimation animation)
		{
			ReadOnlySpan<char> firstPropertyName;
			ReadOnlySpan<char> subPropertyName;
			var firstDotIndex = propertyName.IndexOf('.');
			if (firstDotIndex > -1)
			{
				firstPropertyName = propertyName.AsSpan().Slice(0, firstDotIndex);
				subPropertyName = propertyName.AsSpan().Slice(firstDotIndex + 1);
			}
			else
			{
				firstPropertyName = propertyName;
				subPropertyName = default;
			}

			if (!IsAnimatableProperty(firstPropertyName))
			{
				throw new ArgumentException($"Property '{firstPropertyName}' is not animatable.");
			}

			if (_animations?.ContainsKey(propertyName) == true)
			{
				StopAnimation(propertyName);
			}

			_animations ??= new();
			_animations[propertyName] = animation;
			animation.PropertyChanged += ReEvaluateAnimation;
			var animationValue = animation.Start();

			try
			{
				this.SetAnimatableProperty(firstPropertyName, subPropertyName, animationValue);
			}
			catch (Exception ex)
			{
				// Important to catch the exception.
				// It can currently happen for non-implemented animations which will evaluate to null and the target animation property is value type.
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().LogError($"An exception occurred while setting animation value '{animationValue}' to property '{propertyName}' for animation '{animation}'. {ex.Message}");
				}
			}
		}

		private void ReEvaluateAnimation(CompositionAnimation animation)
		{
			if (_animations == null)
			{
				return;
			}

			foreach (var (key, value) in _animations)
			{
				if (value == animation)
				{
					var propertyName = key;
					ReadOnlySpan<char> firstPropertyName;
					ReadOnlySpan<char> subPropertyName;
					var firstDotIndex = propertyName.IndexOf('.');
					if (firstDotIndex > -1)
					{
						firstPropertyName = propertyName.AsSpan().Slice(0, firstDotIndex);
						subPropertyName = propertyName.AsSpan().Slice(firstDotIndex + 1);
					}
					else
					{
						firstPropertyName = propertyName;
						subPropertyName = default;
					}

					this.SetAnimatableProperty(firstPropertyName, subPropertyName, animation.Evaluate());
				}
			}
		}

		public void StopAnimation(string propertyName)
		{
			if (_animations?.TryGetValue(propertyName, out var animation) == true)
			{
				animation.PropertyChanged -= ReEvaluateAnimation;
				animation.Stop();
				_animations.Remove(propertyName);
			}
		}

		public void Dispose() => DisposeInternal();

		private protected virtual void DisposeInternal()
		{
		}

		internal virtual void StartAnimationCore(string propertyName, CompositionAnimation animation)
		{

		}

		internal void AddContext(CompositionObject context, string? propertyName)
		{
			_contextStore.AddContext(context, propertyName);
		}

		internal void RemoveContext(CompositionObject context, string? propertyName)
		{
			_contextStore.RemoveContext(context, propertyName);
		}

		private protected void SetProperty(ref bool field, bool value, [CallerMemberName] string? propertyName = null)
		{
			if (field == value)
			{
				return;
			}

			field = value;

			OnPropertyChanged(propertyName, false);
		}

		private protected void SetProperty(ref int field, int value, [CallerMemberName] string? propertyName = null)
		{
			if (field == value)
			{
				return;
			}

			field = value;

			OnPropertyChanged(propertyName, false);
		}

		private protected void SetProperty(ref float field, float value, [CallerMemberName] string? propertyName = null)
		{
			if (field.Equals(value))
			{
				return;
			}

			field = value;

			OnPropertyChanged(propertyName, false);
		}

		private protected void SetProperty(ref Matrix3x2 field, Matrix3x2 value, [CallerMemberName] string? propertyName = null)
		{
			if (field.Equals(value))
			{
				return;
			}

			field = value;

			OnPropertyChanged(propertyName, false);
		}

		private protected void SetProperty(ref Matrix4x4 field, Matrix4x4 value, [CallerMemberName] string? propertyName = null)
		{
			if (field.Equals(value))
			{
				return;
			}

			field = value;

			OnPropertyChanged(propertyName, false);
		}

		private protected void SetProperty(ref Vector2 field, Vector2 value, [CallerMemberName] string? propertyName = null)
		{
			if (field.Equals(value))
			{
				return;
			}

			field = value;

			OnPropertyChanged(propertyName, false);
		}

		private protected void SetProperty(ref Vector3 field, Vector3 value, [CallerMemberName] string? propertyName = null)
		{
			if (field.Equals(value))
			{
				return;
			}

			field = value;

			OnPropertyChanged(propertyName, false);
		}

		private protected void SetProperty(ref Quaternion field, Quaternion value, [CallerMemberName] string? propertyName = null)
		{
			if (field.Equals(value))
			{
				return;
			}

			field = value;

			OnPropertyChanged(propertyName, false);
		}

		private protected void SetProperty(ref Color field, Color value, [CallerMemberName] string? propertyName = null)
		{
			if (field == value)
			{
				return;
			}

			field = value;

			OnPropertyChanged(propertyName, false);
		}

		private protected void SetEnumProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
			where T : Enum
		{
			if (EqualityComparer<T>.Default.Equals(field, value))
			{
				return;
			}

			field = value;

			OnPropertyChanged(propertyName, false);
		}

		private protected void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
			where T : CompositionObject?
		{
			if (field == value)
			{
				return;
			}

			OnCompositionPropertyChanged(field, value, propertyName);

			field = value;

			OnPropertyChanged(propertyName, false);
		}

		private protected void SetObjectProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
		{
			if (field?.Equals(value) ?? value == null)
			{
				return;
			}

			// This check is here for backward compatibility
			// Is this valid even for non-composition objects like interface?
			var fieldCO = field as CompositionObject;
			var valueCO = value as CompositionObject;
			if (fieldCO != null || value != null)
			{
				OnCompositionPropertyChanged(fieldCO, valueCO, propertyName);
			}

			field = value;

			OnPropertyChanged(propertyName, false);
		}

		private protected void OnChanged() => OnPropertyChanged(null, false);

		private protected void OnCompositionPropertyChanged(CompositionObject? oldValue, CompositionObject? newValue) => OnCompositionPropertyChanged(oldValue, newValue, null);

		private protected void OnCompositionPropertyChanged(CompositionObject? oldValue, CompositionObject? newValue, string? propertyName)
		{
			if (oldValue != null)
			{
				oldValue.RemoveContext(this, propertyName);
			}

			if (newValue != null)
			{
				newValue.AddContext(this, propertyName);
			}
		}

		private protected void OnPropertyChanged(string? propertyName, bool isSubPropertyChange)
		{
			OnPropertyChangedCore(propertyName, isSubPropertyChange);
			_contextStore.RaiseChanged();
		}

		private protected virtual void OnPropertyChangedCore(string? propertyName, bool isSubPropertyChange)
		{
		}
	}
}
