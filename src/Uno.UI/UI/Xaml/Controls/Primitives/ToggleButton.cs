#nullable enable

namespace Windows.UI.Xaml.Controls.Primitives
{
	/// <summary>
	/// Represents a control that a user can select (check) or clear (uncheck).
	/// Base class for controls that can switch states, such as CheckBox and RadioButton.
	/// </summary>
	public partial class ToggleButton : ButtonBase, IFrameworkTemplatePoolAware
	{
		/// <summary>
		/// This is a workaround for the template pooling issue where we change IsChecked when the template is recycled.
		/// This prevents incorrect event raising but is not a "real" solution. Pooling could still cause issues.
		/// This workaround can be removed if pooling is removed. See https://github.com/unoplatform/uno/issues/12189
		/// </summary>
		private bool _suppressCheckedChanged;

		/// <summary>
		/// Initializes a new instance of the ToggleButton class.
		/// </summary>
		public ToggleButton()
		{
			DefaultStyleKey = typeof(ToggleButton);
		}

		/// <summary>
		/// Fires when a ToggleButton is checked.
		/// </summary>
		public event RoutedEventHandler? Checked;

		/// <summary>
		/// Fires when a ToggleButton is unchecked.
		/// </summary>
		public event RoutedEventHandler? Unchecked;

		/// <summary>
		/// Fires when the state of a ToggleButton is switched to the indeterminate state.
		/// </summary>
		public event RoutedEventHandler? Indeterminate;

		/// <summary>
		/// Gets or sets whether the ToggleButton is checked.
		/// </summary>
		public bool? IsChecked
		{
			get => (bool?)GetValue(IsCheckedProperty);
			set => SetValue(IsCheckedProperty, value);
		}

		/// <summary>
		/// Identifies the IsChecked dependency property.
		/// </summary>
		public static DependencyProperty IsCheckedProperty { get; } =
			DependencyProperty.Register(
				nameof(IsChecked),
				typeof(bool?),
				typeof(ToggleButton),
				new FrameworkPropertyMetadata(false)
			);

		/// <summary>
		/// Gets or sets a value that indicates whether the control supports three states.
		/// </summary>
		public bool IsThreeState
		{
			get => (bool)GetValue(IsThreeStateProperty);
			set => SetValue(IsThreeStateProperty, value);
		}

		/// <summary>
		/// Identifies the IsThreeState dependency property.
		/// </summary>
		public static DependencyProperty IsThreeStateProperty { get; } =
			DependencyProperty.Register(
				nameof(IsThreeState),
				typeof(bool),
				typeof(ToggleButton),
				new FrameworkPropertyMetadata(false));

		/// <summary>
		/// Called when the ToggleButton receives toggle stimulus.
		/// </summary>
		protected virtual void OnToggle() => OnToggleImpl();

		/// <summary>
		/// Determines if the current toggle button can revert its state when tapped.
		/// </summary>
		internal bool CanRevertState { get; set; } = true;

		public void OnTemplateRecycled()
		{
			try
			{
				_suppressCheckedChanged = true;
				IsChecked = false;
			}
			finally
			{
				_suppressCheckedChanged = false;
			}
		}
	}
}
