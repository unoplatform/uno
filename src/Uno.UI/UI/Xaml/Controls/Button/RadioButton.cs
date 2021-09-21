using System;
using System.Collections.Generic;
using Uno.Disposables;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Controls
{
	/// <summary>
	/// Represents a button that allows a user to select a single option from a group of options.
	/// </summary>
	public partial class RadioButton : ToggleButton
	{
		/// <summary>
		/// Initializes a new instance of the RadioButton class.
		/// </summary>
		public RadioButton()
		{
			// When a Radio button is checked, clicking it again won't uncheck it.
			CanRevertState = false;
			DefaultStyleKey = typeof(RadioButton);
		}

		/// <summary>
		/// Gets or sets the name that specifies which RadioButton controls are mutually exclusive.
		/// </summary>
		/// <remarks>In line with UWP, the property returns empty string
		/// even if the actual group is null. Conversely, it is not possible
		/// to set it to <see langword="null"/> directly (only via SetValue).</remarks>
		public string GroupName
		{
			get => (string)GetValue(GroupNameProperty) ?? string.Empty;
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException(nameof(value));
				}

				SetValue(GroupNameProperty, value);
			}
		}

		/// <summary>
		/// Identifies the GroupName dependency property.
		/// </summary>
		public static DependencyProperty GroupNameProperty { get; } =
			DependencyProperty.Register(
				nameof(GroupName),
				typeof(string),
				typeof(RadioButton),
				new FrameworkPropertyMetadata(null)
			);

		private protected override void OnLoaded()
		{
			base.OnLoaded();

			UnregisterSafe(this);
			Register((string)GetValue(GroupNameProperty) ?? "", this);
		}

		private protected override void OnUnloaded()
		{
			base.OnUnloaded();

			Unregister((string)GetValue(GroupNameProperty) ?? "", this);
		}
	}
}
