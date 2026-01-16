using Windows.Foundation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Input;

using Microsoft.UI.Input;

namespace Microsoft.UI.Xaml.Controls.Primitives
{
	public partial class LoopingSelectorItem
	{
		internal LoopingSelectorItem()
		{
			_state = State.Normal;
			_visualIndex = 0;
			_pParentNoRef = null;
			_hasPeerBeenCreated = false;

			InitializeImpl();
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			var measureOverride = base.MeasureOverride(availableSize);
			return measureOverride;
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			var arrangeOverride = base.ArrangeOverride(finalSize);
			return arrangeOverride;
		}

		void InitializeImpl()
		{
			//wrl.ComPtr<xaml_controls.IContentControlFactory> spInnerFactory;
			//ContentControl spInnerInstance;
			//DependencyObject spInnerInspectable;

			//LoopingSelectorItemGenerated.InitializeImpl();
			//(wf.GetActivationFactory(
			//	wrl_wrappers.Hstring(RuntimeClass_Microsoft_UI_Xaml_Controls_ContentControl),
			//	&spInnerFactory));

			//(spInnerFactory.CreateInstance(
			//	(DependencyObject)((ILoopingSelectorItem)(this)),
			//	&spInnerInspectable,
			//	&spInnerInstance));

			//(SetComposableBasePointers(
			//		spInnerInspectable,
			//		spInnerFactory));

			//(Private.SetDefaultStyleKey(
			//		spInnerInspectable,
			//		"Microsoft.UI.Xaml.Controls.Primitives.LoopingSelectorItem"));

			DefaultStyleKey = typeof(LoopingSelectorItem);
		}

		protected override void OnPointerEntered(PointerRoutedEventArgs pEventArgs)
		{
			var pointerDeviceType = PointerDeviceType.Touch;
			PointerPoint spPointerPoint;
			global::Windows.Devices.Input.PointerDevice spPointerDevice;

			spPointerPoint = pEventArgs.GetCurrentPoint(null);
			if (spPointerPoint == null) { return; }

			spPointerDevice = spPointerPoint.PointerDevice;
			if (spPointerDevice == null) { return; }

			pointerDeviceType = (PointerDeviceType)spPointerDevice.PointerDeviceType;

			if (pointerDeviceType == PointerDeviceType.Mouse)
			{
				if (_state != State.Selected)
				{
					SetState(State.PointerOver, false);
				}
			}
		}

		protected override void OnPointerPressed(PointerRoutedEventArgs args)
		{
			if (_state != State.Selected)
			{
				SetState(State.Pressed, false);
			}
		}

		protected override void OnPointerExited(PointerRoutedEventArgs args)
		{
			if (_state != State.Selected)
			{
				SetState(State.Normal, false);
			}
		}

		protected override void OnPointerCaptureLost(PointerRoutedEventArgs args)
		{
			if (_state != State.Selected)
			{
				SetState(State.Normal, false);
			}
		}

		#region UIElementOverrides

		protected override AutomationPeer OnCreateAutomationPeer()
		{
			AutomationPeer returnValue;
			LoopingSelectorItemAutomationPeer spLoopingSelectorItemAutomationPeer;

			//(wrl.MakeAndInitialize<xaml_automation_peers.LoopingSelectorItemAutomationPeer>
			//		(spLoopingSelectorItemAutomationPeer, this));
			spLoopingSelectorItemAutomationPeer = new LoopingSelectorItemAutomationPeer(this);


			_hasPeerBeenCreated = true;

			//spLoopingSelectorItemAutomationPeer.CopyTo(returnValue);
			returnValue = spLoopingSelectorItemAutomationPeer;

			return returnValue;
		}

		#endregion

		void GoToState(State newState, bool useTransitions)
		{
			string strState = null;

			switch (newState)
			{
				case State.Normal:
					strState = "Normal";
					break;
				case State.Expanded:
					strState = "Expanded";
					break;
				case State.Selected:
					strState = "Selected";
					break;
				case State.PointerOver:
					strState = "PointerOver";
					break;
				case State.Pressed:
					strState = "Pressed";
					break;
			}

			//wrl.ComPtr<xaml.IVisualStateManagerStatics> spVSMStatics;
			//wrl.ComPtr<xaml_controls.IControl> spThisAsControl;

			//(wf.GetActivationFactory(
			//	wrl_wrappers.Hstring(RuntimeClass_Microsoft_UI_Xaml_VisualStateManager),
			//	&spVSMStatics));

			//QueryInterface(__uuidof(xaml_controls.IControl), &spThisAsControl);

			//boolean returnValue = false;
			//spVSMStatics.GoToState(spThisAsControl, wrl_wrappers.Hstring(strState), useTransitions, &returnValue);
			VisualStateManager.GoToState(this, strState, useTransitions);
		}

