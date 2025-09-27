// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\CommandBarFlyout\CommandBarFlyout.cpp, tag winui3/release/1.7.3, commit 65718e2813a90fc900e8775d2ddc580b268fcc2f

using System;
using System.Numerics;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;
using Uno.UI.DataBinding;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation.Collections;
using static Microsoft.UI.Xaml.Controls._Tracing;
using CommandBarFlyoutCommandBar = Microsoft.UI.Xaml.Controls.Primitives.CommandBarFlyoutCommandBar;

namespace Microsoft.UI.Xaml.Controls;

partial class CommandBarFlyout
{
	// Change to 'true' to turn on debugging outputs in Output window
	//private const bool s_IsDebugOutputEnabled = false;
	//private const bool s_IsVerboseDebugOutputEnabled = false;

	// List of AppBarButton/AppBarToggleButton dependency properties being listened to for raising the CommandBarFlyoutCommandBar.OnCommandBarElementDependencyPropertyChanged notifications.
	// IsCompact and LabelPosition have no effect on an AppBarButton's rendering, when used as a secondary command, they are not present in the list.
	// These two arrays are initialized in the ructor instead of being statically initialized here because that would result in the initialization happening during
	// dllmain and it is not valid to call COM apis at that time.
	private static readonly DependencyProperty[] s_appBarButtonDependencyProperties = new DependencyProperty[s_commandBarElementDependencyPropertiesCount];
	private static readonly DependencyProperty[] s_appBarToggleButtonDependencyProperties = new DependencyProperty[s_commandBarElementDependencyPropertiesCount];

