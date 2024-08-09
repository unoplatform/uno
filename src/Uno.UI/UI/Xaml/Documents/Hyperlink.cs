using System;
using System.Collections.Generic;
using System.Text;
using DirectUI;
using Uno.UI;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;
using Uno.UI.Xaml.Media;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.Text;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Uno.Extensions;

namespace Windows.UI.Xaml.Documents
{
	public sealed partial class Hyperlink : Span
	{
#if !__WASM__
		private readonly IFocusable _focusableHelper;
#endif

		private const string HyperlinkForegroundPressedKey = "HyperlinkForegroundPressed";
		private const string HyperlinkForegroundPointerOverKey = "HyperlinkForegroundPointerOver";
		private protected override Brush DefaultTextForegroundBrush => DefaultBrushes.HyperlinkForegroundBrush;

		public
#if __WASM__
			new
#endif
			bool Focus(FocusState value)
		{
			//If the App tries to call Focus with an Unfocused state, throw:
			if (FocusState.Unfocused == value)
			{
				throw new ArgumentOutOfRangeException(nameof(value), "Focus method does not allow FocusState.Unfocused");
			}

			var valueNative = value;

			Hyperlink coreHyperlink = this;
			if (coreHyperlink == null)
			{
				// Focus may be called on a disconnected element (when the framework
				// peer has been disassociated from its core peer).  If the core peer
				// has already been disassociated, return 'unfocusable'.
				return false;
			}

			DependencyObject spFocusTarget = null;

			var pFocusManager = VisualTree.GetFocusManagerForElement(this);
			if (pFocusManager == null)
			{
				return false;
			}

			if (coreHyperlink.IsFocusable())
			{
				spFocusTarget = coreHyperlink;
			}

			var result = pFocusManager.SetFocusedElement(
				new FocusMovement(
					spFocusTarget,
					FocusNavigationDirection.None,
					valueNative));
			return result.WasMoved;
		}

		public event TypedEventHandler<Hyperlink, HyperlinkClickEventArgs> Click;

		public
#if __WASM__
			new
#endif
			event RoutedEventHandler GotFocus;

		internal void OnGotFocus(RoutedEventArgs args) => GotFocus?.Invoke(this, args);

		public
#if __WASM__
			new
#endif
			event RoutedEventHandler LostFocus;

		internal void OnLostFocus(RoutedEventArgs args) => LostFocus?.Invoke(this, args);

#if !__WASM__
		public Hyperlink()
		{
			OnUnderlineStyleChanged();
			_focusableHelper = new FocusableHelper(this);
		}
#endif

		#region NavigateUri

		public Uri NavigateUri
		{
			get => (Uri)this.GetValue(NavigateUriProperty);
			set => this.SetValue(NavigateUriProperty, value);
		}

		public static DependencyProperty NavigateUriProperty { get; } =
			DependencyProperty.Register(
				"NavigateUri",
				typeof(Uri),
				typeof(Hyperlink),
				new FrameworkPropertyMetadata(
					defaultValue: default(Uri),
					options: FrameworkPropertyMetadataOptions.Inherits,
					propertyChangedCallback: (s, e) => ((Hyperlink)s).OnNavigateUriChangedPartial((Uri)e.NewValue)
				)
			);
		partial void OnNavigateUriChangedPartial(Uri newNavigateUri);

		#endregion

		#region UnderlineStyle

		public UnderlineStyle UnderlineStyle
		{
			get => (UnderlineStyle)this.GetValue(UnderlineStyleProperty);
			set => this.SetValue(UnderlineStyleProperty, value);
		}

		internal static DependencyProperty UnderlineStyleProperty { get; } =
			DependencyProperty.Register(
				"UnderlineStyle",
				typeof(UnderlineStyle),
				typeof(Hyperlink),
				new FrameworkPropertyMetadata(
					defaultValue: UnderlineStyle.Single,
					options: FrameworkPropertyMetadataOptions.Inherits,
					propertyChangedCallback: (s, e) => ((Hyperlink)s).OnUnderlineStyleChanged()
				)
			);

		private void OnUnderlineStyleChanged()
		{
			TextDecorations = UnderlineStyle == UnderlineStyle.Single
				? TextDecorations.Underline
				: TextDecorations.None;
		}

