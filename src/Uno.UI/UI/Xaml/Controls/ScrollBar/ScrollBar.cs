using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
using Uno.Disposables;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using DirectUI;


#if HAS_UNO_WINUI
using Microsoft.UI.Input;
#else
using Windows.Devices.Input;
using Windows.UI.Input;
#endif

namespace Microsoft.UI.Xaml.Controls.Primitives
{
	public partial class ScrollBar
	{
		[ThreadStatic]
		private static Orientation? _fixedOrientation;

		internal static IDisposable MaterializingFixed(Orientation orientation)
		{
			_fixedOrientation = orientation;
			return Disposable.Create(() => _fixedOrientation = null);
		}

		// Flag indicating whether the ScrollBar must react to user input or not.
		bool m_isIgnoringUserInput;

		// Flag indicating whether the mouse is over the
		bool m_isPointerOver;

		// Used to prevent GoToState(true /*bUseTransitions*/) calls while applying the template.
		// We don't want to show the initial fade-out of the mouse/panning indicators.
		bool m_suspendVisualStateUpdates = true; // = true: Visual state update are disabled until the template has been applied at least once!

		// Value indicating how far the ScrollBar has been dragged.
		double m_dragValue;

		// Template parts for the horizontal and vertical templates
		// (each including a root, increase small/large repeat buttons, and
		// a thumb).
		FrameworkElement m_tpElementHorizontalTemplate;
		RepeatButton m_tpElementHorizontalLargeIncrease;
		RepeatButton m_tpElementHorizontalLargeDecrease;
		RepeatButton m_tpElementHorizontalSmallIncrease;
		RepeatButton m_tpElementHorizontalSmallDecrease;
		Thumb m_tpElementHorizontalThumb;
		FrameworkElement m_tpElementVerticalTemplate;
		RepeatButton m_tpElementVerticalLargeIncrease;
		RepeatButton m_tpElementVerticalLargeDecrease;
		RepeatButton m_tpElementVerticalSmallIncrease;
		RepeatButton m_tpElementVerticalSmallDecrease;
		Thumb m_tpElementVerticalThumb;

		FrameworkElement m_tpElementHorizontalPanningRoot;
		FrameworkElement m_tpElementHorizontalPanningThumb;
		FrameworkElement m_tpElementVerticalPanningRoot;
		FrameworkElement m_tpElementVerticalPanningThumb;

		// Event registration tokens for events attached to template parts
		// so the handlers can be removed if we apply a new template.
		SerialDisposable m_ElementHorizontalThumbDragStartedToken = new SerialDisposable();
		SerialDisposable m_ElementHorizontalThumbDragDeltaToken = new SerialDisposable();
		SerialDisposable m_ElementHorizontalThumbDragCompletedToken = new SerialDisposable();
		SerialDisposable m_ElementHorizontalLargeDecreaseClickToken = new SerialDisposable();
		SerialDisposable m_ElementHorizontalLargeIncreaseClickToken = new SerialDisposable();
		SerialDisposable m_ElementHorizontalSmallDecreaseClickToken = new SerialDisposable();
		SerialDisposable m_ElementHorizontalSmallIncreaseClickToken = new SerialDisposable();
		SerialDisposable m_ElementVerticalThumbDragStartedToken = new SerialDisposable();
		SerialDisposable m_ElementVerticalThumbDragDeltaToken = new SerialDisposable();
		SerialDisposable m_ElementVerticalThumbDragCompletedToken = new SerialDisposable();
		SerialDisposable m_ElementVerticalLargeDecreaseClickToken = new SerialDisposable();
		SerialDisposable m_ElementVerticalLargeIncreaseClickToken = new SerialDisposable();
		SerialDisposable m_ElementVerticalSmallDecreaseClickToken = new SerialDisposable();
		SerialDisposable m_ElementVerticalSmallIncreaseClickToken = new SerialDisposable();

		// value that indicates that we are currently blocking indicators from showing
		bool m_blockIndicators;

		// Enters/Leaves the mode where the child's actual size is used
		// for the extent exposed through IScrollInfo
		bool m_isUsingActualSizeAsExtent;

		static bool IsConscious()
			=> Uno.UI.Helpers.WinUI.SharedHelpers.ShouldUseDynamicScrollbars();

		// Returns True/False depending on the OS Settings "Play animations in Windows" value. A True OS value can be overridden by
		// the RuntimeEnabledFeature::DisableGlobalAnimations key for testing purposes.
		static bool IsAnimationEnabled
			=> Uno.UI.Helpers.WinUI.SharedHelpers.IsAnimationsEnabled();

		/// <summary>
		/// Indicates if this scrollbar supports to change its orientation once its template has been applied (cf. remarks).
		/// This is false by default (which means that the ScrollBar will support dynamic orientation changes).
		/// </summary>
		/// <remarks>
		/// This flag is for performance consideration, it allows ScrollBar to load only half of its template.
		/// It's used by core controls (e.g. ScrollViewer) where the ScrollBar's orientation will never change.
		/// It's required as, unlike UWP, a control which is Visibility = Collapsed will get its template applied anyway.
		/// </remarks>
		internal bool IsFixedOrientation { get; set; }

		// Initializes a new instance of the ScrollBar class.
		public ScrollBar()
		{
			m_isIgnoringUserInput = false;
			m_isPointerOver = false;
			m_suspendVisualStateUpdates = false;
			m_dragValue = 0.0;
			m_blockIndicators = false;
			m_isUsingActualSizeAsExtent = false;

			if (_fixedOrientation is Orientation fixedOrientation)
			{
				IsFixedOrientation = true;
				Orientation = fixedOrientation;
			}

			Initialize();
		}

		// Prepares object's state

		void Initialize()
		{
			DefaultStyleKey = typeof(ScrollBar);

			SizeChanged += OnSizeChanged;
			LayoutUpdated += OnLayoutUpdated;
			Loaded += ReAttachEvents;
			Unloaded += DetachEvents;
		}

		// Update the visual states when the Visibility property is changed.
		protected override void OnVisibilityChanged(Visibility oldValue, Visibility newValue)
		{
			base.OnVisibilityChanged(oldValue, newValue);
			var visibility = Visibility;
			if (Visibility.Visible == visibility)
			{
				UpdateVisualState();
			}
			else
			{
				m_isPointerOver = false;
			}
		}

