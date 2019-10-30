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
	/// <param name="args">The new value of the property, prior to any coercion attempt.</param>
	internal delegate object CoerceValueCallback(DependencyObject dependencyObject, object baseValue);

	/// <summary>
	/// Defines the metadata to use for a dependency property/
	/// </summary>
	public partial class PropertyMetadata
	{
		private bool _isDefaultValueSet;
		private object _defaultValue;

		private bool _isCoerceValueCallbackSet;
		private CoerceValueCallback _coerceValueCallback;

		/// <summary>
		/// Should <see cref="CoerceValueCallback"/> be raised even if value has not changed?
		/// </summary>
		internal bool CoerceWhenUnchanged { get; set; } = true;

		internal PropertyMetadata() { }

		public PropertyMetadata(
			object defaultValue
		)			
		{
			DefaultValue = defaultValue;
		}

		public PropertyMetadata(
			object defaultValue,
			PropertyChangedCallback propertyChangedCallback
		)
		{
			DefaultValue = defaultValue;
			PropertyChangedCallback = propertyChangedCallback;
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

		public object DefaultValue
		{
			get => _defaultValue;
			internal set
			{
				_defaultValue = value;
				_isDefaultValueSet = true;
			}
		}

		public PropertyChangedCallback PropertyChangedCallback { get; internal set; }

		internal CoerceValueCallback CoerceValueCallback
		{
			get => _coerceValueCallback;
			set
			{
				_coerceValueCallback = value;
				_isCoerceValueCallbackSet = true;
			}
		}

		internal protected virtual void Merge(PropertyMetadata baseMetadata, DependencyProperty dp)
		{
			// The supplied metadata is merged with the property metadata for 
			// the dependency property as it exists on the base owner. Any 
			// characteristics that were specified in the original base 
			// metadata will persist; only those characteristics that were 
			// specifically changed in the new metadata will override the 
			// characteristics of the base metadata. Some characteristics such
			// as DefaultValue are replaced if specified in the new metadata. 
			// Others, such as PropertyChangedCallback, are combined. 
			// Ultimately, the merge behavior depends on the property metadata 
			// type being used for the override, so the behavior described here 
			// is for the existing property metadata classes used by WPF 
			// dependency properties. For details, see Dependency Property 
			// Metadata and Framework Property Metadata.
			// Source: https://msdn.microsoft.com/en-us/library/ms597491(v=vs.110).aspx

			if (!_isCoerceValueCallbackSet)
			{
				CoerceValueCallback = baseMetadata.CoerceValueCallback;
			}

			if (!_isDefaultValueSet)
			{
				DefaultValue = baseMetadata.DefaultValue;
			}
			
			// Merge PropertyChangedCallback delegates
			PropertyChangedCallback = baseMetadata.PropertyChangedCallback + PropertyChangedCallback;
		}

		internal void MergePropertyChangedCallback(PropertyChangedCallback callback)
		{
			PropertyChangedCallback += callback;
		}


		internal void RaisePropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
		{
			PropertyChangedCallback?.Invoke(source, e);
		}
	}
}
