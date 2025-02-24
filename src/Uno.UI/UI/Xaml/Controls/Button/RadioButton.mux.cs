#nullable enable

using System;
using System.Collections.Generic;
using DirectUI;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;
using Windows.System;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using static Microsoft/* UWP don't rename */.UI.Xaml.Controls._Tracing;

namespace Windows.UI.Xaml.Controls
{
	public partial class RadioButton : ToggleButton
	{
		private protected override void Initialize()
		{
			base.Initialize();
			// Ignore the ENTER key by default
			SetAcceptsReturn(false);
			Register("", this);
		}

		internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
		{
			base.OnPropertyChanged2(args);

			if (args.Property == GroupNameProperty)
			{
				OnGroupNamePropertyChanged(args.OldValue, args.NewValue);
			}
			else if (args.Property == IsCheckedProperty)
			{
				AutomationPeer? automationPeer = null;

				bool bOldValue = false;
				bool bNewValue = false;

				var bListenerExistsForPropertyChangedEvent = AutomationPeer.ListenerExistsHelper(AutomationEvents.PropertyChanged);
				var bListenerExistsForElementSelectedEvent = AutomationPeer.ListenerExistsHelper(AutomationEvents.SelectionItemPatternOnElementSelected);
				var bListenerExistsForElementRemovedFromSelectionEvent = AutomationPeer.ListenerExistsHelper(AutomationEvents.SelectionItemPatternOnElementRemovedFromSelection);

				if (bListenerExistsForPropertyChangedEvent ||
					bListenerExistsForElementSelectedEvent ||
					bListenerExistsForElementRemovedFromSelectionEvent)
				{
					automationPeer = GetOrCreateAutomationPeer();

					var spOldValue = (bool?)args.OldValue;
					var spNewValue = (bool?)args.NewValue;

					if (spOldValue != null)
					{
						bOldValue = spOldValue.Value;
					}

					if (spNewValue != null)
					{
						bNewValue = spNewValue.Value;
					}
				}

				if (automationPeer != null)
				{
					if (bListenerExistsForPropertyChangedEvent)
					{
						if (automationPeer is RadioButtonAutomationPeer radioButtonAutomationPeer)
						{
							radioButtonAutomationPeer.RaiseIsSelectedPropertyChangedEvent(bOldValue, bNewValue);
						}
					}

					if (bOldValue != bNewValue)
					{
						if (bListenerExistsForElementSelectedEvent && bNewValue)
						{
							automationPeer.RaiseAutomationEvent(AutomationEvents.SelectionItemPatternOnElementSelected);
						}

						if (bListenerExistsForElementRemovedFromSelectionEvent && !bNewValue)
						{
							automationPeer.RaiseAutomationEvent(AutomationEvents.SelectionItemPatternOnElementRemovedFromSelection);
						}
					}
				}
			}
		}

		private void OnGroupNamePropertyChanged(object pOldValue, object pNewValue)
		{
			var strOldValue = pOldValue as string;
			var strNewValue = pNewValue as string;

			Unregister(strOldValue ?? "", this);
			Register(strNewValue ?? "", this);
		}

		private protected override void OnChecked()
		{
			UpdateRadioButtonGroup();
			base.OnChecked();
		}

		protected override void OnToggle()
		{
			IsChecked = true;
		}

		private static void Register(string groupName, RadioButton radioButton)
		{
			if (radioButton is null)
			{
				throw new ArgumentNullException(nameof(radioButton));
			}

			// When registering, require DXamlCore to create the table of radio button group names if it doesn't exist.
			var groupsByName = DXamlCore.Current.GetRadioButtonGroupsByName(true);

			if (!groupsByName!.TryGetValue(groupName, out var groupElements))
			{
				groupElements = new List<WeakReference<RadioButton>>();

				groupsByName.Add(groupName, groupElements);
			}

			// Keep a weak ref to the button; we don't want this list to cause the button to leak, and IWeakReference is
			// automatically updated during GC.
			var weakRef = new WeakReference<RadioButton>(radioButton);
			groupElements.Add(weakRef);
		}

