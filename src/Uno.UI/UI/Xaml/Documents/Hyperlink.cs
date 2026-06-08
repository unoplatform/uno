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
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.Extensions;

namespace Microsoft.UI.Xaml.Documents
{
	public sealed partial class Hyperlink : Span
	{
		private readonly IFocusable _focusableHelper;

		private const string HyperlinkForegroundPressedKey = "HyperlinkForegroundPressed";
		private const string HyperlinkForegroundPointerOverKey = "HyperlinkForegroundPointerOver";
		private const string HyperlinkForeground = nameof(HyperlinkForeground);

		public
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
			event RoutedEventHandler GotFocus;

		internal void OnGotFocus(RoutedEventArgs args) => GotFocus?.Invoke(this, args);

		public
			event RoutedEventHandler LostFocus;

		internal void OnLostFocus(RoutedEventArgs args) => LostFocus?.Invoke(this, args);

		public Hyperlink()
		{
			OnUnderlineStyleChanged();
			_focusableHelper = new FocusableHelper(this);
		}

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

			if (NavigateUri != null)
			{
				_ = Launcher.LaunchUriAsync(NavigateUri);
			}
		}
		#endregion

		internal void SetCurrentForeground()
		{
#if UNO_HAS_ENHANCED_LIFECYCLE
			// MUX Reference: Hyperlink.cpp UpdateForegroundColor uses GetContext()->LookupThemeResource(theme, ...)
			// to resolve resources with the element's theme. We push the containing element's theme
			// onto the resource stack so Application.Current.Resources.TryGetValue resolves with the
			// correct theme, especially when called from pointer events outside theme propagation.
			var needsPush = false;
			var containingFe = GetContainingFrameworkElement();
			if (containingFe is not null)
			{
				var theme = containingFe.GetTheme();
				if (theme != Theme.None)
				{
					var themeKey = Theming.GetBaseValue(theme) == Theme.Light ? "Light" : "Dark";
					var currentActiveTheme = ResourceDictionary.GetActiveTheme();
					if (!themeKey.Equals(currentActiveTheme.Key))
					{
						ResourceDictionary.PushRequestedThemeForSubTree(themeKey);
						needsPush = true;
					}
				}
			}
#endif

			try
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
				else // normal
				{
					// this is close, although not identical, to what the WinUI source does
					this.ClearValue(ForegroundProperty, DependencyPropertyValuePrecedences.Animations);
					if (this.GetCurrentHighestValuePrecedence(ForegroundProperty) == DependencyPropertyValuePrecedences.Local)
					{
						this.SetValue(ForegroundProperty, this.GetValue(ForegroundProperty), DependencyPropertyValuePrecedences.Animations);
					}
					else if (Application.Current.Resources.TryGetValue(HyperlinkForeground, out var defaultBrush))
					{
						this.SetValue(ForegroundProperty, defaultBrush, DependencyPropertyValuePrecedences.Animations);
					}
				}
			}
			finally
			{
#if UNO_HAS_ENHANCED_LIFECYCLE
				if (needsPush)
				{
					ResourceDictionary.PopRequestedThemeForSubTree();
				}
#endif
			}
		}

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
	}
}
