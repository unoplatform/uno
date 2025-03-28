using Uno.UI.Xaml;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Rendering;
using Windows.UI.Xaml.Controls;

#nullable enable

namespace Windows.UI.Xaml
{
	/// <summary>
	/// This contains focus-related mixins that belong either in UIElement or in Control depending whether
	/// targeting UWP or WinUI. When WinUI becomes the "standard", we can move this in UIElement directly.
	/// </summary>
	public partial class UIElement
	{
		public FocusState FocusState
		{
			get => GetFocusStateValue();
			set => SetFocusStateValue(value);
		}

		[GeneratedDependencyProperty(DefaultValue = default(FocusState))]
		public static DependencyProperty FocusStateProperty { get; } = CreateFocusStateProperty();

		public bool IsTabStop
		{
			get => (bool)GetValue(IsTabStopProperty);
			set => SetValue(IsTabStopProperty, value);
		}

		public static DependencyProperty IsTabStopProperty { get; } =
			DependencyProperty.Register(
				nameof(IsTabStop),
				typeof(bool),
				typeof(UIElement),
				new FrameworkPropertyMetadata(
					(bool)false, // This is true for Control descendants (handled by overriding the default in the Control constructor
					(s, e) => ((UIElement)s)?.OnIsTabStopChanged((bool)e.OldValue, (bool)e.NewValue)
				)
			);

		private protected virtual void OnIsTabStopChanged(bool oldValue, bool newValue) { }

		public int TabIndex
		{
			get => GetTabIndexValue();
			set => SetTabIndexValue(value);
		}

		[GeneratedDependencyProperty(DefaultValue = int.MaxValue)]
		public static DependencyProperty TabIndexProperty { get; } = CreateTabIndexProperty();

		public DependencyObject XYFocusUp
		{
			get => GetXYFocusUpValue();
			set => SetXYFocusUpValue(value);
		}

		[GeneratedDependencyProperty(DefaultValue = default(DependencyObject))]
		public static DependencyProperty XYFocusUpProperty { get; } = CreateXYFocusUpProperty();

		public DependencyObject XYFocusDown
		{
			get => GetXYFocusDownValue();
			set => SetXYFocusDownValue(value);
		}

		[GeneratedDependencyProperty(DefaultValue = default(DependencyObject))]
		public static DependencyProperty XYFocusDownProperty { get; } = CreateXYFocusDownProperty();

		public DependencyObject XYFocusLeft
		{
			get => GetXYFocusLeftValue();
			set => SetXYFocusLeftValue(value);
		}

		[GeneratedDependencyProperty(DefaultValue = default(DependencyObject))]
		public static DependencyProperty XYFocusLeftProperty { get; } = CreateXYFocusLeftProperty();

		public DependencyObject XYFocusRight
		{
			get => GetXYFocusRightValue();
			set => SetXYFocusRightValue(value);
		}

		[GeneratedDependencyProperty(DefaultValue = default(DependencyObject))]
		public static DependencyProperty XYFocusRightProperty { get; } = CreateXYFocusRightProperty();

		public bool UseSystemFocusVisuals
		{
			get => GetUseSystemFocusVisualsValue();
			set => SetUseSystemFocusVisualsValue(value);
		}

		[GeneratedDependencyProperty(DefaultValue = false)]
		public static DependencyProperty UseSystemFocusVisualsProperty { get; } = CreateUseSystemFocusVisualsProperty();

		internal virtual void UpdateFocusState(FocusState focusState)
		{
			if (focusState != FocusState)
			{
				SetValue(FocusStateProperty, focusState);

				//If the keyboard is used to navigate to the element, then the focus rectangle should be displayed.
				//Conversely, the user shouldn't need to use the keyboard to remove the focus, so any act that would remove focus is acceptable.
				if (focusState == FocusState.Keyboard || focusState == FocusState.Unfocused)
				{
					//Check if the SystemFocusVisuals are enabled
					if (UseSystemFocusVisuals)
					{
						var thisElement = this;
						UIElement? focusTargetDescendant = null;

						if (thisElement is Control control)
						{
							focusTargetDescendant = control.FocusTargetDescendant as UIElement;
						}

						if (focusTargetDescendant != null)
						{
							var focusManager = VisualTree.GetFocusManagerForElement(this);
							NWSetContentDirty(focusTargetDescendant, DirtyFlags.Render);
							if (focusState == FocusState.Unfocused)
							{
								focusManager?.SetFocusRectangleUIElement(null);
							}

							else
							{
								focusManager?.SetFocusRectangleUIElement(focusTargetDescendant);
							}
						}
						else
						{
							NWSetContentDirty(this, DirtyFlags.Render);
						}
					}
				}
			}
		}

		internal virtual bool IsFocusable =>
					/*IsActive() &&*/ //TODO Uno: No concept of IsActive in Uno yet.
					IsVisible() &&
					(IsEnabled() || ((this as FrameworkElement)?.AllowFocusWhenDisabled == true)) &&
					(IsTabStop || IsFocusableForFocusEngagement()) &&
					AreAllAncestorsVisible();

		internal virtual bool IsFocusableForFocusEngagement() => false;

		private protected bool IsVisible() => Visibility == Visibility.Visible;

		private bool IsEnabled()
		{
			if (this is Control control)
			{
				return control.IsEnabled;
			}

			return true;
		}

		internal bool IsEnabledInternal() => IsEnabled();

		internal
#if __ANDROID__
			new
#endif
			bool IsFocused => FocusState != FocusState.Unfocused;

		internal bool IsKeyboardFocused => FocusState == FocusState.Keyboard;
	}
}