		#endregion

		#region Hover
		private Pointer _hoveredPointer;
		internal void SetPointerOver(Pointer pointer)
		{
			_hoveredPointer = pointer;
			SetCurrentForeground();
		}

		internal bool ReleasePointerOver(Pointer pointer)
		{
			if (_hoveredPointer?.Equals(pointer) ?? false)
			{
				_hoveredPointer = null;
				SetCurrentForeground();
				return true;
			}
			else
			{
				return false;
			}
		}
		#endregion

		#region Click
		private Pointer _pressedPointer;
		internal void SetPointerPressed(Pointer pointer)
		{
			_pressedPointer = pointer;
			SetCurrentForeground();
		}

		internal bool ReleasePointerPressed(Pointer pointer)
		{
			if (_pressedPointer?.Equals(pointer) ?? false)
			{
				OnClick();

				_pressedPointer = null;
				SetCurrentForeground();
				return true;
			}
			else
			{
				return false;
			}
		}

		internal bool AbortPointerPressed(Pointer pointer)
		{
			if (_pressedPointer?.Equals(pointer) ?? false)
			{
				_pressedPointer = null;
				SetCurrentForeground();
				return true;
			}
			else
			{
				return false;
			}
		}

		internal void AbortAllPointerState()
		{
			_pressedPointer = null;
			_hoveredPointer = null;
			SetCurrentForeground();
		}

		internal void OnClick()
		{
			Click?.Invoke(this, new HyperlinkClickEventArgs { OriginalSource = this });

#if !__WASM__  // handled natively in WASM/Html
			if (NavigateUri != null)
			{
				_ = Launcher.LaunchUriAsync(NavigateUri);
			}
#endif
		}
		#endregion

		private void SetCurrentForeground()
		{
			if (_pressedPointer is { }
				&& Application.Current.Resources.TryGetValue(HyperlinkForegroundPressedKey, out var pressedBrush))
			{
				this.SetValue(ForegroundProperty, pressedBrush, DependencyPropertyValuePrecedences.Animations);
			}
			else if (_hoveredPointer is { }
				&& Application.Current.Resources.TryGetValue(HyperlinkForegroundPointerOverKey, out var hoveredBrush))
			{
				this.SetValue(ForegroundProperty, hoveredBrush, DependencyPropertyValuePrecedences.Animations);
			}
			else
			{
				this.ClearValue(ForegroundProperty, DependencyPropertyValuePrecedences.Animations);
			}
		}

#if !__WASM__
		public FocusState FocusState
		{
			get => GetFocusStateValue();
			set => SetFocusStateValue(value);
		}

		[GeneratedDependencyProperty(DefaultValue = default(FocusState))]
		public static DependencyProperty FocusStateProperty { get; } = CreateFocusStateProperty();

		public bool IsTabStop
		{
			get { return (bool)GetValue(IsTabStopProperty); }
			set { SetValue(IsTabStopProperty, value); }
		}

		public static DependencyProperty IsTabStopProperty { get; } =
			DependencyProperty.Register(
				nameof(IsTabStop),
				typeof(bool),
				typeof(Hyperlink),
				new FrameworkPropertyMetadata(defaultValue: (bool)true)
			);

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

		public XYFocusNavigationStrategy XYFocusDownNavigationStrategy
		{
			get => GetXYFocusDownNavigationStrategyValue();
			set => SetXYFocusDownNavigationStrategyValue(value);
		}

		[GeneratedDependencyProperty(DefaultValue = default(XYFocusNavigationStrategy))]
		public static DependencyProperty XYFocusDownNavigationStrategyProperty { get; } = CreateXYFocusDownNavigationStrategyProperty();

		public XYFocusNavigationStrategy XYFocusLeftNavigationStrategy
		{
			get => GetXYFocusLeftNavigationStrategyValue();
			set => SetXYFocusLeftNavigationStrategyValue(value);
		}

		[GeneratedDependencyProperty(DefaultValue = default(XYFocusNavigationStrategy))]
		public static DependencyProperty XYFocusLeftNavigationStrategyProperty { get; } = CreateXYFocusLeftNavigationStrategyProperty();