		// Apply a template to the
		protected override void OnApplyTemplate()
		{
			string strAutomationName;
			m_suspendVisualStateUpdates = true;

			FrameworkElement spElementHorizontalTemplate;
			FrameworkElement spElementVerticalTemplate;
			FrameworkElement spElementHorizontalPanningRoot;
			FrameworkElement spElementHorizontalPanningThumb;
			FrameworkElement spElementVerticalPanningRoot;
			FrameworkElement spElementVerticalPanningThumb;

			RepeatButton spElementHorizontalLargeIncrease;
			RepeatButton spElementHorizontalLargeDecrease;
			RepeatButton spElementHorizontalSmallIncrease;
			RepeatButton spElementHorizontalSmallDecrease;

			RepeatButton spElementVerticalLargeIncrease;
			RepeatButton spElementVerticalLargeDecrease;
			RepeatButton spElementVerticalSmallIncrease;
			RepeatButton spElementVerticalSmallDecrease;

			Thumb spElementVerticalThumb;
			Thumb spElementHorizontalThumb;

			// Cleanup any existing template parts
			DetachEvents();

			// Apply the template to the base class
			base.OnApplyTemplate();

			// Get the parts
			if (!IsFixedOrientation || Orientation == Orientation.Horizontal)
			{
				spElementHorizontalTemplate = GetTemplateChildHelper<FrameworkElement>("HorizontalRoot");
				m_tpElementHorizontalTemplate = spElementHorizontalTemplate;
				spElementHorizontalLargeIncrease = GetTemplateChildHelper<RepeatButton>("HorizontalLargeIncrease");
				m_tpElementHorizontalLargeIncrease = spElementHorizontalLargeIncrease;
				if (m_tpElementHorizontalLargeIncrease != null)
				{
					strAutomationName = AutomationProperties.GetName(m_tpElementHorizontalLargeIncrease);

					if (strAutomationName == null)
					{
						strAutomationName = DXamlCore.Current.GetLocalizedResourceString("UIA_SCROLLBAR_HORIZONTALLARGEINCREASE");
						AutomationProperties.SetName(m_tpElementHorizontalLargeIncrease as RepeatButton, strAutomationName);
					}
				}
				spElementHorizontalSmallIncrease = GetTemplateChildHelper<RepeatButton>("HorizontalSmallIncrease");
				m_tpElementHorizontalSmallIncrease = spElementHorizontalSmallIncrease;
				if (m_tpElementHorizontalSmallIncrease != null)
				{
					strAutomationName = AutomationProperties.GetName(m_tpElementHorizontalSmallIncrease);

					if (strAutomationName == null)
					{
						strAutomationName = DXamlCore.Current.GetLocalizedResourceString("UIA_SCROLLBAR_HORIZONTALSMALLINCREASE");
						AutomationProperties.SetName(m_tpElementHorizontalSmallIncrease, strAutomationName);

					}
				}
				spElementHorizontalLargeDecrease = GetTemplateChildHelper<RepeatButton>("HorizontalLargeDecrease");
				m_tpElementHorizontalLargeDecrease = spElementHorizontalLargeDecrease;
				if (m_tpElementHorizontalLargeDecrease != null)
				{
					strAutomationName = AutomationProperties.GetName(m_tpElementHorizontalLargeDecrease);

					if (strAutomationName == null)
					{
						strAutomationName = DXamlCore.Current.GetLocalizedResourceString("UIA_SCROLLBAR_HORIZONTALLARGEDECREASE");
						AutomationProperties.SetName(m_tpElementHorizontalLargeDecrease, strAutomationName);

					}
				}
				spElementHorizontalSmallDecrease = GetTemplateChildHelper<RepeatButton>("HorizontalSmallDecrease");
				m_tpElementHorizontalSmallDecrease = spElementHorizontalSmallDecrease;
				if (m_tpElementHorizontalSmallDecrease != null)
				{
					strAutomationName = AutomationProperties.GetName(m_tpElementHorizontalSmallDecrease);

					if (strAutomationName == null)
					{
						strAutomationName = DXamlCore.Current.GetLocalizedResourceString("UIA_SCROLLBAR_HORIZONTALSMALLDECREASE");
						AutomationProperties.SetName(m_tpElementHorizontalSmallDecrease, strAutomationName);

					}
				}
				spElementHorizontalThumb = GetTemplateChildHelper<Thumb>("HorizontalThumb");
				m_tpElementHorizontalThumb = spElementHorizontalThumb;
				if (m_tpElementHorizontalThumb != null)
				{
					strAutomationName = AutomationProperties.GetName(m_tpElementHorizontalThumb);

					if (strAutomationName == null)
					{
						strAutomationName = DXamlCore.Current.GetLocalizedResourceString("UIA_SCROLLBAR_HORIZONTALTHUMB");
						AutomationProperties.SetName(m_tpElementHorizontalThumb, strAutomationName);

					}
				}
			}

			if (!IsFixedOrientation || Orientation == Orientation.Vertical)
			{
				spElementVerticalTemplate = GetTemplateChildHelper<FrameworkElement>("VerticalRoot");
				m_tpElementVerticalTemplate = spElementVerticalTemplate;

				spElementVerticalLargeIncrease = GetTemplateChildHelper<RepeatButton>("VerticalLargeIncrease");
				m_tpElementVerticalLargeIncrease = spElementVerticalLargeIncrease;
				if (m_tpElementVerticalLargeIncrease != null)
				{
					strAutomationName = AutomationProperties.GetName(m_tpElementVerticalLargeIncrease);

					if (strAutomationName == null)
					{
						strAutomationName = DXamlCore.Current.GetLocalizedResourceString("UIA_SCROLLBAR_VERTICALALLARGEINCREASE");
						AutomationProperties.SetName(m_tpElementVerticalLargeIncrease, strAutomationName);

					}
				}

				spElementVerticalSmallIncrease = GetTemplateChildHelper<RepeatButton>("VerticalSmallIncrease");
				m_tpElementVerticalSmallIncrease = spElementVerticalSmallIncrease;
				if (m_tpElementVerticalSmallIncrease != null)
				{
					strAutomationName = AutomationProperties.GetName(m_tpElementVerticalSmallIncrease);

					if (strAutomationName == null)
					{
						strAutomationName = DXamlCore.Current.GetLocalizedResourceString("UIA_SCROLLBAR_VERTICALSMALLINCREASE");
						AutomationProperties.SetName(m_tpElementVerticalSmallIncrease, strAutomationName);

					}
				}
				spElementVerticalLargeDecrease = GetTemplateChildHelper<RepeatButton>("VerticalLargeDecrease");
				m_tpElementVerticalLargeDecrease = spElementVerticalLargeDecrease;
				if (m_tpElementVerticalLargeDecrease != null)
				{
					strAutomationName = AutomationProperties.GetName(m_tpElementVerticalLargeDecrease);

					if (strAutomationName == null)
					{
						strAutomationName = DXamlCore.Current.GetLocalizedResourceString("UIA_SCROLLBAR_VERTICALLARGEDECREASE");
						AutomationProperties.SetName(m_tpElementVerticalLargeDecrease, strAutomationName);

					}
				}
				spElementVerticalSmallDecrease = GetTemplateChildHelper<RepeatButton>("VerticalSmallDecrease");
				m_tpElementVerticalSmallDecrease = spElementVerticalSmallDecrease;
				if (m_tpElementVerticalSmallDecrease != null)
				{
					strAutomationName = AutomationProperties.GetName(m_tpElementVerticalSmallDecrease);

					if (strAutomationName == null)
					{
						strAutomationName = DXamlCore.Current.GetLocalizedResourceString("UIA_SCROLLBAR_VERTICALSMALLDECREASE");
						AutomationProperties.SetName(m_tpElementVerticalSmallDecrease, strAutomationName);

					}
				}
				spElementVerticalThumb = GetTemplateChildHelper<Thumb>("VerticalThumb");
				m_tpElementVerticalThumb = spElementVerticalThumb;
				if (m_tpElementVerticalThumb != null)
				{
					strAutomationName = AutomationProperties.GetName(m_tpElementVerticalThumb);

					if (strAutomationName == null)
					{
						strAutomationName = DXamlCore.Current.GetLocalizedResourceString("UIA_SCROLLBAR_VERTICALTHUMB");
						AutomationProperties.SetName(m_tpElementVerticalThumb as Thumb, strAutomationName);

					}
				}

				spElementHorizontalPanningRoot = GetTemplateChildHelper<FrameworkElement>("HorizontalPanningRoot");
				m_tpElementHorizontalPanningRoot = spElementHorizontalPanningRoot;
				spElementHorizontalPanningThumb = GetTemplateChildHelper<FrameworkElement>("HorizontalPanningThumb");
				m_tpElementHorizontalPanningThumb = spElementHorizontalPanningThumb;
				spElementVerticalPanningRoot = GetTemplateChildHelper<FrameworkElement>("VerticalPanningRoot");
				m_tpElementVerticalPanningRoot = spElementVerticalPanningRoot;
				spElementVerticalPanningThumb = GetTemplateChildHelper<FrameworkElement>("VerticalPanningThumb");
				m_tpElementVerticalPanningThumb = spElementVerticalPanningThumb;
			}

			// Attach the event handlers
			AttachEvents();

			// Updating states for parts where properties might have been updated
			// through XAML before the template was loaded.
			UpdateScrollBarVisibility();

			m_suspendVisualStateUpdates = false;
			ChangeVisualState(false);
		}

