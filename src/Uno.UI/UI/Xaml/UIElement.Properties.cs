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
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Uno;
using Uno.Disposables;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.Controls;
using Uno.UI.Media;
using Uno.UI.Xaml;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;

#if __IOS__
using UIKit;
#endif

namespace Microsoft.UI.Xaml
{
	public partial class UIElement : DependencyObject, IXUidProvider
	{
		/// <summary>
		/// Gets or sets the access key (mnemonic) for this element.
		/// </summary>
		/// <remarks>
		/// Setting this property enables the AccessKeyDisplayRequested event to be raised.
		/// </remarks>
		public string AccessKey
		{
			get => (string)GetValue(AccessKeyProperty);
			set => SetValue(AccessKeyProperty, value);
		}

		/// <summary>
		/// Identifies for the AccessKey dependency property.
		/// </summary>
		public static DependencyProperty AccessKeyProperty { get; } =
			DependencyProperty.Register(
				nameof(AccessKey),
				typeof(string),
				typeof(UIElement),
				new FrameworkPropertyMetadata(default(string)));

		/// <summary>
		/// Gets or sets a source element that provides the access key scope for this element,
		/// even if it's not in the visual tree of the source element.
		/// </summary>
		public DependencyObject AccessKeyScopeOwner
		{
			get => (DependencyObject)this.GetValue(AccessKeyScopeOwnerProperty);
			set => SetValue(AccessKeyScopeOwnerProperty, value);
		}

		/// <summary>
		/// Identifies for the AccessKeyScopeOwner dependency property.
		/// </summary>
		public static DependencyProperty AccessKeyScopeOwnerProperty { get; } =
			DependencyProperty.Register(
				nameof(AccessKeyScopeOwner),
				typeof(DependencyObject),
				typeof(UIElement),
				new FrameworkPropertyMetadata(default(DependencyObject), FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext));

		/// <summary>
		/// Gets or sets a value that indicates whether an element defines its own access key scope.
		/// </summary>
		public bool IsAccessKeyScope
		{
			get => (bool)GetValue(IsAccessKeyScopeProperty);
			set => SetValue(IsAccessKeyScopeProperty, value);
		}

		/// <summary>
		/// Identifies for the IsAccessKeyScope dependency property.
		/// </summary>
		public static DependencyProperty IsAccessKeyScopeProperty { get; } =
			DependencyProperty.Register(
				nameof(IsAccessKeyScope),
				typeof(bool),
				typeof(UIElement),
				new FrameworkPropertyMetadata(default(bool)));

		[GeneratedDependencyProperty(DefaultValue = true, ChangedCallback = true)]
		public static DependencyProperty IsHitTestVisibleProperty { get; } = CreateIsHitTestVisibleProperty();

		public bool IsHitTestVisible
		{
			get => GetIsHitTestVisibleValue();
			set => SetIsHitTestVisibleValue(value);
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
			get => GetKeyboardAcceleratorsValue() ?? (KeyboardAccelerators = new List<KeyboardAccelerator>());
			set => SetKeyboardAcceleratorsValue(value);
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
				new FrameworkPropertyMetadata(KeyboardAcceleratorPlacementMode.Auto));

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
