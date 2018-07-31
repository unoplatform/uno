using System;
using System.Drawing;
using Uno.Disposables;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Controls.Primitives
{
	public partial class ToggleButton : ButtonBase, IFrameworkTemplatePoolAware
	{
		//Renamed to have the same naming as Windows.
		//https://msdn.microsoft.com/en-us/library/system.windows.controls.primitives.togglebutton.checked(v=vs.110).aspx
		public event RoutedEventHandler Checked;
		public event RoutedEventHandler Unchecked;

		public ToggleButton()
		{
			InitializeVisualStates();

			Click += (s, e) =>
			{
				if (CanRevertState)
				{
					IsChecked = IsChecked.HasValue ? !IsChecked.Value : true;
				}
				else
				{
					IsChecked = true;
				}
			};
		}

		/// <summary>
		/// Determines if the current toggle button can revert its state when tapped.
		/// </summary>
		internal bool CanRevertState { get; set; } = true;

#region IsChecked DependencyProperty

		public bool? IsChecked
		{
			get { return (bool?)this.GetValue(IsCheckedProperty); }
			set { this.SetValue(IsCheckedProperty, value); }
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
#endregion

		protected virtual void OnIsCheckedChanged(bool? oldValue, bool? newValue)
		{
			if (newValue.HasValue && newValue.Value)
			{
				Checked?.Invoke(this, new RoutedEventArgs());
			}
			else
			{
				Unchecked?.Invoke(this, new RoutedEventArgs());
			}
		}

		public void OnTemplateRecycled()
		{
			IsChecked = false;
		}
	}
}