		private static void DetachEvents(object snd, RoutedEventArgs args) // OnUnloaded
			=> (snd as ScrollBar)?.DetachEvents();

		private void DetachEvents()
		{
			if (m_tpElementHorizontalThumb != null)
			{
				m_ElementHorizontalThumbDragStartedToken.Disposable = null;
				m_ElementHorizontalThumbDragDeltaToken.Disposable = null;
				m_ElementHorizontalThumbDragCompletedToken.Disposable = null;
			}

			if (m_tpElementHorizontalLargeDecrease != null)
			{
				m_ElementHorizontalLargeDecreaseClickToken.Disposable = null;
			}

			if (m_tpElementHorizontalLargeIncrease != null)
			{
				m_ElementHorizontalLargeIncreaseClickToken.Disposable = null;
			}

			if (m_tpElementHorizontalSmallDecrease != null)
			{
				m_ElementHorizontalSmallDecreaseClickToken.Disposable = null;
			}

			if (m_tpElementHorizontalSmallIncrease != null)
			{
				m_ElementHorizontalSmallIncreaseClickToken.Disposable = null;
			}

			if (m_tpElementVerticalThumb != null)
			{
				m_ElementVerticalThumbDragStartedToken.Disposable = null;
				m_ElementVerticalThumbDragDeltaToken.Disposable = null;
				m_ElementVerticalThumbDragCompletedToken.Disposable = null;
			}

			if (m_tpElementVerticalLargeDecrease != null)
			{
				m_ElementVerticalLargeDecreaseClickToken.Disposable = null;
			}

			if (m_tpElementVerticalLargeIncrease != null)
			{
				m_ElementVerticalLargeIncreaseClickToken.Disposable = null;
			}

			if (m_tpElementVerticalSmallDecrease != null)
			{
				m_ElementVerticalSmallDecreaseClickToken.Disposable = null;
			}

			if (m_tpElementVerticalSmallIncrease != null)
			{
				m_ElementVerticalSmallIncreaseClickToken.Disposable = null;
			}
		}

		private static void ReAttachEvents(object snd, RoutedEventArgs args) // OnLoaded
		{
			if (snd is ScrollBar sb)
			{
				sb.DetachEvents(); // Do not double listen events!
				sb.AttachEvents();
			}
		}

