using System;
using System.Drawing;
using Uno.Disposables;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Controls.Primitives
{
	public partial class ToggleButton : ButtonBase, IFrameworkTemplatePoolAware
	{
		public event RoutedEventHandler Checked;
		public event RoutedEventHandler Unchecked;
		public event RoutedEventHandler Indeterminate;

		public ToggleButton()
		{
			InitializeVisualStates();

			Click += (s, e) =>
			{
				OnToggle();
			};

			DefaultStyleKey = typeof(ToggleButton);
		}

		/// <summary>
		/// Determines if the current toggle button can revert its state when tapped.
		/// </summary>
		internal bool CanRevertState { get; set; } = true;

		public bool IsThreeState
		{
			get => (bool)this.GetValue(IsThreeStateProperty);
			set => this.SetValue(IsThreeStateProperty, value);
		}

		public static DependencyProperty IsThreeStateProperty { get; } =
			DependencyProperty.Register(
				name: nameof(IsThreeState),
				propertyType: typeof(bool),
				ownerType: typeof(ToggleButton),
				typeMetadata: new FrameworkPropertyMetadata(
					defaultValue: false));


		public bool? IsChecked
		{
			get => (bool?)this.GetValue(IsCheckedProperty);
			set => this.SetValue(IsCheckedProperty, value);
		}

		public static readonly DependencyProperty IsCheckedProperty =
			DependencyProperty.Register(
				"IsChecked",
				typeof(bool?),
				typeof(ToggleButton),
				new PropertyMetadata(
					false,
					propertyChangedCallback: (s, e) => ((ToggleButton)s).OnIsCheckedChanged(e.OldValue as bool?, e.NewValue as bool?)
				)
			);

		protected virtual void OnIsCheckedChanged(bool? oldValue, bool? newValue)
		{
			if (IsChecked == null)
			{
				// Indeterminate
				Indeterminate?.Invoke(this, new RoutedEventArgs(this));
			}
			else if (IsChecked.Value)
			{
				// Checked
				Checked?.Invoke(this, new RoutedEventArgs(this));
			}
			else
			{
				// Unchecked
				Unchecked?.Invoke(this, new RoutedEventArgs(this));
			}
		}

		public void OnTemplateRecycled()
		{
			IsChecked = false;
		}

		protected virtual void OnToggle()
		{
			if (CanRevertState)
			{
				if (IsChecked == null)
				{
					// Indeterminate
					// Set to Unchecked
					IsChecked = false;
				}
				else if (IsChecked is bool isChecked && isChecked)
				{
					// Checked
					if (IsThreeState)
					{
						// Set to Indeterminate
						IsChecked = null;
					}
					else
					{
						// Set to Unchecked
						IsChecked = false;
					}
				}
				else
				{
					// Unchecked
					// Set to Checked
					IsChecked = true;
				}
			}
			else
			{
				IsChecked = true;
			}
		}

		internal void AutomationPeerToggle()
		{
			OnToggle();
		}

		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new ToggleButtonAutomationPeer(this);
		}
	}
}
