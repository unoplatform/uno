using System;
using Uno.UI;
using Uno.UI.DataBinding;
using Uno.UI.Helpers.Boxes;
using Windows.Foundation;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class FlyoutPresenter : ContentControl
	{
		private ManagedWeakReference _wrFlyout;

		/// <summary>
		/// Gets or sets a value that indicates whether the default shadow effect is shown.
		/// </summary>
		public bool IsDefaultShadowEnabled
		{
			get => (bool)GetValue(IsDefaultShadowEnabledProperty);
			set => SetValue(IsDefaultShadowEnabledProperty, Boxer.Box(value));
		}

		/// <summary>
		/// Identifies the IsDefaultShadowEnabled dependency property.
		/// </summary>
		public static DependencyProperty IsDefaultShadowEnabledProperty { get; } =
			DependencyProperty.Register(
				nameof(IsDefaultShadowEnabled),
				typeof(bool),
				typeof(FlyoutPresenter),
				new FrameworkPropertyMetadata(BoolBoxes.True));

		public FlyoutPresenter()
		{
			DefaultStyleKey = typeof(FlyoutPresenter);
		}

		internal void SetOwnerFlyout(FlyoutBase flyout)
		{
			WeakReferencePool.ReturnWeakReference(this, _wrFlyout);
			_wrFlyout = flyout is not null ? WeakReferencePool.RentWeakReference(this, flyout) : null;
		}

		internal FlyoutBase GetOwnerFlyout()
			=> _wrFlyout?.IsAlive == true ? _wrFlyout.Target as FlyoutBase : null;

		protected override AutomationPeer OnCreateAutomationPeer()
			=> new FlyoutPresenterAutomationPeer(this);

		// MUX Reference FlyoutPresenter_partial.cpp, tag winui3/release/2.5.1, commit ba3a8d59e
		internal override string GetPlainText()
		{
			// If an automation name is set on the owner flyout, we'll use that as our plain text.
			// Otherwise, we'll report the default plain text.
			if (GetOwnerFlyout() is { } ownerFlyout
				&& AutomationProperties.GetName(ownerFlyout) is { Length: > 0 } automationName)
			{
				return automationName;
			}

			// If we have no title, we'll fall back to the default implementation,
			// which retrieves our content as plain text (e.g., if our content is a string,
			// it returns that; if our content is a TextBlock, it returns its Text value, etc.)
			// If we get the plain text from the content, then we want to truncate it,
			// in case the resulting automation name is very long.
			return Popup.TruncateAutomationName(base.GetPlainText());
		}

		protected override void OnApplyTemplate()
		{
			CleanupTemplateParts();

			base.OnApplyTemplate();

			var spInnerScrollViewer = GetTemplateChild<ScrollViewer>("ScrollViewer");
			//
			// Block calling SetPtrValue() temporary by failing DRT ImmersiveColorMediumCppFlyout.
			//
			//IFC(SetPtrValue(m_tpInnerScrollViewer, spInnerScrollViewer));
			//if (m_tpInnerScrollViewer)
			//{
			//    m_tpInnerScrollViewer.Cast<ScrollViewer>()->m_isFocusableOnFlyoutScrollViewer = TRUE;
			//}
			//
			// The below code must be removed when SetPtrValue is called.
			//
			if (spInnerScrollViewer is { })
			{
				spInnerScrollViewer.m_isFocusableOnFlyoutScrollViewer = true;
			}

			// Apply a shadow
			// Deliverable 19819460: Allow LTEs to target Popup.Child when Popup is windowed
			// ThemeTransition can't be applied to child of a windowed popup, we are targeting grandchild in FlyoutBase_partial.
			// Shadows need to be on the same element to work right, so we are applying shadows to grandchild too.
			if (IsDefaultShadowEnabled && VisualTreeHelper.GetChildrenCount(this) > 0 && VisualTreeHelper.GetChild(this, 0) is UIElement spChildAsUIE)
			{
				DirectUI.ElevationHelper.ApplyElevationEffect(spChildAsUIE);
			}
		}

		private void CleanupTemplateParts()
		{
			// m_tpInnerScrollViewer is never set at this tag, see OnApplyTemplate.
		}

		// TODO Uno: WinUI returns the owning flyout's DependencyObject_Name, which isn't stored on a DependencyObject in Uno.
		internal string GetOwnerName() => string.Empty;

		internal DependencyObject GetTargetIfOpenedAsTransient()
		{
			var ownerFlyout = GetOwnerFlyout();

			if (ownerFlyout is null)
			{
				return null;
			}

			var showMode = ownerFlyout.ShowMode;

			if (showMode == FlyoutShowMode.Auto ||
				showMode == FlyoutShowMode.Standard)
			{
				return null;
			}

			return ownerFlyout.Target;
		}


		internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
		{
			if (args.Property == AllowFocusOnInteractionProperty)
			{
				Content?.SetValue(AllowFocusOnInteractionProperty, Boxer.Box(AllowFocusOnInteraction));
			}
			else if (args.Property == AllowFocusWhenDisabledProperty)
			{
				Content?.SetValue(AllowFocusWhenDisabledProperty, Boxer.Box(AllowFocusWhenDisabled));
			}

			base.OnPropertyChanged2(args);
		}

		protected override void OnContentChanged(object oldValue, object newValue)
		{
			base.OnContentChanged(oldValue, newValue);

			Content?.SetValue(AllowFocusOnInteractionProperty, Boxer.Box(AllowFocusOnInteraction));
			Content?.SetValue(AllowFocusWhenDisabledProperty, Boxer.Box(AllowFocusWhenDisabled));
		}

		protected override bool CanCreateTemplateWithoutParent { get; } = true;
	}
}