		private void AttachEvents()
		{
			if (m_tpElementHorizontalThumb != null || m_tpElementVerticalThumb != null)
			{
				if (m_tpElementHorizontalThumb != null)
				{
					m_tpElementHorizontalThumb.DragStarted += OnThumbDragStarted;
					m_ElementHorizontalThumbDragStartedToken.Disposable = Disposable.Create(() => m_tpElementHorizontalThumb.DragStarted -= OnThumbDragStarted);
					m_tpElementHorizontalThumb.DragDelta += OnThumbDragDelta;
					m_ElementHorizontalThumbDragDeltaToken.Disposable = Disposable.Create(() => m_tpElementHorizontalThumb.DragDelta -= OnThumbDragDelta);
					m_tpElementHorizontalThumb.DragCompleted += OnThumbDragCompleted;
					m_ElementHorizontalThumbDragCompletedToken.Disposable = Disposable.Create(() => m_tpElementHorizontalThumb.DragCompleted -= OnThumbDragCompleted);
					m_tpElementHorizontalThumb.IgnoreTouchInput = true;
				}

				if (m_tpElementVerticalThumb != null)
				{
					m_tpElementVerticalThumb.DragStarted += OnThumbDragStarted;
					m_ElementVerticalThumbDragStartedToken.Disposable = Disposable.Create(() => m_tpElementVerticalThumb.DragStarted -= OnThumbDragStarted);
					m_tpElementVerticalThumb.DragDelta += OnThumbDragDelta;
					m_ElementVerticalThumbDragDeltaToken.Disposable = Disposable.Create(() => m_tpElementVerticalThumb.DragDelta -= OnThumbDragDelta);
					m_tpElementVerticalThumb.DragCompleted += OnThumbDragCompleted;
					m_ElementVerticalThumbDragCompletedToken.Disposable = Disposable.Create(() => m_tpElementVerticalThumb.DragCompleted -= OnThumbDragCompleted);
					m_tpElementVerticalThumb.IgnoreTouchInput = true;
				}
			}

			if (m_tpElementHorizontalLargeDecrease != null || m_tpElementVerticalLargeDecrease != null)
			{
				if (m_tpElementHorizontalLargeDecrease != null)
				{
					m_tpElementHorizontalLargeDecrease.Click += LargeDecrement;
					m_ElementHorizontalLargeDecreaseClickToken.Disposable = Disposable.Create(() => m_tpElementHorizontalLargeDecrease.Click -= LargeDecrement);
					m_tpElementHorizontalLargeDecrease.IgnoreTouchInput = true;
				}

				if (m_tpElementVerticalLargeDecrease != null)
				{
					m_tpElementVerticalLargeDecrease.Click += LargeDecrement;
					m_ElementVerticalLargeDecreaseClickToken.Disposable = Disposable.Create(() => m_tpElementVerticalLargeDecrease.Click -= LargeDecrement);
					m_tpElementVerticalLargeDecrease.IgnoreTouchInput = true;
				}
			}

			if (m_tpElementHorizontalLargeIncrease != null || m_tpElementVerticalLargeIncrease != null)
			{
				if (m_tpElementHorizontalLargeIncrease != null)
				{
					m_tpElementHorizontalLargeIncrease.Click += LargeIncrement;
					m_ElementHorizontalLargeIncreaseClickToken.Disposable = Disposable.Create(() => m_tpElementHorizontalLargeIncrease.Click -= LargeIncrement);
					m_tpElementHorizontalLargeIncrease.IgnoreTouchInput = true;
				}

				if (m_tpElementVerticalLargeIncrease != null)
				{
					m_tpElementVerticalLargeIncrease.Click += LargeIncrement;
					m_ElementVerticalLargeIncreaseClickToken.Disposable = Disposable.Create(() => m_tpElementVerticalLargeIncrease.Click -= LargeIncrement);
					m_tpElementVerticalLargeIncrease.IgnoreTouchInput = true;
				}
			}

			if (m_tpElementHorizontalSmallDecrease != null || m_tpElementVerticalSmallDecrease != null)
			{
				if (m_tpElementHorizontalSmallDecrease != null)
				{
					m_tpElementHorizontalSmallDecrease.Click += SmallDecrement;
					m_ElementHorizontalSmallDecreaseClickToken.Disposable = Disposable.Create(() => m_tpElementHorizontalSmallDecrease.Click -= SmallDecrement);
					m_tpElementHorizontalSmallDecrease.IgnoreTouchInput = true;
				}

				if (m_tpElementVerticalSmallDecrease != null)
				{
					m_tpElementVerticalSmallDecrease.Click += SmallDecrement;
					m_ElementVerticalSmallDecreaseClickToken.Disposable = Disposable.Create(() => m_tpElementVerticalSmallDecrease.Click -= SmallDecrement);
					m_tpElementVerticalSmallDecrease.IgnoreTouchInput = true;
				}
			}

			if (m_tpElementHorizontalSmallIncrease != null || m_tpElementVerticalSmallIncrease != null)
			{
				if (m_tpElementHorizontalSmallIncrease != null)
				{
					m_tpElementHorizontalSmallIncrease.Click += SmallIncrement;
					m_ElementHorizontalSmallIncreaseClickToken.Disposable = Disposable.Create(() => m_tpElementHorizontalSmallIncrease.Click -= SmallIncrement);
					m_tpElementHorizontalSmallIncrease.IgnoreTouchInput = true;
				}

				if (m_tpElementVerticalSmallIncrease != null)
				{
					m_tpElementVerticalSmallIncrease.Click += SmallIncrement;
					m_ElementVerticalSmallIncreaseClickToken.Disposable = Disposable.Create(() => m_tpElementVerticalSmallIncrease.Click -= SmallIncrement);
					m_tpElementVerticalSmallIncrease.IgnoreTouchInput = true;
				}
			}
		}

		// Retrieves a reference to a child template object given its name
		private T GetTemplateChildHelper<T>(string childName) where T : class
			=> GetTemplateChild(childName) as T;

		// IsEnabled property changed handler.
		private protected override void OnIsEnabledChanged(IsEnabledChangedEventArgs e)
		{
			base.OnIsEnabledChanged(e);

			if (!e.NewValue)
			{
				m_isPointerOver = false;
			}

			UpdateVisualState();
		}

		// PointerEnter event handler.
		protected override void OnPointerEntered(PointerRoutedEventArgs pArgs)
		{
			m_isPointerOver = true;

			if (!IsDragging)
			{
				UpdateVisualState();
			}
		}

		// PointerExited event handler.
		protected override void OnPointerExited(PointerRoutedEventArgs pArgs)
		{
			m_isPointerOver = false;

			if (!IsDragging)
			{
				UpdateVisualState();
			}
		}

		// PointerPressed event handler.
		protected override void OnPointerPressed(PointerRoutedEventArgs pArgs)
		{
			PointerPoint spPointerPoint;
			PointerPointProperties spPointerProperties;
			var handled = pArgs.Handled;

			spPointerPoint = pArgs.GetCurrentPoint(this);

			spPointerProperties = spPointerPoint.Properties;
			var bIsLeftButtonPressed = spPointerProperties.IsLeftButtonPressed;
			if (bIsLeftButtonPressed)
			{
				Pointer spPointer;
				if (!handled)
				{
					pArgs.Handled = true;
					spPointer = pArgs.Pointer;
					var captured = CapturePointer(spPointer);
				}
			}
		}

		// PointerReleased event handler.
		protected override void OnPointerReleased(PointerRoutedEventArgs pArgs)
		{
			var handled = pArgs.Handled;
			if (!handled)
			{
				pArgs.Handled = true;
			}
		}

