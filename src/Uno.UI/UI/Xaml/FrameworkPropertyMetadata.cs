using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
			Options = options;
		}

		public FrameworkPropertyMetadata(
			object defaultValue,
			FrameworkPropertyMetadataOptions options,
			PropertyChangedCallback propertyChangedCallback
		) : base(defaultValue, propertyChangedCallback)
		{
			Options = options;
		}

		internal FrameworkPropertyMetadata(
			object defaultValue,
			FrameworkPropertyMetadataOptions options,
			PropertyChangedCallback propertyChangedCallback,
			CoerceValueCallback coerceValueCallback
		) : base(defaultValue, propertyChangedCallback, coerceValueCallback)
		{
			Options = options;
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
			CoerceValueCallback coerceValueCallback
		) : base(defaultValue, propertyChangedCallback, coerceValueCallback)
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
			PropertyChangedCallback propertyChangedCallback,
			CoerceValueCallback coerceValueCallback,
			UpdateSourceTrigger defaultUpdateSourceTrigger
		) : base(defaultValue, propertyChangedCallback, coerceValueCallback)
		{
			Options = options;
			DefaultUpdateSourceTrigger = defaultUpdateSourceTrigger;
		}
		
		public FrameworkPropertyMetadataOptions Options { get; set; }

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