		/// <summary>
		/// Unregister by searching for the instance in all groups. This is safer during shutdown, because
		/// GroupName may access an external (CLR) string which may have been GC'ed.
		/// </summary>
		private static void UnregisterSafe(RadioButton radioButton)
		{
			if (radioButton is null)
			{
				throw new ArgumentNullException(nameof(radioButton));
			}

			var groupsByName = DXamlCore.Current.GetRadioButtonGroupsByName(false);

			// DXamlCore may have already deleted the list of names depending on the order of operations during shutdown,
			// so it may be a nullptr.
			if (groupsByName != null)
			{
				var toRemove = new List<string>();
				foreach (var groupElements in groupsByName)
				{
					var found = UnregisterFromGroup(groupElements.Value, radioButton);

					if (groupElements.Value.Count == 0)
					{
						toRemove.Add(groupElements.Key);
					}

					if (found)
					{
						break;
					}
				}

				foreach (var key in toRemove)
				{
					groupsByName.Remove(key);
				}
			}
		}

		/// <summary>
		/// Unregister within a specific group name. This is faster, but not safe during shutdown.
		/// </summary>
		/// <param name="groupName">Group name.</param>
		/// <param name="radioButton">RadioButton.</param>
		private static void Unregister(string groupName, RadioButton radioButton)
		{
			if (radioButton is null)
			{
				throw new ArgumentNullException(nameof(radioButton));
			}

			bool found = false;

			var groupsByName = DXamlCore.Current.GetRadioButtonGroupsByName(false);

			// DXamlCore may have already deleted the list of names depending on the order of operations during shutdown,
			// so it may be a nullptr.
			if (groupsByName != null)
			{
				if (groupsByName.TryGetValue(groupName, out var groupElements))
				{
					found = UnregisterFromGroup(groupElements, radioButton);

					if (groupElements.Count == 0)
					{
						groupsByName.Remove(groupName);
					}
				}
			}
		}

		private static bool UnregisterFromGroup(List<WeakReference<RadioButton>> groupElements, RadioButton radioButton)
		{
			bool found = false;

			// Remove pRadioButton from the list
			bool fRestart = false;

			for (int i = 0; i < groupElements.Count; i = fRestart ? 0 : i + 1)
			{
				fRestart = false;

				// Resolve the weak ref to a RadioButton
				groupElements[i].TryGetTarget(out var radioButtonCheck);

				// If this is the radio button we're looking for, or it's a dead weak reference, we want
				// to remove this item from the list.
				if (radioButtonCheck == null || radioButtonCheck == radioButton)
				{
					// Remove the item
					groupElements.RemoveAt(i);

					// If this was the item we were looking for, we're done.
					if (radioButtonCheck == radioButton)
					{
						found = true;
						break;
					}
					// Otherwise, we removed this because it was a dead weak ref, but it might not have been the item
					// we were looking for. There could be more dead weak refs in this list, or the actual
					// live item still in this list. For that case, restart the enumeration and continue.
					else
					{
						MUX_ASSERT(radioButtonCheck == null);
						fRestart = true;
					}
				}
			}

			return found;
		}