		/// PointerCaptureLost event handler.
		protected override void OnPointerCaptureLost(PointerRoutedEventArgs pArgs)
		{
			UpdateVisualState(true);
		}

		protected override void OnDoubleTapped(DoubleTappedRoutedEventArgs pArgs)
		{
			pArgs.Handled = true;
		}

		protected override void OnTapped(TappedRoutedEventArgs pArgs)
		{
			pArgs.Handled = true;
		}


		// Create ScrollBarAutomationPeer to represent the
		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new ScrollBarAutomationPeer(this);
		}

		// Change to the correct visual state for the button.
		private protected override void ChangeVisualState(
			// true to use transitions when updating the visual state, false
			// to snap directly to the new visual state.
			bool bUseTransitions)
		{
			if (m_suspendVisualStateUpdates)
			{
				return;
			}

			var prefix = IsFixedOrientation ? $"{Orientation}_" : "";

			var scrollingIndicator = IndicatorMode;
			var isEnabled = IsEnabled;
			bool isSuccessful;
			if (!isEnabled)
			{
				VisualStateManager.GoToState(this, prefix + "Disabled", bUseTransitions);
			}
			else if (m_isPointerOver)
			{
				isSuccessful = VisualStateManager.GoToState(this, prefix + "PointerOver", bUseTransitions);
				//Default to Normal if PointerOver state isn't available.
				if (!isSuccessful)
				{
					VisualStateManager.GoToState(this, prefix + "Normal", bUseTransitions);
				}
			}
			else
			{
				VisualStateManager.GoToState(this, prefix + "Normal", bUseTransitions);
			}

			if (!m_blockIndicators && (!IsConscious() || scrollingIndicator == ScrollingIndicatorMode.MouseIndicator))
			{
				VisualStateManager.GoToState(this, prefix + "MouseIndicator", bUseTransitions);
			}
			else if (!m_blockIndicators && scrollingIndicator == ScrollingIndicatorMode.TouchIndicator)
			{
				isSuccessful = VisualStateManager.GoToState(this, prefix + "TouchIndicator", bUseTransitions);
				//Default to MouseActiveState if Panning state isn't available.
				if (!isSuccessful)
				{
					VisualStateManager.GoToState(this, prefix + "MouseIndicator", bUseTransitions);
				}
			}
			else
			{
				VisualStateManager.GoToState(this, prefix + "NoIndicator", bUseTransitions);
			}

			// Expanded/Collapsed States were added in RS3 and ExpandedWithoutAnimation/CollapsedWithoutAnimation states
			// were added in RS4. Since Expanded can exist without ExpandedWithoutAnimation (and the same for collapsed)
			// each time we try to transition  to a *WithoutAnimation state we need to check to make sure the transition was
			// successful. If it was not we fallback to the appropriate expanded or collapsed state.
			// No Quirks are required since these are new states and if the states are not present then they are no-op.
			// UseTransitions is always true since the delay behavior is defined in the transitions when
			// animations are enabled. When animations are disabled, the framework does not run transitions.
			if (!IsConscious())
			{
				VisualStateManager.GoToState(this, prefix + (isEnabled ? "Expanded" : "Collapsed"), true /* useTransitions */);
			}
			else
			{
				isSuccessful = false;
				var animate = IsAnimationEnabled;
				if (isEnabled && m_isPointerOver)
				{
					if (!animate)
					{
						isSuccessful = VisualStateManager.GoToState(this, prefix + "ExpandedWithoutAnimation", true /* useTransitions */);
					}
					if (!isSuccessful)
					{
						VisualStateManager.GoToState(this, prefix + "Expanded", true /* useTransitions */);
					}
				}
				else
				{
					if (!animate)
					{
						isSuccessful = VisualStateManager.GoToState(this, prefix + "CollapsedWithoutAnimation", true /* useTransitions */);
					}
					if (!isSuccessful)
					{
						VisualStateManager.GoToState(this, prefix + "Collapsed", true /* useTransitions */);
					}
				}
			}
		}

		// Returns the actual length of the ScrollBar in the direction of its orientation.
		double GetTrackLength()
		{
			var orientation = Orientation;
			double length;
			if (orientation == Orientation.Horizontal)
			{
				length = ActualWidth;
			}
			else
			{
				length = ActualHeight;
			}

			//
			// Set the track length as zero which is collapsed state if the length is greater than
			// the current viewport size in case of the layout is dirty with using actual size(Width/Height)
			// as the extent. The invalid track length setting will cause of updating the new layout size
			// on the ScrollViewer and ScrollContentPresenter that will keep to ask ScrollBar for updating
			// the track length continuously which is a layout cycle crash.
			//
			if (m_isUsingActualSizeAsExtent
				&& (IsMeasureDirty || IsArrangeDirty)
			)
			{
				var viewport = ViewportSize;

				// Return the length as zero because of current length is greater than
				// the viewport that is a layout cycle issue. The valid length and
				// viewport will be updated after complete layout updating.
				if (!double.IsNaN(viewport) && !double.IsNaN(length) && length != 0 && length > viewport)
				{
					return 0.0f;
				}
			}

			// Added to consider the case where everything is collapsed.
			return double.IsNaN(length) ? 0.0f : length;
		}

		// Returns the combined actual length in the direction of its orientation of the ScrollBar's RepeatButtons.
		private double GetRepeatButtonsLength()
		{
			double length = 0;
			Thickness increaseMargin;
			Thickness decreaseMargin;
			var orientation = Orientation;
			double smallLength;
			if (orientation == Orientation.Horizontal)
			{
				if (m_tpElementHorizontalSmallDecrease != null)
				{
					smallLength = m_tpElementHorizontalSmallDecrease.ActualWidth;
					decreaseMargin = m_tpElementHorizontalSmallDecrease.Margin;
					length = smallLength + decreaseMargin.Left + decreaseMargin.Right;
				}
				if (m_tpElementHorizontalSmallIncrease != null)
				{
					smallLength = m_tpElementHorizontalSmallIncrease.ActualWidth;
					increaseMargin = m_tpElementHorizontalSmallIncrease.Margin;
					length += smallLength + increaseMargin.Left + increaseMargin.Right;
				}
			}
			else
			{
				if (m_tpElementVerticalSmallDecrease != null)
				{
					smallLength = m_tpElementVerticalSmallDecrease.ActualHeight;
					decreaseMargin = m_tpElementVerticalSmallDecrease.Margin;
					length = smallLength + decreaseMargin.Top + decreaseMargin.Bottom;
				}
				if (m_tpElementVerticalSmallIncrease != null)
				{
					smallLength = m_tpElementVerticalSmallIncrease.ActualHeight;
					increaseMargin = m_tpElementVerticalSmallIncrease.Margin;
					length += smallLength + increaseMargin.Top + increaseMargin.Bottom;
				}
			}

			return length;
		}

