// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Windows.Foundation.Collections;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Markup;

namespace Microsoft.UI.Xaml.Controls;

[ContentProperty(Name = nameof(PrimaryCommands))]
public partial class CommandBarFlyout : FlyoutBase
{
	// Copyright (c) Microsoft Corporation. All rights reserved.
	// Licensed under the MIT License. See LICENSE in the project root for license information.

# include "pch.h"
# include "common.h"
# include "CommandBarFlyout.h"
# include "CommandBarFlyoutCommandBar.h"
# include "Vector.h"
# include "RuntimeProfiler.h"

# include "CommandBarFlyout.properties.cpp"

	// Change to 'true' to turn on debugging outputs in Output window
	bool CommandBarFlyoutTrace.s_IsDebugOutputEnabled { false };
	bool CommandBarFlyoutTrace.s_IsVerboseDebugOutputEnabled { false };

	// List of AppBarButton/AppBarToggleButton dependency properties being listened to for raising the CommandBarFlyoutCommandBar.OnCommandBarElementDependencyPropertyChanged notifications.
	// IsCompact and LabelPosition have no effect on an AppBarButton's rendering, when used as a secondary command, they are not present in the list.
	// These two arrays are initialized in the ructor instead of being statically initialized here because that would result in the initialization happening during
	// dllmain and it is not valid to call COM apis at that time.
	DependencyProperty CommandBarFlyout.s_appBarButtonDependencyProperties[s_commandBarElementDependencyPropertiesCount]{ null, null, null };
	DependencyProperty CommandBarFlyout.s_appBarToggleButtonDependencyProperties[s_commandBarElementDependencyPropertiesCount]{ null, null, null };

