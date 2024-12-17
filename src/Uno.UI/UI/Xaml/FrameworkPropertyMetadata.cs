using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace Windows.UI.Xaml
{
	/// <summary>
	/// Defines the metadata to use for a dependency property for framework elements
	/// </summary>
	/// <remarks>
	/// This class in not _UWP compatible_ and are used to make DependencyProperties with
	/// special abilities (like _inheritable properties_).
	/// It should be used only to create controls like those in UWP. You should not use
	/// this class in application code or they won't compile and work properly on UWP.
	/// </remarks>
	public class FrameworkPropertyMetadata : PropertyMetadata
	{
		public FrameworkPropertyMetadata(
			object defaultValue
		) : base(defaultValue)
		{
		}

		public FrameworkPropertyMetadata(
			object defaultValue,
			FrameworkPropertyMetadataOptions options
		) : base(defaultValue)
		{
			Options = options.WithDefault();
		}

		internal FrameworkPropertyMetadata(
			object defaultValue,
			CoerceValueCallback coerceValueCallback
		) : base(defaultValue, coerceValueCallback)
		{
		}

		public FrameworkPropertyMetadata(
			object defaultValue,
			FrameworkPropertyMetadataOptions options,
			PropertyChangedCallback propertyChangedCallback
		) : base(defaultValue, propertyChangedCallback)
		{
			Options = options.WithDefault();
		}

		internal FrameworkPropertyMetadata(PropertyChangedCallback propertyChangedCallback)
			: base(default, propertyChangedCallback)
		{
		}

		internal FrameworkPropertyMetadata(
			object defaultValue,
			FrameworkPropertyMetadataOptions options,
			PropertyChangedCallback propertyChangedCallback,
			BackingFieldUpdateCallback backingFieldUpdateCallback
		) : base(defaultValue, propertyChangedCallback, backingFieldUpdateCallback)
		{
			Options = options.WithDefault();
		}

		internal FrameworkPropertyMetadata(
			object defaultValue,
			BackingFieldUpdateCallback backingFieldUpdateCallback
		) : base(defaultValue, null, backingFieldUpdateCallback)
		{
		}

		internal FrameworkPropertyMetadata(
			object defaultValue,
			FrameworkPropertyMetadataOptions options,
			BackingFieldUpdateCallback backingFieldUpdateCallback
		) : base(defaultValue, null, backingFieldUpdateCallback)
		{
			Options = options.WithDefault();
		}

		internal FrameworkPropertyMetadata(
			object defaultValue,
			FrameworkPropertyMetadataOptions options,
			PropertyChangedCallback propertyChangedCallback,
			CoerceValueCallback coerceValueCallback
		) : base(defaultValue, propertyChangedCallback, coerceValueCallback, null)
		{
			Options = options.WithDefault();
		}

		internal FrameworkPropertyMetadata(
			object defaultValue,
			FrameworkPropertyMetadataOptions options,
			PropertyChangedCallback propertyChangedCallback,
			CoerceValueCallback coerceValueCallback,
			BackingFieldUpdateCallback backingFieldUpdateCallback
		) : base(defaultValue, propertyChangedCallback, coerceValueCallback, backingFieldUpdateCallback)
		{
			Options = options.WithDefault();
		}

		internal FrameworkPropertyMetadata(
			object defaultValue,
			FrameworkPropertyMetadataOptions options,
			PropertyChangedCallback propertyChangedCallback,
			CoerceValueCallback coerceValueCallback,
			BackingFieldUpdateCallback backingFieldUpdateCallback,
			CreateDefaultValueCallback createDefaultValueCallback
		) : base(defaultValue, propertyChangedCallback, coerceValueCallback, backingFieldUpdateCallback, createDefaultValueCallback)
		{
			Options = options.WithDefault();
		}

		internal FrameworkPropertyMetadata(
			object defaultValue,
			PropertyChangedCallback propertyChangedCallback
		) : base(defaultValue, propertyChangedCallback)
		{
		}

		internal FrameworkPropertyMetadata(
			object defaultValue,
			PropertyChangedCallback propertyChangedCallback,
			BackingFieldUpdateCallback backingFieldUpdateCallback
		) : base(defaultValue, propertyChangedCallback, backingFieldUpdateCallback)
		{
		}

		internal FrameworkPropertyMetadata(
			object defaultValue,
			PropertyChangedCallback propertyChangedCallback,
			CoerceValueCallback coerceValueCallback
		) : base(defaultValue, propertyChangedCallback, coerceValueCallback, null)
		{
		}

		internal FrameworkPropertyMetadata(
			PropertyChangedCallback propertyChangedCallback,
			CoerceValueCallback coerceValueCallback
		) : base(propertyChangedCallback, coerceValueCallback)
		{
		}

		internal FrameworkPropertyMetadata(
			object defaultValue,
			FrameworkPropertyMetadataOptions options,
			BackingFieldUpdateCallback backingFieldUpdateCallback,
			CoerceValueCallback coerceValueCallback
		) : base(defaultValue: defaultValue, propertyChangedCallback: null, coerceValueCallback: coerceValueCallback, backingFieldUpdateCallback: backingFieldUpdateCallback)
		{
			Options = options.WithDefault();
		}

		public FrameworkPropertyMetadataOptions Options { get; set; } = FrameworkPropertyMetadataOptions.Default;

		// Kept for binary compat only.
		// This property should be removed, and the whole FrameworkPropertyMetadata should be internal.
		public UpdateSourceTrigger DefaultUpdateSourceTrigger
		{
			get
			{
				if (this == TextBox.TextProperty.Metadata)
				{
					return UpdateSourceTrigger.Explicit;
				}

				return UpdateSourceTrigger.PropertyChanged;
			}
		}

		/// <summary>
		/// Temporary flag to opt-in for automatic parent propagation.
		/// </summary>
		internal bool IsLogicalChild
		{
			get => Options.HasLogicalChild();
			set => Options = value
				? Options |= FrameworkPropertyMetadataOptions.LogicalChild
				: Options &= ~FrameworkPropertyMetadataOptions.LogicalChild;
		}

		/// <summary>
		/// Determines if the storage of this property's value should use a <see cref="Uno.UI.DataBinding.ManagedWeakReference"/> backing
		/// </summary>
		public bool HasWeakStorage
		{
			get => Options.HasWeakStorage();
			set => Options = value
				? Options |= FrameworkPropertyMetadataOptions.WeakStorage
				: Options &= ~FrameworkPropertyMetadataOptions.WeakStorage;
		}

		internal override PropertyMetadata CloneWithOverwrittenDefaultValue(object newDefaultValue)
		{
			return new FrameworkPropertyMetadata(newDefaultValue, Options, PropertyChangedCallback, CoerceValueCallback, BackingFieldUpdateCallback, CreateDefaultValueCallback);
		}
	}
}