		protected override void OnValueChanged(double oldValue, double newValue)
		{
			UpdateTrackLayout();
			base.OnValueChanged(oldValue, newValue);
		}

		// Called when the Minimum value changed.
		protected override void OnMinimumChanged(
				double oldMinimum,
				double newMinimum)
		{
			UpdateTrackLayout();
		}

		// Called when the Maximum value changed.
		protected override void OnMaximumChanged(
				double oldMaximum,
				double newMaximum)
		{
			UpdateTrackLayout();
		}

		// Gets a value indicating whether the ScrollBar is currently dragging.
		private bool IsDragging
		{
			get
			{
				var orientation = Orientation;
				if (orientation == Orientation.Horizontal && m_tpElementHorizontalThumb != null)
				{
					return m_tpElementHorizontalThumb.IsDragging;
				}
				else if (orientation == Orientation.Vertical && m_tpElementVerticalThumb != null)
				{
					return m_tpElementVerticalThumb.IsDragging;
				}
				else
				{
					return false;
				}
			}
		}

		// Value indicating whether the ScrollBar reacts to user input or not.
		internal bool IsIgnoringUserInput
		{
			get => m_isIgnoringUserInput;
			set => m_isIgnoringUserInput = value;
		}

		// Called whenever the Thumb drag operation is started.
		private void OnThumbDragStarted(
			object pSender,
			DragStartedEventArgs pArgs)
		{
			m_dragValue = Value;
		}

		// Whenever the thumb gets dragged, we handle the event through this function to
		// update the current value depending upon the thumb drag delta.
		private void OnThumbDragDelta(
			object pSender,
			DragDeltaEventArgs pArgs)
		{
			double offset = 0.0;
			double zoom = 1.0;
			var maximum = Maximum;
			var minimum = Minimum;
			var orientation = Orientation;
			double trackLength;
			double repeatButtonsLength;
			double thumbSize;
			double change;
			if (orientation == Orientation.Horizontal &&
				m_tpElementHorizontalThumb != null)
			{
				change = pArgs.HorizontalChange;
				trackLength = GetTrackLength();
				repeatButtonsLength = GetRepeatButtonsLength();
				trackLength -= repeatButtonsLength;
				thumbSize = m_tpElementHorizontalThumb.ActualWidth;

				offset = zoom * change / (trackLength - thumbSize) * (maximum - minimum);
			}
			else if (orientation == Orientation.Vertical &&
				m_tpElementVerticalThumb != null)
			{
				change = pArgs.VerticalChange;
				trackLength = GetTrackLength();
				repeatButtonsLength = GetRepeatButtonsLength();
				trackLength -= repeatButtonsLength;
				thumbSize = m_tpElementVerticalThumb.ActualHeight;

				offset = zoom * change / (trackLength - thumbSize) * (maximum - minimum);
			}

			if (!double.IsNaN(offset) &&
				!double.IsInfinity(offset))
			{
				m_dragValue += offset;
				var newValue = Math.Min(maximum, Math.Max(minimum, m_dragValue));
				var value = Value;
				if (newValue != value)
				{
					Value = newValue;
					RaiseScrollEvent(ScrollEventType.ThumbTrack);
				}
			}
		}

		// Raise the Scroll event when teh Thumb drag is completed.
		private void OnThumbDragCompleted(
			object pSender,
			DragCompletedEventArgs pArgs)
		{
			RaiseScrollEvent(ScrollEventType.EndScroll);
		}

		// Handle the SizeChanged event.
		private static void OnSizeChanged(
			object pSender,
			SizeChangedEventArgs pArgs)
		{
			(pSender as ScrollBar)?.UpdateTrackLayout();
		}

		private static void OnLayoutUpdated(
			object pSender,
			object pArgs)
		{
			(pSender as ScrollBar)?.UpdateTrackLayout();
		}

		// Called whenever the SmallDecrement button is clicked.
		void SmallDecrement(
			object pSender,
			RoutedEventArgs pArgs)
		{
			var value = Value;
			var change = SmallChange;
			var edge = Minimum;
			var newValue = Math.Max(value - change, edge);
			if (newValue != value)
			{
				Value = newValue;
				RaiseScrollEvent(ScrollEventType.SmallDecrement);
			}
		}

		// Called whenever the SmallIncrement button is clicked.
		void SmallIncrement(
			object pSender,
			RoutedEventArgs pArgs)
		{
			var value = Value;
			var change = SmallChange;
			var edge = Maximum;
			var newValue = Math.Min(value + change, edge);
			if (newValue != value)
			{
				Value = newValue;
				RaiseScrollEvent(ScrollEventType.SmallIncrement);
			}
		}

		// Called whenever the LargeDecrement button is clicked.
		void LargeDecrement(
			object pSender,
			RoutedEventArgs pArgs)
		{
			var value = Value;
			var change = LargeChange;
			var edge = Minimum;
			var newValue = Math.Max(value - change, edge);
			if (newValue != value)
			{
				Value = newValue;
				RaiseScrollEvent(ScrollEventType.LargeDecrement);
			}
		}

		// Called whenever the LargeIncrement button is clicked.
		void LargeIncrement(
			object pSender,
			RoutedEventArgs pArgs)
		{
			var value = Value;
			var change = LargeChange;
			var edge = Maximum;
			var newValue = Math.Min(value + change, edge);
			if (newValue != value)
			{
				Value = newValue;
				RaiseScrollEvent(ScrollEventType.LargeIncrement);
			}
		}

		// This raises the Scroll event, passing in the scrollEventType as a parameter
		// to let the handler know what triggered this event.
		void RaiseScrollEvent(
			ScrollEventType scrollEventType)
		{
			ScrollEventArgs spArgs;

			// TODO: Add tracing for small change events

			// Create the args
			spArgs = new ScrollEventArgs();
			spArgs.ScrollEventType = scrollEventType;
			spArgs.NewValue = Value;
			spArgs.OriginalSource = this;

			// Raise the event
			Scroll?.Invoke(this, spArgs);
		}

