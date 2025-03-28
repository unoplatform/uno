#if !HAS_UNO_WINUI

namespace Windows.UI.Xaml.Controls
{
	public partial class Control
	{
		/// <summary>
		/// Attempts to set the focus on the control.
		/// </summary>
		/// <param name="value">Specifies how focus was set, as a value of the enumeration.</param>
		/// <returns>True if focus was set to the control, or focus was already on the control. False if the control is not focusable.</returns>
		public new bool Focus(FocusState value) => FocusImpl(value);

		public new FocusState FocusState
		{
			get => base.FocusState;
			set => base.FocusState = value;
		}

		public static new DependencyProperty FocusStateProperty => UIElement.FocusStateProperty;

		public new bool IsTabStop
		{
			get => base.IsTabStop;
			set => base.IsTabStop = value;
		}

		public static new DependencyProperty IsTabStopProperty => UIElement.IsTabStopProperty;

		public new int TabIndex
		{
			get => base.TabIndex;
			set => base.TabIndex = value;
		}

		public static new DependencyProperty TabIndexProperty => UIElement.TabIndexProperty;

		public new DependencyObject XYFocusUp
		{
			get => base.XYFocusUp;
			set => base.XYFocusUp = value;
		}

		public static new DependencyProperty XYFocusUpProperty => UIElement.XYFocusUpProperty;

		public new DependencyObject XYFocusDown
		{
			get => base.XYFocusDown;
			set => base.XYFocusDown = value;
		}

		public static new DependencyProperty XYFocusDownProperty => UIElement.XYFocusDownProperty;

		public new DependencyObject XYFocusLeft
		{
			get => base.XYFocusLeft;
			set => base.XYFocusLeft = value;
		}

		public static new DependencyProperty XYFocusLeftProperty => UIElement.XYFocusLeftProperty;

		public new DependencyObject XYFocusRight
		{
			get => base.XYFocusRight;
			set => base.XYFocusRight = value;
		}

		public static new DependencyProperty XYFocusRightProperty => UIElement.XYFocusRightProperty;

		public new bool UseSystemFocusVisuals
		{
			get => base.UseSystemFocusVisuals;
			set => base.UseSystemFocusVisuals = value;
		}

		public static new DependencyProperty UseSystemFocusVisualsProperty => UIElement.UseSystemFocusVisualsProperty;
	}
}
#endif
