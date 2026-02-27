#if IS_UNIT_TESTS || __WASM__
#pragma warning disable CS0067
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.UI.Core;
using Microsoft.UI.Input;
using Uno.UI.Xaml;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.System;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Helpers;

#if __APPLE_UIKIT__
using UIKit;
#endif

namespace Microsoft.UI.Xaml
{
	public partial class UIElement : DependencyObject, IXUidProvider
	{
		// TODO: Add more PropMethodCall and combine them in BitVector32
		private bool _isHitTestVisible = true;

		public bool IsHitTestVisible
		{
			get => (bool)GetValue(IsHitTestVisibleProperty);
			set => SetValue(IsHitTestVisibleProperty, value);
		}

		public static DependencyProperty IsHitTestVisibleProperty { get; } = DependencyProperty.Register(
			nameof(IsHitTestVisible),
			typeof(bool),
			typeof(UIElement),
			new FrameworkPropertyMetadata(defaultValue: true, propertyChangedCallback: (s, e) => ((UIElement)s).OnIsHitTestVisibleChanged((bool)e.OldValue, (bool)e.NewValue))
			{
				PropMethodCall = HitTestVisible,
			}
		);

		private static object HitTestVisible(DependencyObject @do, bool isGet, object valueToSet)
		{
			var @this = (UIElement)@do;
			if (isGet)
			{
				return Boxes.Box(@this._isHitTestVisible);
			}
			else
			{
				var newValue = (bool)valueToSet;
				if (newValue != @this._isHitTestVisible)
				{
					@this._isHitTestVisible = newValue;
					// The value has changed.
					return true;
				}

				// The value didn't change.
				return false;
			}
		}


		[GeneratedDependencyProperty(DefaultValue = 1.0, ChangedCallback = true)]
		public static DependencyProperty OpacityProperty { get; } = CreateOpacityProperty();

		public double Opacity
		{
			get => GetOpacityValue();
			set => SetOpacityValue(value);
		}

		/// <summary>
		/// Sets the visibility of the current view
		/// </summary>
		[GeneratedDependencyProperty(DefaultValue = Visibility.Visible, ChangedCallback = true, Options = FrameworkPropertyMetadataOptions.AffectsMeasure)]
		public static DependencyProperty VisibilityProperty { get; } = CreateVisibilityProperty();

		public
#if __ANDROID__
		new
#endif
		Visibility Visibility
		{
			get => GetVisibilityValue();
			set => SetVisibilityValue(value);
		}

		/// <summary>
		/// Represents the final cursor shape of the element that is set using ProtectedCursor.
		/// </summary>
		/// <remarks>
		/// This property should never be directly set except from ProtectedCursor's ChangedCallback, and its value should always be calculated through the dependency property system.
		/// </remarks>
		/// <remarks>
		/// The type is nullable because we need a state that indicates that the cursor should be hidden. We choose that state to be null.
		/// </remarks>
		[GeneratedDependencyProperty(DefaultValue = InputSystemCursorShape.Arrow, Options = FrameworkPropertyMetadataOptions.Inherits, ChangedCallback = false)]
		internal static DependencyProperty CalculatedFinalCursorProperty { get; } = CreateCalculatedFinalCursorProperty();

		internal InputSystemCursorShape? CalculatedFinalCursor
		{
			get => GetCalculatedFinalCursorValue();
			private set => SetCalculatedFinalCursorValue(value);
		}

		[GeneratedDependencyProperty(DefaultValue = null, ChangedCallback = true, Options = FrameworkPropertyMetadataOptions.LogicalChild)]
		public static DependencyProperty ContextFlyoutProperty { get; } = CreateContextFlyoutProperty();

		public FlyoutBase ContextFlyout
		{
			get => GetContextFlyoutValue();
			set => SetContextFlyoutValue(value);
		}

		[GeneratedDependencyProperty(DefaultValue = null)]
		internal static DependencyProperty KeyboardAcceleratorsProperty { get; } = CreateKeyboardAcceleratorsProperty();

		public IList<KeyboardAccelerator> KeyboardAccelerators
		{
			get => GetKeyboardAcceleratorsValue();
			private set => SetKeyboardAcceleratorsValue(value);
		}

		/// <summary>
		/// Gets or sets a value that indicates whether the control tooltip displays
		/// the key combination for its associated keyboard accelerator.
		/// </summary>
		public KeyboardAcceleratorPlacementMode KeyboardAcceleratorPlacementMode
		{
			get => (KeyboardAcceleratorPlacementMode)GetValue(KeyboardAcceleratorPlacementModeProperty);
			set => SetValue(KeyboardAcceleratorPlacementModeProperty, value);
		}

		/// <summary>
		/// Identifies the KeyboardAcceleratorPlacementMode dependency property.
		/// </summary>
		public static DependencyProperty KeyboardAcceleratorPlacementModeProperty { get; } =
			DependencyProperty.Register(
				nameof(KeyboardAcceleratorPlacementMode),
				typeof(KeyboardAcceleratorPlacementMode),
				typeof(UIElement),
				new FrameworkPropertyMetadata(KeyboardAcceleratorPlacementMode.Auto, FrameworkPropertyMetadataOptions.Inherits));

		/// <summary>
		/// Gets or sets a value that indicates the control tooltip that displays the accelerator key combination.
		/// </summary>
		public DependencyObject KeyboardAcceleratorPlacementTarget
		{
			get => (DependencyObject)GetValue(KeyboardAcceleratorPlacementTargetProperty);
			set => SetValue(KeyboardAcceleratorPlacementTargetProperty, value);
		}

		/// <summary>
		/// Identifies the KeyboardAcceleratorPlacementTarget dependency property.
		/// </summary>
		public static DependencyProperty KeyboardAcceleratorPlacementTargetProperty { get; } =
			DependencyProperty.Register(
				nameof(KeyboardAcceleratorPlacementTarget),
				typeof(DependencyObject),
				typeof(UIElement),
				new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext));

		/// <summary>
		/// Occurs when a keyboard shortcut (or accelerator) is pressed.
		/// </summary>
		public event TypedEventHandler<UIElement, ProcessKeyboardAcceleratorEventArgs> ProcessKeyboardAccelerators;
	}
}