		// Change the template being used to display this control when the orientation
		// changes.
		void OnOrientationChanged()
		{
			Orientation orientation = Orientation;

			//Set Visible and collapsed based on orientation.
			if (m_tpElementVerticalTemplate != null)
			{
				m_tpElementVerticalTemplate.Visibility =
					orientation == Orientation.Horizontal ?
						Visibility.Collapsed :
						Visibility.Visible;
			}

			if (m_tpElementVerticalPanningRoot != null)
			{
				m_tpElementVerticalPanningRoot.Visibility =
					orientation == Orientation.Horizontal ?
						Visibility.Collapsed :
						Visibility.Visible;
			}

			if (m_tpElementHorizontalTemplate != null)
			{
				m_tpElementHorizontalTemplate.Visibility =
					orientation == Orientation.Horizontal ?
						Visibility.Visible :
						Visibility.Collapsed;
			}

			if (m_tpElementHorizontalPanningRoot != null)
			{
				m_tpElementHorizontalPanningRoot.Visibility =
					orientation == Orientation.Horizontal ?
						Visibility.Visible :
						Visibility.Collapsed;
			}

			UpdateTrackLayout();
		}

		// Update track based on panning or mouse activity
		void RefreshTrackLayout()
		{
			UpdateTrackLayout();
			ChangeVisualState(true);
		}

		//Update scrollbar visibility based on what input device is active and the orientation
		//of the
		void UpdateScrollBarVisibility()
		{
			OnOrientationChanged();
			RefreshTrackLayout();
		}


		// This method will take the current min, max, and value to
		// calculate and layout the current control measurements.
		void UpdateTrackLayout()
		{
			Thickness newMargin;

			var maximum = Maximum;
			var minimum = Minimum;
			var value = Value;
			var orientation = Orientation;
			var trackLength = GetTrackLength();
			double mouseIndicatorLength;
			double touchIndicatorLength;
			UpdateIndicatorLengths(trackLength, out mouseIndicatorLength, out touchIndicatorLength);
			double difference = maximum - minimum;
			double multiplier;
			//Check to make sure that its not dividing by zero.
			if (difference == 0.0)
			{
				multiplier = 0.0;
			}
			else
			{
				multiplier = (value - minimum) / difference;
			}

			var repeatButtonsLength = GetRepeatButtonsLength();
			double largeDecreaseNewSize = Math.Max(0.0, multiplier * (trackLength - repeatButtonsLength - mouseIndicatorLength));
			double indicatorOffset = Math.Max(0.0, multiplier * (trackLength - touchIndicatorLength));

			if (orientation == Orientation.Horizontal &&
				m_tpElementHorizontalLargeDecrease != null &&
				m_tpElementHorizontalThumb != null)
			{
				m_tpElementHorizontalLargeDecrease.Width = largeDecreaseNewSize;
			}
			else if (orientation == Orientation.Vertical &&
				m_tpElementVerticalLargeDecrease != null &&
				m_tpElementVerticalThumb != null)
			{
				m_tpElementVerticalLargeDecrease.Height = largeDecreaseNewSize;
			}

			if (orientation == Orientation.Horizontal &&
				m_tpElementHorizontalPanningRoot != null)
			{
				newMargin = m_tpElementHorizontalPanningRoot.Margin;
				newMargin.Left = indicatorOffset;
				m_tpElementHorizontalPanningRoot.Margin = newMargin;
			}
			else if (orientation == Orientation.Vertical &&
				m_tpElementVerticalPanningRoot != null)
			{
				newMargin = m_tpElementVerticalPanningRoot.Margin;
				newMargin.Top = indicatorOffset;
				m_tpElementVerticalPanningRoot.Margin = newMargin;
			}
		}

		// Based on the ViewportSize, the Track's length, and the Minimum and Maximum
		// values, we will calculate the length of the Thumb.
		private void ConvertViewportSizeToDisplayUnits(
			double trackLength,
		out double pThumbSize)
		{
			var maximum = Maximum;
			var minimum = Minimum;
			var viewport = ViewportSize;

			// We need to round to the nearest whole number.
			// In the case where pThumbSize is calculated to have a fractional part of exactly .5,
			// then in UpdateTrackLayout() where we calculate the largeDecreaseNewSize we end up giving
			// largeDecreaseNewSize a size of 0.5 as well, and at this point the grid laying out
			// the mouse portion of the ScrollBar template nudges the increase repeat button 1 px.
			pThumbSize = Math.Round(trackLength * viewport / Math.Max(1, viewport + maximum - minimum), 0);
		}