	public CommandBarFlyout()
	{
		//__RP_Marker_ClassById(RuntimeProfiler.ProfId_CommandBarFlyout);

		// Initialize s_appBarButtonDependencyProperties and s_appBarToggleButtonDependencyProperties if needed.
		if (s_appBarButtonDependencyProperties[0] == null)
		{
			s_appBarButtonDependencyProperties[0] = AppBarButton.IconProperty;
			s_appBarButtonDependencyProperties[1] = AppBarButton.LabelProperty;

			s_appBarToggleButtonDependencyProperties[0] = AppBarToggleButton.IconProperty;
			s_appBarToggleButtonDependencyProperties[1] = AppBarToggleButton.LabelProperty;

			s_appBarButtonDependencyProperties[s_commandBarElementDependencyPropertiesCount - 1] = AppBarButton.KeyboardAcceleratorTextOverrideProperty;
			s_appBarToggleButtonDependencyProperties[s_commandBarElementDependencyPropertiesCount - 1] = AppBarToggleButton.KeyboardAcceleratorTextOverrideProperty;
		}

		ShouldConstrainToRootBounds = false;
		AreOpenCloseAnimationsEnabled = false;

		m_primaryCommands = new ObservableVector<ICommandBarElement>();
		m_secondaryCommands = new ObservableVector<ICommandBarElement>();

		void OnPrimaryCommandsChanged(IObservableVector<ICommandBarElement> sender, IVectorChangedEventArgs args)
		{
			if (m_commandBar is { } commandBar)
			{
				SharedHelpers.ForwardVectorChange(sender, commandBar.PrimaryCommands, args);
			}
		}

		m_primaryCommands.VectorChanged += OnPrimaryCommandsChanged;
		m_primaryCommandsVectorChangedToken.Disposable = Disposable.Create(() => m_primaryCommands.VectorChanged -= OnPrimaryCommandsChanged);

		void OnSecondaryCommandsChanged(IObservableVector<ICommandBarElement> sender, IVectorChangedEventArgs args)
		{
			if (m_commandBar is { } commandBar)
			{
				SharedHelpers.ForwardVectorChange(sender, commandBar.SecondaryCommands, args);

				// We want to ensure that any interaction with secondary items causes the CommandBarFlyout
				// to close, so we'll attach a Click handler to any buttons and Checked/Unchecked handlers
				// to any toggle buttons that we get and close the flyout when they're invoked.
				// The only exception is buttons with flyouts - in that case, clicking on the button
				// will just open the flyout rather than executing an action, so we don't want that to
				// do anything.
				var index = (int)args.Index;
				void closeFlyoutFunc(object sender, object args) => Hide();

				switch (args.CollectionChange)
				{
					case CollectionChange.ItemChanged:
						{
							var element = sender[index];
							var button = element as AppBarButton;
							var toggleButton = element as AppBarToggleButton;

							UnhookCommandBarElementDependencyPropertyChanges(index);

							if (button is not null)
							{
								HookAppBarButtonDependencyPropertyChanges(button, index);
							}
							else if (toggleButton is not null)
							{
								HookAppBarToggleButtonDependencyPropertyChanges(toggleButton, index);
							}

							if (button is not null && button.Flyout is null)
							{
								button.Click += closeFlyoutFunc;
								m_secondaryButtonClickRevokerByIndexMap[index] = Disposable.Create(() => button.Click -= closeFlyoutFunc);

								SharedHelpers.EraseIfExists(m_secondaryToggleButtonCheckedRevokerByIndexMap, index);
								SharedHelpers.EraseIfExists(m_secondaryToggleButtonUncheckedRevokerByIndexMap, index);
							}
							else if (toggleButton is not null)
							{
								SharedHelpers.EraseIfExists(m_secondaryButtonClickRevokerByIndexMap, index);
								toggleButton.Checked += closeFlyoutFunc;
								m_secondaryToggleButtonCheckedRevokerByIndexMap[index] = Disposable.Create(() => toggleButton.Checked -= closeFlyoutFunc);
								toggleButton.Unchecked += closeFlyoutFunc;
								m_secondaryToggleButtonUncheckedRevokerByIndexMap[index] = Disposable.Create(() => toggleButton.Unchecked -= closeFlyoutFunc);
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

							if (button is not null)
							{
								HookAppBarButtonDependencyPropertyChanges(button, index);
							}
							else if (toggleButton is not null)
							{
								HookAppBarToggleButtonDependencyPropertyChanges(toggleButton, index);
							}

							if (button is not null && button.Flyout is null)
							{
								button.Click += closeFlyoutFunc;
								m_secondaryButtonClickRevokerByIndexMap[index] = Disposable.Create(() => button.Click -= closeFlyoutFunc);
							}
							else if (toggleButton is not null)
							{
								toggleButton.Checked += closeFlyoutFunc;
								m_secondaryToggleButtonCheckedRevokerByIndexMap[index] = Disposable.Create(() => toggleButton.Checked -= closeFlyoutFunc);
								toggleButton.Unchecked += closeFlyoutFunc;
								m_secondaryToggleButtonUncheckedRevokerByIndexMap[index] = Disposable.Create(() => toggleButton.Unchecked -= closeFlyoutFunc);
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
						MUX_ASSERT(false);
						break;
				}
			}
		}

		m_secondaryCommands.VectorChanged += OnSecondaryCommandsChanged;
		m_secondaryCommandsVectorChangedToken.Disposable = Disposable.Create(() => m_secondaryCommands.VectorChanged -= OnSecondaryCommandsChanged);

		Opening += (s, e) =>
		{
			// The CommandBarFlyout is shown in standard mode in the case
			// where it's being opened as a context menu, rather than as a selection flyout.
			// In that circumstance, we want to have the flyout be open from the start.
			if (m_commandBar is { } commandBar)
			{
				// If we are in AlwaysExpanded mode then we want to make sure we open in Standard ShowMode
				// Otherwise the timing on the creation of the two drops shows is such that the primary items
				// draw their shadow on top of the secondary items.
				// When CommandBarFlyout is in AlwaysOpen state, don't show the overflow button
				if (AlwaysExpanded)
				{
					commandBar.OverflowButtonVisibility = CommandBarOverflowButtonVisibility.Collapsed;
					ShowMode = FlyoutShowMode.Standard;
				}
				else
				{
					commandBar.OverflowButtonVisibility = CommandBarOverflowButtonVisibility.Auto;
				}

				SharedHelpers.QueueCallbackForCompositionRendering(() =>
				{
					if (commandBar is { } commandBarFlyoutCommandBar)
					{
						using var scopeGuard = Disposable.Create(() =>
						{
							commandBarFlyoutCommandBar.m_commandBarFlyoutIsOpening = false;
						});

						commandBarFlyoutCommandBar.m_commandBarFlyoutIsOpening = true;

						// If we don't have IFlyoutBase5 available, then we assume a standard show mode.
						if (ShowMode == FlyoutShowMode.Standard)
						{
#if HAS_UNO
							// In case of Uno Platform, the callback is executed too early, before CommandBarFlyoutCommandBar.OnApplyTemplate.
							// This causes unexpected behavior https://github.com/unoplatform/uno/issues/20984. To avoid this, we schedule the IsOpen change
							// on the next tick.
							DispatcherQueue.TryEnqueue(() =>
							{
								commandBar.IsOpen = true;
							});
#endif
						}
					}
				});
			}

			if (m_primaryCommands.Count > 0)
			{
				AddDropShadow();
			}
		};

		Opened += (s, e) =>
		{
			if (m_commandBar is { } commandBar)
			{
				if (commandBar.HasOpenAnimation())
				{
					commandBar.PlayOpenAnimation();
				}
			}

			if (FrameworkElementAutomationPeer.FromElement(m_commandBar) is { } commandBarPeer)
			{
				commandBarPeer.RaiseAutomationEvent(AutomationEvents.MenuOpened);
			}
		};

		Closing += (s, args) =>
		{
			if (m_commandBar is { } commandBar)
			{
				// We are not able to fade this shadow out with the V2 default opacity closing animition.
				// Additionally we drop shadows doing play well with the clip animation of the V1 style.
				// So we need to remove it in all cases.
				RemoveDropShadow();

				if (!m_isClosingAfterCloseAnimation && commandBar.HasCloseAnimation())
				{
					args.Cancel = true;

					CommandBarFlyout commandBarFlyout = this;

					commandBar.PlayCloseAnimation(WeakReferencePool.RentSelfWeakReference((IWeakReferenceProvider)this), () =>
					{
						m_isClosingAfterCloseAnimation = true;
						Hide();
						m_isClosingAfterCloseAnimation = false;
					});
				}
				else
				{
					// If we don't have an animation, close the command bar and thus it's subflyouts.
					commandBar.IsOpen = false;
				}

				//Drop shadows do not play nicely with clip animations, if we are using both, clear the shadow
				if (commandBar.OpenAnimationKind == CommandBarFlyoutOpenCloseAnimationKind.Clip)
				{
					commandBar.ClearShadow();
				}
			}
		};

		// Close the CommandBar in order to ensure that we're always starting from a known state when opening the flyout.
		Closed += (s, e) =>
		{
			if (m_commandBar is { } commandBar)
			{
				if (commandBar.IsOpen)
				{
					commandBar.IsOpen = false;
				}

				if (FrameworkElementAutomationPeer.FromElement(commandBar) is { } commandBarPeer)
				{
					commandBarPeer.RaiseAutomationEvent(AutomationEvents.MenuClosed);
				}
			}
		};
	}

#if !HAS_UNO // Detaching is not needed, as these are all children of the flyout child.
	CommandBarFlyout.~public CommandBarFlyout()
	{
		m_primaryCommands.VectorChanged(m_primaryCommandsVectorChangedToken);
		m_secondaryCommands.VectorChanged(m_secondaryCommandsVectorChangedToken);

		UnhookAllCommandBarElementDependencyPropertyChanges();
	}
#endif

	protected override Control CreatePresenter()
	{
		var commandBar = new CommandBarFlyoutCommandBar();

		// Localized string resource lookup is more expensive on MRTCore. Do the lookup ahead of time and reuse it for all
		// the CommandBarFlyoutCommandBar::EnsureLocalizedControlTypes calls in response to PrimaryCommands/SecondCommands
		// changed events.
		commandBar.CacheLocalizedStringResources();
		using var scopeGuard = Disposable.Create(() =>
		{
			commandBar.ClearLocalizedStringResourceCache();
		});

		SharedHelpers.CopyVector(m_primaryCommands, commandBar.PrimaryCommands);
		SharedHelpers.CopyVector(m_secondaryCommands, commandBar.SecondaryCommands);

		SetSecondaryCommandsToCloseWhenExecuted();
		HookAllCommandBarElementDependencyPropertyChanges();

		FlyoutPresenter presenter = new();
		presenter.Background = null;
		presenter.Foreground = null;
		presenter.BorderBrush = null;
		presenter.MinWidth = 0.0;
		presenter.MaxWidth = double.PositiveInfinity;
		presenter.MinHeight = 0.0;
		presenter.MaxHeight = double.PositiveInfinity;
		presenter.BorderThickness = ThicknessHelper.FromUniformLength(0);
		presenter.Padding = ThicknessHelper.FromUniformLength(0);
		presenter.Content = commandBar;
		// Clear the default CornerRadius(4) on FlyoutPresenter, CommandBarFlyout will do its own handling.
		presenter.CornerRadius = default;
		presenter.Translation = new Vector3(0.0f, 0.0f, 32.0f);

		// Disable the default shadow, as we'll be providing our own shadow.
		presenter.IsDefaultShadowEnabled = false;

		m_presenter = presenter;

		void onOpenedHandler(object sender, object args)
		{
			// If we open the CommandBar, then we should no longer be in a transient show mode -
			// we now know that the user wants to interact with us.
			ShowMode = FlyoutShowMode.Standard;
		}

		commandBar.Opened += onOpenedHandler;
		m_commandBarOpenedRevoker.Disposable = Disposable.Create(() => commandBar.Opened -= onOpenedHandler);

		commandBar.SetPresenter(presenter);

		// We'll need to remove the presenter's drop shadow on the commandBar's Opening/Closing
		// because we need it to disappear during its expand/shrink animation when the Overflow is opened.
		// It will be re-added once the storyboard for the overflow animations are completed.
		// That code can be found inside CommandBarFlyoutCommandBar.
		void onCommandBarOpening(object sender, object args)
		{
			if (m_commandBar is { } commandBar)
			{
				if (commandBar.HasSecondaryOpenCloseAnimations())
				{
					// We'll only need to do the mid-animation remove/add when the "..." button is
					// pressed to open/close the overflow. This means we shouldn't do it for AlwaysExpanded
					// and if there's nothing in the overflow.
					if (m_secondaryCommands.Count > 0)
					{
						RemoveDropShadow();
					}
				}
			}
		}

		commandBar.Opening += onCommandBarOpening;
		m_commandBarOpeningRevoker.Disposable = Disposable.Create(() => commandBar.Opening -= onCommandBarOpening);

		void onCommandBarClosing(object sender, object args)
		{
			if (m_commandBar is { } commandBar)
			{
				if (commandBar.HasSecondaryOpenCloseAnimations())
				{
					RemoveDropShadow();
				}
			}
		}
		commandBar.Closing += onCommandBarClosing;
		m_commandBarClosingRevoker.Disposable = Disposable.Create(() => commandBar.Closing -= onCommandBarClosing);

		commandBar.SetOwningFlyout(this);

		m_commandBar = commandBar;
		return presenter;
	}

	private void SetSecondaryCommandsToCloseWhenExecuted()
	{
		m_secondaryButtonClickRevokerByIndexMap.Clear();
		m_secondaryToggleButtonCheckedRevokerByIndexMap.Clear();
		m_secondaryToggleButtonUncheckedRevokerByIndexMap.Clear();

		void closeFlyoutFunc(object sender, object args)
		{
			Hide();
		}

		for (int i = 0; i < SecondaryCommands.Count; i++)
		{
			var element = SecondaryCommands[i];
			var button = element as AppBarButton;
			var toggleButton = element as AppBarToggleButton;

			if (button is not null && button.Flyout is null)
			{
				button.Click += closeFlyoutFunc;
				var clickRevoker = new SerialDisposable();
				clickRevoker.Disposable = Disposable.Create(() => button.Click -= closeFlyoutFunc);
				m_secondaryButtonClickRevokerByIndexMap[i] = clickRevoker;
			}
			else if (toggleButton is not null)
			{
				toggleButton.Checked += closeFlyoutFunc;
				var secondaryCheckedRevoker = new SerialDisposable();
				secondaryCheckedRevoker.Disposable = Disposable.Create(() => toggleButton.Checked -= closeFlyoutFunc);
				m_secondaryToggleButtonCheckedRevokerByIndexMap[i] = secondaryCheckedRevoker;

				toggleButton.Unchecked += closeFlyoutFunc;
				var secondaryUncheckedRevoker = new SerialDisposable();
				secondaryUncheckedRevoker.Disposable = Disposable.Create(() => toggleButton.Unchecked -= closeFlyoutFunc);
				m_secondaryToggleButtonUncheckedRevokerByIndexMap[i] = secondaryUncheckedRevoker;
			}
		}
	}

	internal void AddDropShadow()
	{
		if (m_presenter is { } presenter)
		{
			ThemeShadow shadow = new();
			presenter.Shadow = shadow;
		}
	}

	internal void RemoveDropShadow()
	{
		if (m_presenter is { } presenter)
		{
			presenter.Shadow = null;
		}
	}

	internal override Control GetPresenter() => m_presenter;

	private void HookAppBarButtonDependencyPropertyChanges(AppBarButton appBarButton, int index)
	{
		var commandBarElementDependencyPropertiesCount = s_commandBarElementDependencyPropertiesCount;

		var revokers = new IDisposable[commandBarElementDependencyPropertiesCount];
		m_propertyChangedRevokersByIndexMap[index] = revokers;

		for (int commandBarElementDependencyPropertyIndex = 0; commandBarElementDependencyPropertyIndex < commandBarElementDependencyPropertiesCount; commandBarElementDependencyPropertyIndex++)
		{
			var token = appBarButton.RegisterDisposablePropertyChangedCallback(
					s_appBarButtonDependencyProperties[commandBarElementDependencyPropertyIndex],
					OnCommandBarElementDependencyPropertyChanged);
			revokers[commandBarElementDependencyPropertyIndex] = token;
		}
	}

	private void HookAppBarToggleButtonDependencyPropertyChanges(AppBarToggleButton appBarToggleButton, int index)
	{
		var commandBarElementDependencyPropertiesCount = s_commandBarElementDependencyPropertiesCount;

		var revokers = new IDisposable[commandBarElementDependencyPropertiesCount];
		m_propertyChangedRevokersByIndexMap[index] = revokers;

		for (int commandBarElementDependencyPropertyIndex = 0; commandBarElementDependencyPropertyIndex < commandBarElementDependencyPropertiesCount; commandBarElementDependencyPropertyIndex++)
		{
			var disposable = appBarToggleButton.RegisterDisposablePropertyChangedCallback(s_appBarToggleButtonDependencyProperties[commandBarElementDependencyPropertyIndex], OnCommandBarElementDependencyPropertyChanged);
			revokers[commandBarElementDependencyPropertyIndex] = disposable;
		}
	}

	private void HookAllCommandBarElementDependencyPropertyChanges()
	{
		UnhookAllCommandBarElementDependencyPropertyChanges();

		for (int i = 0; i < SecondaryCommands.Count; i++)
		{
			var element = SecondaryCommands[i];
			var button = element as AppBarButton;
			var toggleButton = element as AppBarToggleButton;

			if (button is not null)
			{
				HookAppBarButtonDependencyPropertyChanges(button, i);
			}
			else if (toggleButton is not null)
			{
				HookAppBarToggleButtonDependencyPropertyChanges(toggleButton, i);
			}
		}
	}

	private void UnhookCommandBarElementDependencyPropertyChanges(int index, bool eraseRevokers = true)
	{
		if (m_propertyChangedRevokersByIndexMap.TryGetValue(index, out var revokers))
		{
			var commandBarElementDependencyPropertiesCount = s_commandBarElementDependencyPropertiesCount;

			for (int commandBarElementDependencyPropertyIndex = 0; commandBarElementDependencyPropertyIndex < commandBarElementDependencyPropertiesCount; commandBarElementDependencyPropertyIndex++)
			{
				revokers[commandBarElementDependencyPropertyIndex].Dispose();
				revokers[commandBarElementDependencyPropertyIndex] = null;
			}

			if (eraseRevokers)
			{
				m_propertyChangedRevokersByIndexMap.Remove(index);
			}
		}
	}

	private void UnhookAllCommandBarElementDependencyPropertyChanges()
	{
		foreach (var revokers in m_propertyChangedRevokersByIndexMap)
		{
			UnhookCommandBarElementDependencyPropertyChanges(revokers.Key, false /*eraseRevokers*/);
		}
		m_propertyChangedRevokersByIndexMap.Clear();
	}

	// Let the potential CommandBarFlyoutCommandBar know of the dependency property change so it can adjust its size.
	private void OnCommandBarElementDependencyPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
	{
		//COMMANDBARFLYOUT_TRACE_VERBOSE(this, TRACE_MSG_METH, METH_NAME, this);

		if (m_commandBar is { } commandBar)
		{
			commandBar.OnCommandBarElementDependencyPropertyChanged();
		}
	}
}