	public CommandBarFlyout()
	{
		//__RP_Marker_ClassById(RuntimeProfiler.ProfId_CommandBarFlyout);

		// Initialize s_appBarButtonDependencyProperties and s_appBarToggleButtonDependencyProperties if needed.
		if (s_appBarButtonDependencyProperties[0] == null)
		{
			s_appBarButtonDependencyProperties[0] = AppBarButton.IconProperty();
			s_appBarButtonDependencyProperties[1] = AppBarButton.LabelProperty();

			s_appBarToggleButtonDependencyProperties[0] = AppBarToggleButton.IconProperty();
			s_appBarToggleButtonDependencyProperties[1] = AppBarToggleButton.LabelProperty();

			if (SharedHelpers.IsRS4OrHigher())
			{
				s_appBarButtonDependencyProperties[s_commandBarElementDependencyPropertiesCount - 1] = AppBarButton.KeyboardAcceleratorTextOverrideProperty();
				s_appBarToggleButtonDependencyProperties[s_commandBarElementDependencyPropertiesCount - 1] = AppBarToggleButton.KeyboardAcceleratorTextOverrideProperty();
			}
		}

		if (IFlyoutBase6 thisAsFlyoutBase6 = this)
    {
			thisAsFlyoutBase6.ShouldConstrainToRootBounds(false);
		}

		if (IFlyoutBase5 thisAsFlyoutBase5 = this)
    {
			thisAsFlyoutBase5.AreOpenCloseAnimationsEnabled(false);
		}

		m_primaryCommands = new Vector<ICommandBarElement>> ().as< IObservableVector<ICommandBarElement>();
		m_secondaryCommands = new Vector<ICommandBarElement>> ().as< IObservableVector<ICommandBarElement>();

		m_primaryCommandsVectorChangedToken = m_primaryCommands.VectorChanged({
			[this](IObservableVector<ICommandBarElement> & sender, IVectorChangedEventArgs & args)
	
		{
				if (var commandBar = m_commandBar)
            {
					SharedHelpers.ForwardVectorChange(sender, commandBar.PrimaryCommands(), args);
				}
			}
		});

		m_secondaryCommandsVectorChangedToken = m_secondaryCommands.VectorChanged({
			[this](IObservableVector<ICommandBarElement> & sender, IVectorChangedEventArgs & args)
	
		{
				if (var commandBar = m_commandBar)
            {
					SharedHelpers.ForwardVectorChange(sender, commandBar.SecondaryCommands(), args);

					// We want to ensure that any interaction with secondary items causes the CommandBarFlyout
					// to close, so we'll attach a Click handler to any buttons and Checked/Unchecked handlers
					// to any toggle buttons that we get and close the flyout when they're invoked.
					// The only exception is buttons with flyouts - in that case, clicking on the button
					// will just open the flyout rather than executing an action, so we don't want that to
					// do anything.
					int index = args.Index;
					var closeFlyoutFunc = [this](var sender, var args) { Hide(); };

					switch (args.CollectionChange())
					{
						case CollectionChange.ItemChanged:
							{
								var element = sender[index];
								var button = element as AppBarButton;
								var toggleButton = element as AppBarToggleButton;

								UnhookCommandBarElementDependencyPropertyChanges(index);

								if (button)
								{
									HookAppBarButtonDependencyPropertyChanges(button, index);
								}
								else if (toggleButton)
								{
									HookAppBarToggleButtonDependencyPropertyChanges(toggleButton, index);
								}

								if (button && !button.Flyout())
								{
									m_secondaryButtonClickRevokerByIndexMap[index] = button.Click(auto_revoke, closeFlyoutFunc);
									SharedHelpers.EraseIfExists(m_secondaryToggleButtonCheckedRevokerByIndexMap, index);
									SharedHelpers.EraseIfExists(m_secondaryToggleButtonUncheckedRevokerByIndexMap, index);
								}
								else if (toggleButton)
								{
									SharedHelpers.EraseIfExists(m_secondaryButtonClickRevokerByIndexMap, index);
									m_secondaryToggleButtonCheckedRevokerByIndexMap[index] = toggleButton.Checked(auto_revoke, closeFlyoutFunc);
									m_secondaryToggleButtonUncheckedRevokerByIndexMap[index] = toggleButton.Unchecked(auto_revoke, closeFlyoutFunc);
								}
								else
								{
									SharedHelpers.EraseIfExists(m_secondaryButtonClickRevokerByIndexMap, index);
									SharedHelpers.EraseIfExists(m_secondaryToggleButtonCheckedRevokerByIndexMap, index);
									SharedHelpers.EraseIfExists(m_secondaryToggleButtonUncheckedRevokerByIndexMap, index);
								}
								break;
							}
						case CollectionChange.ItemInserted:
							{
								var element = sender[index];
								var button = element as AppBarButton;
								var toggleButton = element as AppBarToggleButton;

								if (button)
								{
									HookAppBarButtonDependencyPropertyChanges(button, index);
								}
								else if (toggleButton)
								{
									HookAppBarToggleButtonDependencyPropertyChanges(toggleButton, index);
								}

								if (button && !button.Flyout())
								{
									m_secondaryButtonClickRevokerByIndexMap[index] = button.Click(auto_revoke, closeFlyoutFunc);
								}
								else if (toggleButton)
								{
									m_secondaryToggleButtonCheckedRevokerByIndexMap[index] = toggleButton.Checked(auto_revoke, closeFlyoutFunc);
									m_secondaryToggleButtonUncheckedRevokerByIndexMap[index] = toggleButton.Unchecked(auto_revoke, closeFlyoutFunc);
								}
								break;
							}
						case CollectionChange.ItemRemoved:
							UnhookCommandBarElementDependencyPropertyChanges(index);

							SharedHelpers.EraseIfExists(m_secondaryButtonClickRevokerByIndexMap, index);
							SharedHelpers.EraseIfExists(m_secondaryToggleButtonCheckedRevokerByIndexMap, index);
							SharedHelpers.EraseIfExists(m_secondaryToggleButtonUncheckedRevokerByIndexMap, index);
							break;
						case CollectionChange.Reset:
							SetSecondaryCommandsToCloseWhenExecuted();
							HookAllCommandBarElementDependencyPropertyChanges();
							break;
						default:
							MUX_MUX_ASSERT(false);
					}
				}
			}
		});

		Opening({
			[this](var &, var &)
	
		{
				// The CommandBarFlyout is shown in standard mode in the case
				// where it's being opened as a context menu, rather than as a selection flyout.
				// In that circumstance, we want to have the flyout be open from the start.
				IFlyoutBase5 thisAsFlyoutBase5 = this;

				if (var commandBar = m_commandBar)
            {
					// If we are in AlwaysExpanded mode then we want to make sure we open in Standard ShowMode
					// Otherwise the timing on the creation of the two drops shows is such that the primary items
					// draw their shadow on top of the secondary items.
					// When CommandBarFlyout is in AlwaysOpen state, don't show the overflow button
					if (AlwaysExpanded())
					{
						commandBar.OverflowButtonVisibility(Windows.UI.Xaml.Controls.CommandBarOverflowButtonVisibility.Collapsed);
						if (thisAsFlyoutBase5)
						{
							thisAsFlyoutBase5.ShowMode(FlyoutShowMode.Standard);
						}
					}
					else
					{
						commandBar.OverflowButtonVisibility(Windows.UI.Xaml.Controls.CommandBarOverflowButtonVisibility.Auto);
					}
					SharedHelpers.QueueCallbackForCompositionRendering(
	
						[strongThis = get_strong(), thisAsFlyoutBase5, commandBar]

					{
						if (var commandBarFlyoutCommandBar = get_self<CommandBarFlyoutCommandBar>(commandBar))
                        {
							var scopeGuard = gsl.finally([commandBarFlyoutCommandBar]()

								{
								commandBarFlyoutCommandBar.m_commandBarFlyoutIsOpening = false;
							});
							commandBarFlyoutCommandBar.m_commandBarFlyoutIsOpening = true;

							// If we don't have IFlyoutBase5 available, then we assume a standard show mode.
							if (!thisAsFlyoutBase5 || thisAsFlyoutBase5.ShowMode() == FlyoutShowMode.Standard)
							{
								commandBar.IsOpen(true);
							}
							}
						}
                );
					}

					if (m_primaryCommands.Size() > 0)
					{
						AddDropShadow();
					}
				}
			});

			Opened({
				[this](var &, var &)
		
		{
					if (var commandBar = get_self<CommandBarFlyoutCommandBar>(m_commandBar))
            {
						if (commandBar.HasOpenAnimation())
						{
							commandBar.PlayOpenAnimation();
						}
					}

					if (var commandBarPeer = FrameworkElementAutomationPeer.FromElement(m_commandBar))
            {
						commandBarPeer.RaiseAutomationEvent(AutomationEvents.MenuOpened);
					}
				}
			});

			Closing({
				[this](var &, FlyoutBaseClosingEventArgs & args)
		
		{
					if (var commandBar = get_self<CommandBarFlyoutCommandBar>(m_commandBar))
            {
						// We are not able to fade this shadow out with the V2 default opacity closing animition.
						// Additionally we drop shadows doing play well with the clip animation of the V1 style.
						// So we need to remove it in all cases.
						RemoveDropShadow();

						if (!m_isClosingAfterCloseAnimation && commandBar.HasCloseAnimation())
						{
							args.Cancel(true);

							CommandBarFlyout commandBarFlyout = this;

							commandBar.PlayCloseAnimation(
								make_weak(commandBarFlyout),
		
								[this]()
		
						{
								m_isClosingAfterCloseAnimation = true;
								Hide();
								m_isClosingAfterCloseAnimation = false;
							});
						}
						else
						{
							// If we don't have an animation, close the command bar and thus it's subflyouts.
							commandBar.IsOpen(false);
						}

						//Drop shadows do not play nicely with clip animations, if we are using both, clear the shadow
						if (SharedHelpers.Is21H1OrHigher() && commandBar.OpenAnimationKind() == CommandBarFlyoutOpenCloseAnimationKind.Clip)
						{
							commandBar.ClearShadow();
						}
					}
				}
			});

			// Close the CommandBar in order to ensure that we're always starting from a known state when opening the flyout.
			Closed({
				[this](var &, var &)
		
		{
					if (var commandBar = m_commandBar)
            {
						if (commandBar.IsOpen())
						{
							commandBar.IsOpen(false);
						}

						if (var commandBarPeer = FrameworkElementAutomationPeer.FromElement(commandBar))
                {
							commandBarPeer.RaiseAutomationEvent(AutomationEvents.MenuClosed);
						}
					}
				}
			});
		}