		// This will resize the Thumb, based on calculations with the
		// ViewportSize, the Track's length, and the Minimum and Maximum
		// values.
		void UpdateIndicatorLengths(
			double trackLength,
		out double pMouseIndicatorLength,
		out double pTouchIndicatorLength)
		{
			double result = double.NaN;
			bool hideThumb = trackLength <= 0.0;
			bool mouseIndicatorLengthWasSet = false;
			bool touchIndicatorLengthWasSet = false;

			pMouseIndicatorLength = 0.0;
			pTouchIndicatorLength = 0.0;

			// Uno workaround: If the scrollbar is smaller than the min size of the thumb,
			//		we will try to set the visibility collapsed and then request to 'HideThumb',
			//		which will drive uno to fall in an infinite layout cycle.
			//		We instead cache the value and apply it only at the end.
			double? m_tpElementHorizontalThumbWidth = default, m_tpElementVerticalThumbHeight = default, m_tpElementHorizontalPanningThumbWidth = default, m_tpElementVerticalPanningThumbHeight = default;
			Visibility? m_tpElementHorizontalThumbVisibility = default, m_tpElementVerticalThumbVisibility = default, m_tpElementHorizontalPanningThumbVisibility = default, m_tpElementVerticalPanningThumbVisibility = default;

			if (!hideThumb)
			{
				double minSize = 0.0;
				var orientation = Orientation;
				var maximum = Maximum;
				var minimum = Minimum;
				var repeatButtonsLength = GetRepeatButtonsLength();
				var trackLengthMinusRepeatButtonsLength = trackLength - repeatButtonsLength;
				double indicatorSize;
				ConvertViewportSizeToDisplayUnits(trackLengthMinusRepeatButtonsLength, out indicatorSize);

				double actualSize;
				double actualSizeMinusRepeatButtonsLength;

				if (orientation == Orientation.Horizontal &&
					m_tpElementHorizontalThumb != null)
				{
					if (maximum - minimum != 0)
					{
						minSize = m_tpElementHorizontalThumb.MinWidth;
						result = Math.Max(minSize, indicatorSize);
					}

					// Hide the thumb if too big
					actualSize = ActualWidth;
					actualSizeMinusRepeatButtonsLength = actualSize - repeatButtonsLength;
					if (maximum - minimum == 0 || result > actualSizeMinusRepeatButtonsLength || trackLengthMinusRepeatButtonsLength <= minSize)
					{
						hideThumb = true;
					}
					else
					{
						m_tpElementHorizontalThumbVisibility = Visibility.Visible;
						m_tpElementHorizontalThumbWidth = result;
						mouseIndicatorLengthWasSet = true;
					}
				}
				else if (orientation == Orientation.Vertical &&
					m_tpElementVerticalThumb != null)
				{
					if (maximum - minimum != 0)
					{
						minSize = m_tpElementVerticalThumb.MinHeight;
						result = Math.Max(minSize, indicatorSize);
					}

					// Hide the thumb if too big
					actualSize = ActualHeight;
					actualSizeMinusRepeatButtonsLength = actualSize - repeatButtonsLength;
					if (maximum - minimum == 0 || result > actualSizeMinusRepeatButtonsLength || trackLengthMinusRepeatButtonsLength <= minSize)
					{
						hideThumb = true;
					}
					else
					{
						m_tpElementVerticalThumbVisibility = Visibility.Visible;
						m_tpElementVerticalThumbHeight = result;
						mouseIndicatorLengthWasSet = true;
					}
				}

				if (mouseIndicatorLengthWasSet)
				{
					//added to consider the case where everything is collapsed.
					pMouseIndicatorLength = double.IsNaN(result) ? 0.0f : result;
				}


				ConvertViewportSizeToDisplayUnits(trackLength, out indicatorSize);

				//Do the same for horizontal panning indicator.
				if (orientation == Orientation.Horizontal &&
					m_tpElementHorizontalPanningThumb != null)
				{
					if (maximum - minimum != 0)
					{
						minSize = m_tpElementHorizontalPanningThumb.MinWidth;
						result = Math.Max(minSize, indicatorSize);
					}

					// Hide the thumb if too big
					actualSize = ActualWidth;
					if (maximum - minimum == 0 || result > actualSize || trackLength <= minSize)
					{
						hideThumb = true;
					}
					else
					{
						m_tpElementHorizontalPanningThumbVisibility = Visibility.Visible;
						m_tpElementHorizontalPanningThumbWidth = result;
						touchIndicatorLengthWasSet = true;
					}
				}
				else if (orientation == Orientation.Vertical &&
					m_tpElementVerticalPanningThumb != null)
				{
					if (maximum - minimum != 0)
					{
						minSize = m_tpElementVerticalPanningThumb.MinHeight;
						result = Math.Max(minSize, indicatorSize);
					}

					// Hide the thumb if too big
					actualSize = ActualHeight;
					if (maximum - minimum == 0 || result > actualSize || trackLength <= minSize)
					{
						hideThumb = true;
					}
					else
					{
						m_tpElementVerticalPanningThumbVisibility = Visibility.Visible;
						m_tpElementVerticalPanningThumbHeight = result;
						touchIndicatorLengthWasSet = true;
					}
				}

				if (touchIndicatorLengthWasSet)
				{
					//added to consider the case where everything is collapsed.
					pTouchIndicatorLength = double.IsNaN(result) ? 0.0f : result;
				}
			}

			if (hideThumb)
			{
				if (m_tpElementHorizontalThumb != null)
				{
					m_tpElementHorizontalThumb.Visibility = Visibility.Collapsed;
				}
				if (m_tpElementVerticalThumb != null)
				{
					m_tpElementVerticalThumb.Visibility = Visibility.Collapsed;
				}
				if (m_tpElementHorizontalPanningThumb != null)
				{
					m_tpElementHorizontalPanningThumb.Visibility = Visibility.Collapsed;
				}
				if (m_tpElementVerticalPanningThumb != null)
				{
					m_tpElementVerticalPanningThumb.Visibility = Visibility.Collapsed;
				}
			}
			else
			{
				if (m_tpElementHorizontalThumbWidth.HasValue) m_tpElementHorizontalThumb.Width = m_tpElementHorizontalThumbWidth.Value;
				if (m_tpElementVerticalThumbHeight.HasValue) m_tpElementVerticalThumb.Height = m_tpElementVerticalThumbHeight.Value;
				if (m_tpElementHorizontalPanningThumbWidth.HasValue) m_tpElementHorizontalPanningThumb.Width = m_tpElementHorizontalPanningThumbWidth.Value;
				if (m_tpElementVerticalPanningThumbHeight.HasValue) m_tpElementVerticalPanningThumb.Height = m_tpElementVerticalPanningThumbHeight.Value;
				if (m_tpElementHorizontalThumbVisibility.HasValue) m_tpElementHorizontalThumb.Visibility = m_tpElementHorizontalThumbVisibility.Value;
				if (m_tpElementVerticalThumbVisibility.HasValue) m_tpElementVerticalThumb.Visibility = m_tpElementVerticalThumbVisibility.Value;
				if (m_tpElementHorizontalPanningThumbVisibility.HasValue) m_tpElementHorizontalPanningThumb.Visibility = m_tpElementHorizontalPanningThumbVisibility.Value;
				if (m_tpElementVerticalPanningThumbVisibility.HasValue) m_tpElementVerticalPanningThumb.Visibility = m_tpElementVerticalPanningThumbVisibility.Value;
			}
		}

#if false
		// during a SemanticZoomOperation we want to be able to block the scrollbar
		// without stomping over the user value
		void BlockIndicatorFromShowing()
		{
			if (!m_blockIndicators)
			{
				m_blockIndicators = true;
				ChangeVisualState(false);
			}
		}

		void ResetBlockIndicatorFromShowing()
		{
			m_blockIndicators = false;

			// Don't change state; stay in NoIndicator. The next ScrollViewer.ShowIndicators()
			// call will drive our next GoToState() call, with transitions.
		}

		void AdjustDragValue(double delta)
		{

			// If somebody is calling this when not dragging, are they confused?
			var dragging = IsDragging;

			m_dragValue += delta;
		}
#endif
	}
}
