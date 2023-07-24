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
		private bool _isDefaultUpdateSourceTriggerSet;
		private UpdateSourceTrigger _defaultUpdateSourceTrigger;

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

		internal FrameworkPropertyMetadata(
			object defaultValue,
			FrameworkPropertyMetadataOptions options,
			PropertyChangedCallback propertyChangedCallback,
			CoerceValueCallback coerceValueCallback,
			UpdateSourceTrigger defaultUpdateSourceTrigger
		) : base(defaultValue, propertyChangedCallback, coerceValueCallback, null)
		{
			Options = options.WithDefault();
			DefaultUpdateSourceTrigger = defaultUpdateSourceTrigger;
		}

		public FrameworkPropertyMetadataOptions Options { get; set; } = FrameworkPropertyMetadataOptions.Default;

		public UpdateSourceTrigger DefaultUpdateSourceTrigger
		{
			get
			{
				// UpdateSourceTrigger.Default doesn't make sense as a value for DefaultUpdateSourceTrigger,
				// as it is usually used to indicate that a binding should use DefaultUpdateSourceTrigger,
				// which should be either UpdateSourceTrigger.PropertyChanged (by default) or UpdateSourceTrigger.Explicit.
				return _defaultUpdateSourceTrigger == UpdateSourceTrigger.Default
					? UpdateSourceTrigger.PropertyChanged
					: _defaultUpdateSourceTrigger;
			}
			private set
			{
				_defaultUpdateSourceTrigger = value;
				_isDefaultUpdateSourceTriggerSet = true;
			}
		}

		protected internal override void Merge(PropertyMetadata baseMetadata, DependencyProperty dp)
		{
			base.Merge(baseMetadata, dp);

			if (baseMetadata is FrameworkPropertyMetadata baseFrameworkMetadata)
			{
				if (!_isDefaultUpdateSourceTriggerSet)
				{
					DefaultUpdateSourceTrigger = baseFrameworkMetadata.DefaultUpdateSourceTrigger;
				}

				// Merge options flags
				Options |= baseFrameworkMetadata.Options;
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
	}
}