		CommandBarFlyout.~public CommandBarFlyout()
		{
			m_primaryCommands.VectorChanged(m_primaryCommandsVectorChangedToken);
			m_secondaryCommands.VectorChanged(m_secondaryCommandsVectorChangedToken);

			UnhookAllCommandBarElementDependencyPropertyChanges();
		}

		IObservableVector<ICommandBarElement> PrimaryCommands()
		{
			return m_primaryCommands;
		}

		IObservableVector<ICommandBarElement> SecondaryCommands()
		{
			return m_secondaryCommands;
		}

		Control CreatePresenter()
		{
			var commandBar = new CommandBarFlyoutCommandBar();

			SharedHelpers.CopyVector(m_primaryCommands, commandBar.PrimaryCommands());
			SharedHelpers.CopyVector(m_secondaryCommands, commandBar.SecondaryCommands());

			SetSecondaryCommandsToCloseWhenExecuted();
			HookAllCommandBarElementDependencyPropertyChanges();

			FlyoutPresenter presenter;
			presenter.Background(null);
			presenter.Foreground(null);
			presenter.BorderBrush(null);
			presenter.MinWidth(0.0;
			presenter.MaxWidth = std.numeric_limits<double>.infinity();
			presenter.MinHeight(0.0;
			presenter.MaxHeight = std.numeric_limits<double>.infinity();
			presenter.BorderThickness(ThicknessHelper.FromUniformLength(0));
			presenter.Padding(ThicknessHelper.FromUniformLength(0));
			presenter.Content = *commandBar;
			if (SharedHelpers.IsRS5OrHigher())
			{
				presenter.Translation({ 0.0f, 0.0f, 32.0f });
	}

    // Disable the default shadow, as we'll be providing our own shadow.
    if (IFlyoutPresenter2 presenter2 = presenter)
    {
        presenter2.IsDefaultShadowEnabled(false);
    }

m_presenter = presenter;

    m_commandBarOpenedRevoker = commandBar.Opened(auto_revoke, {
        [this] (var &, var &)
        {
            if (IFlyoutBase5 thisAsFlyoutBase5 = this)

			{
	// If we open the CommandBar, then we should no longer be in a transient show mode -
	// we now know that the user wants to interact with us.
	thisAsFlyoutBase5.ShowMode(FlyoutShowMode.Standard);
}
}
    });

if (SharedHelpers.Is21H1OrHigher())
{
	commandBar.SetPresenter(presenter);

	// We'll need to remove the presenter's drop shadow on the commandBar's Opening/Closing
	// because we need it to disappear during its expand/shrink animation when the Overflow is opened.
	// It will be re-added once the storyboard for the overflow animations are completed.
	// That code can be found inside CommandBarFlyoutCommandBar.
	m_commandBarOpeningRevoker = commandBar.Opening(auto_revoke, {
		[this, presenter](var &, var &)

			{
			if (var commandBar = get_self<CommandBarFlyoutCommandBar>(m_commandBar))
                {
				if (commandBar.HasSecondaryOpenCloseAnimations())
				{
					// We'll only need to do the mid-animation remove/add when the "..." button is
					// pressed to open/close the overflow. This means we shouldn't do it for AlwaysExpanded
					// and if there's nothing in the overflow.
					if (m_secondaryCommands.Size() > 0)
					{
						RemoveDropShadow();
					}
				}
			}
		}
	});

	m_commandBarClosingRevoker = commandBar.Closing(auto_revoke, {
		[this](var &, var &)

			{
			if (var commandBar = get_self<CommandBarFlyoutCommandBar>(m_commandBar))
                {
				if (commandBar.HasSecondaryOpenCloseAnimations())
				{
					RemoveDropShadow();
				}
			}
		}
	});
}

commandBar.SetOwningFlyout(this);

m_commandBar = *commandBar;
return presenter;
}

void SetSecondaryCommandsToCloseWhenExecuted()
{
	m_secondaryButtonClickRevokerByIndexMap.clear();
	m_secondaryToggleButtonCheckedRevokerByIndexMap.clear();
	m_secondaryToggleButtonUncheckedRevokerByIndexMap.clear();

	var closeFlyoutFunc = [this](var sender, var args) { Hide(); };

	for (uint i = 0; i < SecondaryCommands().Size(); i++)
	{
		var element = SecondaryCommands()[i];
		var button = element as AppBarButton;
		var toggleButton = element as AppBarToggleButton;

		if (button && !button.Flyout())
		{
			m_secondaryButtonClickRevokerByIndexMap[i] = button.Click(auto_revoke, closeFlyoutFunc);
		}
		else if (toggleButton)
		{
			m_secondaryToggleButtonCheckedRevokerByIndexMap[i] = toggleButton.Checked(auto_revoke, closeFlyoutFunc);
			m_secondaryToggleButtonUncheckedRevokerByIndexMap[i] = toggleButton.Unchecked(auto_revoke, closeFlyoutFunc);
		}
	}
}

void AddDropShadow()
{
	if (SharedHelpers.Is21H1OrHigher())
	{
		if (m_presenter is { } presenter)
		{
			Windows.UI.Xaml.Media.ThemeShadow shadow;
			presenter.Shadow(shadow);
		}
	}
}

void RemoveDropShadow()
{
	if (SharedHelpers.Is21H1OrHigher())
	{
		if (m_presenter is { } presenter)
		{
			presenter.Shadow(null);
		}
	}
}

tracker_ref<FlyoutPresenter> GetPresenter()
{
	return m_presenter;
}

void HookAppBarButtonDependencyPropertyChanges(AppBarButton & appBarButton, int index)
{
	var commandBarElementDependencyPropertiesCount = SharedHelpers.IsRS4OrHigher() ? s_commandBarElementDependencyPropertiesCount : s_commandBarElementDependencyPropertiesCountRS3;

	for (int commandBarElementDependencyPropertyIndex = 0; commandBarElementDependencyPropertyIndex < commandBarElementDependencyPropertiesCount; commandBarElementDependencyPropertyIndex++)
	{
		m_propertyChangedRevokersByIndexMap[index][commandBarElementDependencyPropertyIndex] =
			RegisterPropertyChanged(
				appBarButton,
				s_appBarButtonDependencyProperties[commandBarElementDependencyPropertyIndex], { this, &CommandBarFlyout.OnCommandBarElementDependencyPropertyChanged });
    }
}

void HookAppBarToggleButtonDependencyPropertyChanges(AppBarToggleButton & appBarToggleButton, int index)
{
	var commandBarElementDependencyPropertiesCount = SharedHelpers.IsRS4OrHigher() ? s_commandBarElementDependencyPropertiesCount : s_commandBarElementDependencyPropertiesCountRS3;

	for (int commandBarElementDependencyPropertyIndex = 0; commandBarElementDependencyPropertyIndex < commandBarElementDependencyPropertiesCount; commandBarElementDependencyPropertyIndex++)
	{
		m_propertyChangedRevokersByIndexMap[index][commandBarElementDependencyPropertyIndex] =
			RegisterPropertyChanged(
				appBarToggleButton,
				s_appBarToggleButtonDependencyProperties[commandBarElementDependencyPropertyIndex], { this, &CommandBarFlyout.OnCommandBarElementDependencyPropertyChanged });
    }
}

void HookAllCommandBarElementDependencyPropertyChanges()
{
	UnhookAllCommandBarElementDependencyPropertyChanges();

	for (uint i = 0; i < SecondaryCommands().Size(); i++)
	{
		var element = SecondaryCommands()[i];
		var button = element as AppBarButton;
		var toggleButton = element as AppBarToggleButton;

		if (button)
		{
			HookAppBarButtonDependencyPropertyChanges(button, i);
		}
		else if (toggleButton)
		{
			HookAppBarToggleButtonDependencyPropertyChanges(toggleButton, i);
		}
	}
}

void UnhookCommandBarElementDependencyPropertyChanges(int index, bool eraseRevokers)
{
	var revokers = m_propertyChangedRevokersByIndexMap.find(index);
	if (revokers != m_propertyChangedRevokersByIndexMap.end())
	{
		var commandBarElementDependencyPropertiesCount = SharedHelpers.IsRS4OrHigher() ? s_commandBarElementDependencyPropertiesCount : s_commandBarElementDependencyPropertiesCountRS3;

		for (int commandBarElementDependencyPropertyIndex = 0; commandBarElementDependencyPropertyIndex < commandBarElementDependencyPropertiesCount; commandBarElementDependencyPropertyIndex++)
		{
			m_propertyChangedRevokersByIndexMap[index][commandBarElementDependencyPropertyIndex].revoke();
		}

		if (eraseRevokers)
		{
			m_propertyChangedRevokersByIndexMap.erase(revokers);
		}
	}
}

void UnhookAllCommandBarElementDependencyPropertyChanges()
{
	foreach (var revokers in m_propertyChangedRevokersByIndexMap)
	{
		UnhookCommandBarElementDependencyPropertyChanges(revokers.first, false /*eraseRevokers*/);
	}
	m_propertyChangedRevokersByIndexMap.clear();
}

// Let the potential CommandBarFlyoutCommandBar know of the dependency property change so it can adjust its size.
void OnCommandBarElementDependencyPropertyChanged(DependencyObject dependencyObject, DependencyProperty & dependencyProperty)
{
	COMMANDBARFLYOUT_TRACE_VERBOSE(this, TRACE_MSG_METH, METH_NAME, this);

	if (var commandBar = get_self<CommandBarFlyoutCommandBar>(m_commandBar))
    {
	commandBar.OnCommandBarElementDependencyPropertyChanged();
}
}

}
