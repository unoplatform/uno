using System;
using Uno.UI;
using Uno.UI.DataBinding;
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
		// MUX Reference: FlyoutPresenter.h m_wrFlyout — weak back-reference to the
		// owning FlyoutBase so the presenter and its automation peer can resolve the
		// flyout's AutomationProperties.Name and Name properties (matches
		// FlyoutPresenter_partial.cpp GetPlainText / GetOwnerName).
		private ManagedWeakReference _wrFlyout;

		public FlyoutPresenter()
		{
			DefaultStyleKey = typeof(FlyoutPresenter);
		}

		// MUX Reference: FlyoutPresenter_partial.cpp put_Flyout (lines 26-32).
		internal void SetOwnerFlyout(FlyoutBase flyout)
		{
			WeakReferencePool.ReturnWeakReference(this, _wrFlyout);
			_wrFlyout = flyout is not null ? WeakReferencePool.RentWeakReference(this, flyout) : null;
		}

		internal FlyoutBase GetOwnerFlyout()
			=> _wrFlyout?.IsAlive == true ? _wrFlyout.Target as FlyoutBase : null;

		protected override AutomationPeer OnCreateAutomationPeer()
			=> new FlyoutPresenterAutomationPeer(this);

		// MUX Reference: FlyoutPresenter_partial.cpp GetPlainText (lines 103-135).
		// When the owning Flyout has AutomationProperties.Name set, use it as the
		// presenter's plain text so FrameworkElementAutomationPeer.GetNameCore picks
		// it up (matches WinUI behavior).
		internal override string GetPlainText()
		{
			if (GetOwnerFlyout() is { } ownerFlyout
				&& AutomationProperties.GetName(ownerFlyout) is { Length: > 0 } automationName)
			{
				return automationName;
			}

			return base.GetPlainText();
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			var spInnerScrollViewer = GetTemplateChild<ScrollViewer>("ScrollViewer");
			if (spInnerScrollViewer is { })
			{
				spInnerScrollViewer.m_isFocusableOnFlyoutScrollViewer = true;
			}
		}


		internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
		{
			if (args.Property == AllowFocusOnInteractionProperty)
			{
				Content?.SetValue(AllowFocusOnInteractionProperty, AllowFocusOnInteraction);
			}
			else if (args.Property == AllowFocusWhenDisabledProperty)
			{
				Content?.SetValue(AllowFocusWhenDisabledProperty, AllowFocusWhenDisabled);
			}

			base.OnPropertyChanged2(args);
		}

		protected override void OnContentChanged(object oldValue, object newValue)
		{
			base.OnContentChanged(oldValue, newValue);

			Content?.SetValue(AllowFocusOnInteractionProperty, AllowFocusOnInteraction);
			Content?.SetValue(AllowFocusWhenDisabledProperty, AllowFocusWhenDisabled);
		}

		protected override bool CanCreateTemplateWithoutParent { get; } = true;
	}
}