		internal void SetState(State newState, bool useTransitions)
		{
			// NOTE: Not calling GoToState when the LSI is already in the target
			// state allows us to keep animations looking smooth when the following
			// sequence of events happens:
			// LS starts closing . Items changes . LSIs are Recycled/Realized/Assigned New Content
			if (newState != _state)
			{
				GoToState(newState, useTransitions);
				_state = newState;
			}
		}

		internal void AutomationSelect()
		{
			LoopingSelector pLoopingSelectorNoRef = null;
			GetParentNoRef(out pLoopingSelectorNoRef);
			pLoopingSelectorNoRef.AutomationScrollToVisualIdx(_visualIndex, true /* ignoreScrollingState */);
		}

		internal void AutomationGetSelectionContainerUIAPeer(out AutomationPeer ppPeer)
		{
			//wrl.ComPtr<xaml.Automation.Peers.FrameworkElementAutomationPeerStatics> spAutomationPeerStatics;
			AutomationPeer spAutomationPeer;
			UIElement spLoopingSelectorAsUI;
			LoopingSelector pLoopingSelectorNoRef = null;

			GetParentNoRef(out pLoopingSelectorNoRef);
			//(pLoopingSelectorNoRef.QueryInterface(
			//	__uuidof(UIElement),
			//	&spLoopingSelectorAsUI));
			spLoopingSelectorAsUI = this;

			//(wf.GetActivationFactory(
			//	  wrl_wrappers.Hstring(RuntimeClass_Microsoft_UI_Xaml_Automation_Peers_FrameworkElementAutomationPeer),
			//	  &spAutomationPeerStatics));
			//spAutomationPeerStatics.CreatePeerForElement(spLoopingSelectorAsUI, &spAutomationPeer);
			spAutomationPeer = FrameworkElementAutomationPeer.CreatePeerForElement(spLoopingSelectorAsUI);
			//spAutomationPeer.CopyTo(ppPeer);
			ppPeer = spAutomationPeer;
		}

		internal void AutomationGetIsSelected(out bool value)
		{
			LoopingSelector loopingSelectorNoRef = null;
			int selectedIdx;

			GetParentNoRef(out loopingSelectorNoRef);
			selectedIdx = loopingSelectorNoRef.SelectedIndex;

			uint itemIndex = 0;
			loopingSelectorNoRef.VisualIndexToItemIndex((uint)_visualIndex, out itemIndex);

			value = selectedIdx == (int)itemIndex;
		}

		internal void AutomationUpdatePeerIfExists(int itemIndex)
		{
			if (_hasPeerBeenCreated)
			{
				AutomationPeer spAutomationPeer;
				LoopingSelectorItemAutomationPeer spLSIAP;
				LoopingSelectorItemAutomationPeer pLSIAP;
				UIElement spThisAsUI;
				//wrl.ComPtr<xaml_automation_peers.FrameworkElementAutomationPeerStatics> spAutomationPeerStatics;

				//QueryInterface(__uuidof(UIElement), &spThisAsUI);
				spThisAsUI = this;
				//(wf.GetActivationFactory(
				//	wrl_wrappers.Hstring(RuntimeClass_Microsoft_UI_Xaml_Automation_Peers_FrameworkElementAutomationPeer),
				//	&spAutomationPeerStatics));
				// CreatePeerForElement does not always create one - if there is one associated with this UIElement it will reuse that.
				// We do not want to end up creating a bunch of peers for the same element causing Narrator to get confused.
				//spAutomationPeerStatics.CreatePeerForElement(spThisAsUI, &spAutomationPeer);
				spAutomationPeer = FrameworkElementAutomationPeer.CreatePeerForElement(spThisAsUI);
				//spAutomationPeer.As(spLSIAP);
				spLSIAP = spAutomationPeer as LoopingSelectorItemAutomationPeer;
				pLSIAP = spLSIAP;

				pLSIAP.UpdateEventSource();
				pLSIAP.UpdateItemIndex(itemIndex);
			}
		}
	}
}
