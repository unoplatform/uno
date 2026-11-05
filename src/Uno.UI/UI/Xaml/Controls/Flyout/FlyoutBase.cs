using System;
using System.Diagnostics.CodeAnalysis;

using Uno;
using Uno.UI.Xaml;
using Uno.UI;
using Uno.UI.DataBinding;
using Uno.UI.Helpers.Boxes;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls.Primitives
{
	public partial class FlyoutBase : DependencyObject
	{
		public event EventHandler<object> Opened;
		public event EventHandler<object> Closed;
		public event EventHandler<object> Opening;
		public event TypedEventHandler<FlyoutBase, FlyoutBaseClosingEventArgs> Closing;

		protected internal Popup _popup;

		protected FlyoutBase()
		{
		}

		public bool IsOpen
		{
			get => _popup?.IsOpen ?? false;
			private set => SetValue(IsOpenProperty, value);
		}
		public static DependencyProperty IsOpenProperty { get; } =
			DependencyProperty.Register(
				nameof(IsOpen), typeof(bool),
				typeof(FlyoutBase),
				new FrameworkPropertyMetadata(BoolBoxes.False));

		/// <summary>
		/// Gets a value that indicates whether the flyout is shown within the bounds of the XAML root.
		/// </summary>
		public bool IsConstrainedToRootBounds
		{
			get
			{
				EnsurePopupAndPresenter();
				return _popup.IsConstrainedToRootBounds;
			}
		}

		/// <summary>
		/// Gets or sets a value that indicates whether the flyout should be shown within the bounds of the XAML root.
		/// </summary>
		public bool ShouldConstrainToRootBounds
		{
			get => (bool)GetValue(ShouldConstrainToRootBoundsProperty);
			set => SetValue(ShouldConstrainToRootBoundsProperty, Boxer.Box(value));
		}

		/// <summary>
		/// Identifies the ShouldConstrainToRootBounds dependency property.
		/// </summary>
		public static DependencyProperty ShouldConstrainToRootBoundsProperty { get; } =
			DependencyProperty.Register(
				nameof(ShouldConstrainToRootBounds),
				typeof(bool),
				typeof(FlyoutBase),
				new FrameworkPropertyMetadata(BoolBoxes.True));

		// MUX Reference DependencyProperty.cpp, CDependencyProperty::GetDefaultValue, tag winui3/release/2.5.1, commit ba3a8d59e
		internal override bool GetDefaultValue2(DependencyProperty property, out object defaultValue)
		{
			if (property == ShouldConstrainToRootBoundsProperty)
			{
				// MenuFlyouts aren't constrained by default.
				defaultValue = this is MenuFlyout ? BoolBoxes.False : BoolBoxes.True;
				return true;
			}

			return base.GetDefaultValue2(property, out defaultValue);
		}

		#region Placement

		/// <summary>
		/// Preferred placement of the flyout.
		/// </summary>
		/// <remarks>
		/// If there's not enough place, the following logic will be used:
		/// https://docs.microsoft.com/en-us/previous-versions/windows/apps/dn308515(v=win.10)#placing-a-flyout
		/// </remarks>
		public FlyoutPlacementMode Placement
		{
			get { return (FlyoutPlacementMode)GetValue(PlacementProperty); }
			set { SetValue(PlacementProperty, value); }
		}

		public static DependencyProperty PlacementProperty { get; } =
			DependencyProperty.Register(
				"Placement",
				typeof(FlyoutPlacementMode),
				typeof(FlyoutBase),
				new FrameworkPropertyMetadata(default(FlyoutPlacementMode))
			);

		#endregion

		public LightDismissOverlayMode LightDismissOverlayMode
		{
			get
			{
				return (LightDismissOverlayMode)this.GetValue(LightDismissOverlayModeProperty);
			}
			set
			{
				this.SetValue(LightDismissOverlayModeProperty, value);
			}
		}

		public static DependencyProperty LightDismissOverlayModeProperty { get; } =
		Microsoft.UI.Xaml.DependencyProperty.Register(
			"LightDismissOverlayMode", typeof(LightDismissOverlayMode),
			typeof(FlyoutBase),
			new FrameworkPropertyMetadata(default(LightDismissOverlayMode)));


		public DependencyObject OverlayInputPassThroughElement
		{
			get => (DependencyObject)GetValue(OverlayInputPassThroughElementProperty);
			set => SetValue(OverlayInputPassThroughElementProperty, value);
		}

		public static DependencyProperty OverlayInputPassThroughElementProperty { get; } =
			DependencyProperty.Register(nameof(OverlayInputPassThroughElement), typeof(DependencyObject), typeof(FlyoutBase), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext));

		/// <summary>
		/// Gets or sets whether a disabled control can receive focus.
		/// </summary>
		public bool AllowFocusWhenDisabled
		{
			get => GetAllowFocusWhenDisabledValue();
			set => SetAllowFocusWhenDisabledValue(value);
		}

		/// <summary>
		/// Identifies the AllowFocusWhenDisabled  dependency property.
		/// </summary>
		[GeneratedDependencyProperty(DefaultValue = false, Options = FrameworkPropertyMetadataOptions.Inherits)]
		public static DependencyProperty AllowFocusWhenDisabledProperty { get; } = CreateAllowFocusWhenDisabledProperty();

		/// <summary>
		/// Gets or sets a value that indicates whether the element automatically gets focus when the user interacts with it.
		/// </summary>
		public bool AllowFocusOnInteraction
		{
			get => GetAllowFocusOnInteractionValue();
			set => SetAllowFocusOnInteractionValue(value);
		}

		/// <summary>
		/// Identifies for the AllowFocusOnInteraction dependency property.
		/// </summary>
		[GeneratedDependencyProperty(DefaultValue = true, Options = FrameworkPropertyMetadataOptions.Inherits)]
		public static DependencyProperty AllowFocusOnInteractionProperty { get; } = CreateAllowFocusOnInteractionProperty();

		/// <summary>
		/// Gets or sets a value that indicates how a flyout behaves when shown.
		/// </summary>
		public FlyoutShowMode ShowMode
		{
			get => (FlyoutShowMode)this.GetValue(ShowModeProperty);
			set => this.SetValue(ShowModeProperty, value);
		}

		/// <summary>
		/// Identifies the ShowMode dependency property.
		/// </summary>
		public static DependencyProperty ShowModeProperty { get; } =
			DependencyProperty.Register(
				nameof(ShowMode),
				typeof(FlyoutShowMode),
				typeof(FlyoutBase),
				new FrameworkPropertyMetadata(FlyoutShowMode.Standard));

		/// <summary>
		/// Gets a value that indicates whether the flyout should show commands
		/// as primary (toolbar) based on the input device used to open it.
		/// </summary>
		public bool InputDevicePrefersPrimaryCommands
		{
			get => (bool)GetValue(InputDevicePrefersPrimaryCommandsProperty);
			private set => SetValue(InputDevicePrefersPrimaryCommandsProperty, value);
		}

		/// <summary>
		/// Identifies the InputDevicePrefersPrimaryCommands dependency property.
		/// </summary>
		public static DependencyProperty InputDevicePrefersPrimaryCommandsProperty { get; } =
			DependencyProperty.Register(
				nameof(InputDevicePrefersPrimaryCommands),
				typeof(bool),
				typeof(FlyoutBase),
				new FrameworkPropertyMetadata(BoolBoxes.False));

		// In WinUI, Target is declared as a back-reference (weak reference) in
		// IsDependencyPropertyBackReference(). Using ManagedWeakReference (via WeakReferencePool)
		// prevents shared flyouts from leaking the previous placement target's ViewModel
		// via FlyoutBase → Target → DataContext, and avoids per-show allocations.
		private ManagedWeakReference _targetWeakRef;

		public FrameworkElement Target
		{
			get => _targetWeakRef?.IsAlive == true ? _targetWeakRef.Target as FrameworkElement : null;
			private set
			{
				WeakReferencePool.ReturnWeakReference(this, _targetWeakRef);
				_targetWeakRef = value is not null ? WeakReferencePool.RentWeakReference(this, value) : null;
			}
		}

		/// <summary>
		/// Closes the flyout.
		/// </summary>
		public void Hide() => HideImpl();

		public static FlyoutBase GetAttachedFlyout(FrameworkElement element)
		{
			return (FlyoutBase)element.GetValue(AttachedFlyoutProperty);
		}

		public static void SetAttachedFlyout(FrameworkElement element, FlyoutBase value)
		{
			element.SetValue(AttachedFlyoutProperty, value);
		}

		public static DependencyProperty AttachedFlyoutProperty
		{
			[DynamicDependency(nameof(GetAttachedFlyout))]
			[DynamicDependency(nameof(SetAttachedFlyout))]
			get;
		} = DependencyProperty.RegisterAttached(
				"AttachedFlyout",
				typeof(FlyoutBase),
				typeof(FlyoutBase),
				new FrameworkPropertyMetadata(null));
	}
}