		private void UpdateRadioButtonGroup()
		{
			RadioButton? pRadioButtonNoRef = null;
			bool bIsChecked = false;

			bool groupNameExists = false;
			string? groupName;
			GetGroupName(out groupNameExists, out groupName);

			var groupsByName = DXamlCore.Current.GetRadioButtonGroupsByName(false);

			// DXamlCore will delete RadioButton name table during shutdown, so check against it being a nullptr or having 0 elements.
			if (groupsByName != null && groupsByName.Count > 0)
			{
				if (groupsByName.TryGetValue(groupName, out var groupElements))
				{
					DependencyObject? spParent = GetParentForGroup(groupNameExists, this);

					foreach (var radioButtonRef in groupElements)
					{
						// Resolve the weak reference to get an actual RadioButton
						// If the weak reference is dead, it's not a match, we can move on (it will get cleaned up
						// later when the RadioButton is destructed).
						if (!radioButtonRef.TryGetTarget(out var radioButton))
						{
							continue;
						}

						// Cast to a class so we can call all the members
						pRadioButtonNoRef = radioButton;

						var spIsCheckedReference = pRadioButtonNoRef.IsChecked;
						if (spIsCheckedReference != null)
						{
							bIsChecked = spIsCheckedReference.Value;
						}

						if (pRadioButtonNoRef != this && (spIsCheckedReference == null || bIsChecked))
						{
							var spCurrentParent = GetParentForGroup(groupNameExists, pRadioButtonNoRef);

							if (spParent == spCurrentParent)
							{
								pRadioButtonNoRef.IsChecked = false;
							}
						}
					}
				}
				else
				{
					if (!groupNameExists)
					{
						throw new InvalidOperationException("Trying to update RadioButton group which does not exist.");
					}
				}
			}
		}

		private void GetGroupName(out bool groupNameExists, out string groupName)
		{
			groupNameExists = false;
			groupName = "";

			var strGroupName = GroupName;
			if (!string.IsNullOrEmpty(strGroupName))
			{
				groupName = strGroupName;
				groupNameExists = true;
			}
		}

		private DependencyObject? GetParentForGroup(bool groupNameExists, RadioButton radioButton)
		{
			DependencyObject? parent = null;
			DependencyObject? radioButtonParent;

			// If there is no groupName, then get the parent of the RadioButton.
			if (!groupNameExists)
			{
				radioButtonParent = radioButton.Parent;
			}
			// Otherwise, use the root
			else
			{
				radioButtonParent = VisualTree.GetRootForElement(radioButton); //Uno specific: Return RootVisual instead of VisualTreeHelper.GetRootStatic(radioButton);
			}

			if (radioButtonParent != null)
			{
				parent = radioButtonParent;
			}

			return parent;
		}

		internal void AutomationRadioButtonOnToggle()
		{
			// OnToggle through UIAutomation
			OnClick();
		}

		/// <summary>
		/// Create RadioButtonAutomationPeer to represent the RadioButton.
		/// </summary>
		/// <returns>Automation peer.</returns>
		protected override AutomationPeer OnCreateAutomationPeer() => new RadioButtonAutomationPeer(this);

		protected override void OnKeyDown(KeyRoutedEventArgs args)
		{
			base.OnKeyDown(args);

			bool handled = args.Handled;
			if (!handled)
			{
				//GetGroupName(out var groupNameExists, out var groupName);

				// We need to get OriginalKey here and not the "mapped" key because we want to focus
				// the next element in RadioButton "group" for Up/Down and Left/Right keys only for Keyboard,
				// but for other input devices, like Gamepad or Remote, we want the default focus behavior.
				var originalKey = args.OriginalKey;

				bool wasFocused = false;
				switch (originalKey)
				{
					case VirtualKey.Down:
					case VirtualKey.Right:
						wasFocused = FocusNextElementInGroup(/* moveForward */ true);
						handled = true;
						break;
					case VirtualKey.Up:
					case VirtualKey.Left:
						wasFocused = FocusNextElementInGroup(/* moveForward */ false);
						handled = true;
						break;
				}
				args.Handled = handled;
			}
		}

