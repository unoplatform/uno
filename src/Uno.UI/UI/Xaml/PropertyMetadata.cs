using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Windows.UI.Xaml
{
	/// <summary>
	/// Defines a delegate to be called when a property value changes.
	/// </summary>
	/// <param name="dependencyObject">The owner of the dependency property</param>
	/// <param name="args">The arguments for the changes of the property</param>
	public delegate void PropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args);

	/// <summary>
	/// Provides a template for a method that is called whenever a dependency property value is being re-evaluated, or coercion is specifically requested.
	/// </summary>
	/// <param name="dependencyObject">The object that the property exists on. When the callback is invoked, the property system will pass this value.</param>
	/// <param name="baseValue">The new value of the property, prior to any coercion attempt.</param>
	/// <param name="precedence">The precedence at which the new value is being set.</param>
	internal delegate object CoerceValueCallback(DependencyObject dependencyObject, object baseValue, DependencyPropertyValuePrecedences precedence);

	/// <summary>
	/// Provides a delegate for a method used to update backing fields of a dependency property
	/// </summary>
	/// <param name="dependencyObject">The object that the property exists on.</param>
	/// <param name="newValue">The new value of the property</param>
	internal delegate void BackingFieldUpdateCallback(DependencyObject dependencyObject, object newValue);

	/// <summary>
	/// Defines the metadata to use for a dependency property/
	/// </summary>
	public partial class PropertyMetadata
	{
		internal PropertyMetadata() { }

		public PropertyMetadata(
			object defaultValue
		)
		{
			DefaultValue = defaultValue;
		}

		internal PropertyMetadata(
			object defaultValue,
			CoerceValueCallback coerceValueCallback
		)
		{
			DefaultValue = defaultValue;
			CoerceValueCallback = coerceValueCallback;
		}

		internal PropertyMetadata(
			object defaultValue,
			BackingFieldUpdateCallback backingFieldUpdateCallback
		)
		{
			DefaultValue = defaultValue;
			BackingFieldUpdateCallback = backingFieldUpdateCallback;
		}

		public PropertyMetadata(
			object defaultValue,
			PropertyChangedCallback propertyChangedCallback
		)
		{
			DefaultValue = defaultValue;
			PropertyChangedCallback = propertyChangedCallback;
		}

		internal PropertyMetadata(
			object defaultValue,
			PropertyChangedCallback propertyChangedCallback,
			BackingFieldUpdateCallback backingFieldUpdateCallback
		)
		{
			DefaultValue = defaultValue;
			PropertyChangedCallback = propertyChangedCallback;
			BackingFieldUpdateCallback = backingFieldUpdateCallback;
		}

		public PropertyMetadata(
			PropertyChangedCallback propertyChangedCallback
		)
		{
			PropertyChangedCallback = propertyChangedCallback;
		}

		internal PropertyMetadata(
			PropertyChangedCallback propertyChangedCallback,
			CoerceValueCallback coerceValueCallback
		)
		{
			PropertyChangedCallback = propertyChangedCallback;
			CoerceValueCallback = coerceValueCallback;
		}

		internal PropertyMetadata(
			object defaultValue,
			PropertyChangedCallback propertyChangedCallback,
			CoerceValueCallback coerceValueCallback
		)
		{
			DefaultValue = defaultValue;
			PropertyChangedCallback = propertyChangedCallback;
			CoerceValueCallback = coerceValueCallback;
		}

		internal PropertyMetadata(
			object defaultValue,
			PropertyChangedCallback propertyChangedCallback,
			CoerceValueCallback coerceValueCallback,
			BackingFieldUpdateCallback backingFieldUpdateCallback
		)
		{
			DefaultValue = defaultValue;
			PropertyChangedCallback = propertyChangedCallback;
			CoerceValueCallback = coerceValueCallback;
			BackingFieldUpdateCallback = backingFieldUpdateCallback;
		}

		internal PropertyMetadata(
			object defaultValue,
			PropertyChangedCallback propertyChangedCallback,
			CoerceValueCallback coerceValueCallback,
			BackingFieldUpdateCallback backingFieldUpdateCallback,
			CreateDefaultValueCallback createDefaultValueCallback
		)
		{
			DefaultValue = defaultValue;
			PropertyChangedCallback = propertyChangedCallback;
			CoerceValueCallback = coerceValueCallback;
			BackingFieldUpdateCallback = backingFieldUpdateCallback;
			CreateDefaultValueCallback = createDefaultValueCallback;
		}

		private PropertyMetadata(CreateDefaultValueCallback createDefaultValueCallback)
		{
			CreateDefaultValueCallback = createDefaultValueCallback;
		}

		private PropertyMetadata(CreateDefaultValueCallback createDefaultValueCallback, PropertyChangedCallback propertyChangedCallback)
		{
			CreateDefaultValueCallback = createDefaultValueCallback;
			PropertyChangedCallback = propertyChangedCallback;
		}

		public CreateDefaultValueCallback CreateDefaultValueCallback { get; }

		public object DefaultValue { get; }

		public PropertyChangedCallback PropertyChangedCallback { get; internal set; }

		internal CoerceValueCallback CoerceValueCallback { get; set; }

		internal BackingFieldUpdateCallback BackingFieldUpdateCallback { get; set; }

		public static PropertyMetadata Create(object defaultValue)
			=> new PropertyMetadata(defaultValue: defaultValue);

		public static PropertyMetadata Create(object defaultValue, PropertyChangedCallback propertyChangedCallback)
			=> new PropertyMetadata(defaultValue: defaultValue, propertyChangedCallback: propertyChangedCallback);

		public static PropertyMetadata Create(CreateDefaultValueCallback createDefaultValueCallback)
			=> new PropertyMetadata(createDefaultValueCallback: createDefaultValueCallback);

		public static PropertyMetadata Create(CreateDefaultValueCallback createDefaultValueCallback, PropertyChangedCallback propertyChangedCallback)
			=> new PropertyMetadata(createDefaultValueCallback: createDefaultValueCallback, propertyChangedCallback: propertyChangedCallback);

		internal void MergePropertyChangedCallback(PropertyChangedCallback callback)
		{
			PropertyChangedCallback += callback;
		}

		internal bool HasPropertyChanged => PropertyChangedCallback is not null;

		internal void RaisePropertyChangedNoNullCheck(DependencyObject source, DependencyPropertyChangedEventArgs e)
		{
			PropertyChangedCallback.Invoke(source, e);
		}

		internal void RaiseBackingFieldUpdate(DependencyObject source, object newValue)
		{
			BackingFieldUpdateCallback?.Invoke(source, newValue);
		}

		internal virtual PropertyMetadata CloneWithOverwrittenDefaultValue(object newDefaultValue)
		{
			return new PropertyMetadata(newDefaultValue, PropertyChangedCallback, CoerceValueCallback, BackingFieldUpdateCallback, CreateDefaultValueCallback);
		}
	}
}