		public XYFocusNavigationStrategy XYFocusRightNavigationStrategy
		{
			get => GetXYFocusRightNavigationStrategyValue();
			set => SetXYFocusRightNavigationStrategyValue(value);
		}

		[GeneratedDependencyProperty(DefaultValue = default(XYFocusNavigationStrategy))]
		public static DependencyProperty XYFocusRightNavigationStrategyProperty { get; } = CreateXYFocusRightNavigationStrategyProperty();

		public XYFocusNavigationStrategy XYFocusUpNavigationStrategy
		{
			get => GetXYFocusUpNavigationStrategyValue();
			set => SetXYFocusUpNavigationStrategyValue(value);
		}

		[GeneratedDependencyProperty(DefaultValue = default(XYFocusNavigationStrategy))]
		public static DependencyProperty XYFocusUpNavigationStrategyProperty { get; } = CreateXYFocusUpNavigationStrategyProperty();
#else
		// The properties below have moved to UIElement in WinUI, but Hyperlink does not inherit from UIElement and does in Wasm.
		// This makes the properties move down incorrectly.
		// This section places those properties at the same location as the reference implementation.
		public new FocusState FocusState
		{
			get => base.FocusState;
			set => base.FocusState = value;
		}

		public static new DependencyProperty FocusStateProperty { get; } = UIElement.FocusStateProperty;

		public new bool IsTabStop
		{
			get => base.IsTabStop;
			set => base.IsTabStop = value;
		}

		public static new DependencyProperty IsTabStopProperty { get; } = UIElement.IsTabStopProperty;

		public new int TabIndex
		{
			get => base.TabIndex;
			set => base.TabIndex = value;
		}

		public new static DependencyProperty TabIndexProperty { get; } = UIElement.TabIndexProperty;

		public new DependencyObject XYFocusUp
		{
			get => base.XYFocusUp;
			set => base.XYFocusUp = value;
		}

		public new static DependencyProperty XYFocusUpProperty { get; } = UIElement.XYFocusUpProperty;

		public new DependencyObject XYFocusDown
		{
			get => base.XYFocusDown;
			set => base.XYFocusDown = value;
		}

		public new static DependencyProperty XYFocusDownProperty { get; } = UIElement.XYFocusDownProperty;

		public new DependencyObject XYFocusLeft
		{
			get => base.XYFocusLeft;
			set => base.XYFocusLeft = value;
		}

		public new static DependencyProperty XYFocusLeftProperty { get; } = UIElement.XYFocusLeftProperty;

		public new DependencyObject XYFocusRight
		{
			get => base.XYFocusRight;
			set => base.XYFocusRight = value;
		}

		public new static DependencyProperty XYFocusRightProperty { get; } = UIElement.XYFocusRightProperty;

		public new XYFocusNavigationStrategy XYFocusDownNavigationStrategy
		{
			get => base.XYFocusDownNavigationStrategy;
			set => base.XYFocusDownNavigationStrategy = value;
		}

		public new static DependencyProperty XYFocusDownNavigationStrategyProperty { get; } = UIElement.XYFocusDownNavigationStrategyProperty;

		public new XYFocusNavigationStrategy XYFocusLeftNavigationStrategy
		{
			get => base.XYFocusLeftNavigationStrategy;
			set => base.XYFocusLeftNavigationStrategy = value;
		}

		public new static DependencyProperty XYFocusLeftNavigationStrategyProperty { get; } = UIElement.XYFocusLeftNavigationStrategyProperty;

		public new XYFocusNavigationStrategy XYFocusRightNavigationStrategy
		{
			get => base.XYFocusRightNavigationStrategy;
			set => base.XYFocusRightNavigationStrategy = value;
		}

		public new static DependencyProperty XYFocusRightNavigationStrategyProperty { get; } = UIElement.XYFocusRightNavigationStrategyProperty;

		public new XYFocusNavigationStrategy XYFocusUpNavigationStrategy
		{
			get => base.XYFocusUpNavigationStrategy;
			set => base.XYFocusUpNavigationStrategy = value;
		}

		public new static DependencyProperty XYFocusUpNavigationStrategyProperty { get; } = UIElement.XYFocusUpNavigationStrategyProperty;
#endif
	}
}