		private bool FocusNextElementInGroup(bool moveForward)
		{
			bool wasFocused = false;

			GetGroupName(out var currentGroupNameExists, out var currentGroupName);

			DependencyObject? currentParentDO = GetParentForGroup(currentGroupNameExists, this);

			var pFocusManager = VisualTree.GetFocusManagerForElement(this);
			if (currentParentDO != null && pFocusManager != null)
			{
				DependencyObject? nextFocusCandidate = null;
				DependencyObject? currentRadioButtonDO = this;
				DependencyObject? firstFocusCandidate = pFocusManager.GetFirstFocusableElement(currentParentDO); // First focusable element, given current parent.
				DependencyObject? lastFocusCandidate = pFocusManager.GetLastFocusableElement(currentParentDO); // Last focusable element, given current parent.
				FocusNavigationDirection navigationDirection = FocusNavigationDirection.None;

				// Focus candidate is to set to Next or Previous Tab Stop, depending on movement direction, unless we have reached the first/last element, in which case,
				// we loop around.
				if (moveForward)
				{
					nextFocusCandidate = (currentRadioButtonDO == lastFocusCandidate) ? firstFocusCandidate : pFocusManager.GetNextTabStop(currentRadioButtonDO, false);
					navigationDirection = FocusNavigationDirection.Next;
				}
				else
				{
					nextFocusCandidate = (currentRadioButtonDO == firstFocusCandidate) ? lastFocusCandidate : pFocusManager.GetPreviousTabStop(currentRadioButtonDO);
					navigationDirection = FocusNavigationDirection.Previous;
				}

				// Search for next focus candidate until we have looked at all focusable elements in the group.
				while (nextFocusCandidate != null && nextFocusCandidate != currentRadioButtonDO)
				{
					// Check to see if the nextFocusCandidate is a RadioButton.
					if (nextFocusCandidate is RadioButton nextFocusRadio)
					{
						bool haveCommonAncestor = false;
						DependencyObject nextFocusCandidatePeer = nextFocusCandidate;
						if (currentParentDO != null)
						{
							haveCommonAncestor = currentParentDO.IsAncestorOf(nextFocusCandidatePeer);
						}

						if (haveCommonAncestor)
						{
							bool nextGroupNameExists = false;
							string? nextGroupName;
							nextFocusRadio.GetGroupName(out nextGroupNameExists, out nextGroupName);

							if (currentGroupName == nextGroupName)
							{
								pFocusManager.SetFocusedElement(new FocusMovement(nextFocusCandidate, navigationDirection, FocusState.Keyboard));
								wasFocused = true;
								break;
							}
						}
					}

					if (moveForward)
					{
						nextFocusCandidate = pFocusManager.GetNextTabStop(nextFocusCandidate, false);
						navigationDirection = FocusNavigationDirection.Next;
					}
					else
					{
						nextFocusCandidate = pFocusManager.GetPreviousTabStop(nextFocusCandidate);
						navigationDirection = FocusNavigationDirection.Previous;
					}
				}
			}
			return wasFocused;
		}

		/// <summary>
		/// Change to the correct visual state for the RadioButton.
		/// </summary>
		/// <param name="useTransitions">Use transitions.</param>
		private protected override void ChangeVisualState(bool useTransitions)
		{
			var isEnabled = IsEnabled;
			var isPressed = IsPressed;
			var isPointerOver = IsPointerOver;
			var focusState = FocusState;

			var isCheckedReference = IsChecked;

			// Update the Interaction state group
			if (!isEnabled)
			{
				GoToState(useTransitions, "Disabled");
			}
			else if (isPressed)
			{
				GoToState(useTransitions, "Pressed");
			}
			else if (isPointerOver)
			{
				GoToState(useTransitions, "PointerOver");
			}
			else
			{
				GoToState(useTransitions, "Normal");
			}

			//Update the Check state group
			if (isCheckedReference == null)
			{
				// Indeterminate
				GoToState(useTransitions, "Indeterminate");
			}
			else if (isCheckedReference == true)
			{
				// Checked
				GoToState(useTransitions, "Checked");
			}
			else
			{
				// Unchecked
				GoToState(useTransitions, "Unchecked");
			}

			// Update the Focus group
			if (focusState != FocusState.Unfocused && isEnabled)
			{
				if (focusState == FocusState.Pointer)
				{
					GoToState(useTransitions, "PointerFocused");
				}
				else
				{
					GoToState(useTransitions, "Focused");
				}
			}
			else
			{
				GoToState(useTransitions, "Unfocused");
			}
		}
	}
}
