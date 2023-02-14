using Uno.Client;
using Uno.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Uno.Disposables;
using System.Text;
using System.Windows.Input;
using Windows.UI.Input;
using Microsoft.UI.Xaml.Input;
using Uno.Extensions.Specialized;
using Uno.Foundation.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.System;
#if XAMARIN_IOS
using View = UIKit.UIView;
#elif __MACOS__
using View = AppKit.NSView;
#elif __ANDROID__
using View = Android.Views.View;
#else
using View = Microsoft.UI.Xaml.UIElement;
#endif

namespace Microsoft.UI.Xaml.Controls.Primitives
{
	public partial class ButtonBase : ContentControl
	{
		static ButtonBase()
		{
			IsEnabledProperty.OverrideMetadata(
				typeof(ButtonBase),
				new FrameworkPropertyMetadata(
					defaultValue: true,
					propertyChangedCallback: null,
					coerceValueCallback: CoerceIsEnabled
				)
			);
		}

		private readonly SerialDisposable _commandCanExecute = new SerialDisposable();

		public
#if XAMARIN_ANDROID
			new
#endif
			event RoutedEventHandler Click;

		public ButtonBase()
		{
			Initialize();

			InitializeProperties();

			Unloaded += (s, e) =>
				IsPressed = false;

			DefaultStyleKey = typeof(ButtonBase);
		}

		private protected override void OnLoaded()
		{
			base.OnLoaded();
			OnLoadedPartial();

			RegisterEvents();
		}

		partial void OnLoadedPartial();

		private protected override void OnUnloaded()
		{
			base.OnUnloaded();
			OnUnloadedPartial();
		}

		partial void OnUnloadedPartial();

		public new bool IsPointerOver
		{
			get => base.IsPointerOver;
			set => base.IsPointerOver = value;
		}

		private void InitializeProperties()
		{
			PartialInitializeProperties();
		}

		partial void PartialInitializeProperties();

		#region Command (DP)
		public static DependencyProperty CommandProperty { get; } = DependencyProperty.Register(
			nameof(Command), typeof(ICommand), typeof(ButtonBase), new FrameworkPropertyMetadata(default(ICommand)));

		public ICommand Command
		{
			get => (ICommand)GetValue(CommandProperty);
			set => SetValue(CommandProperty, value);
		}

		#endregion

		#region CommandParameter
		public static DependencyProperty CommandParameterProperty { get; } =
			DependencyProperty.Register(
				nameof(CommandParameter),
				typeof(object),
				typeof(ButtonBase),
				new FrameworkPropertyMetadata(default(object), OnCommandParameterChanged));

		public object CommandParameter
		{
			get => GetValue(CommandParameterProperty);
			set => SetValue(CommandParameterProperty, value);
		}

		private static void OnCommandParameterChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args) =>
			((ButtonBase)dependencyObject)?.CoerceValue(IsEnabledProperty);
		#endregion

		public ClickMode ClickMode
		{
			get => (ClickMode)this.GetValue(ClickModeProperty);
			set => this.SetValue(ClickModeProperty, value);
		}

		public new bool IsPressed
		{
			get => (bool)GetValue(IsPressedProperty);
			internal set => SetValue(IsPressedProperty, value);
		}

		public static DependencyProperty ClickModeProperty { get; } =
		DependencyProperty.Register(
			name: nameof(ClickMode),
			propertyType: typeof(ClickMode),
			ownerType: typeof(ButtonBase),
			typeMetadata: new FrameworkPropertyMetadata(ClickMode.Release));

		public static DependencyProperty IsPointerOverProperty { get; } =
		DependencyProperty.Register(
			name: nameof(IsPointerOver),
			propertyType: typeof(bool),
			ownerType: typeof(ButtonBase),
			typeMetadata: new FrameworkPropertyMetadata(default(bool)));

		public static DependencyProperty IsPressedProperty { get; } =
		DependencyProperty.Register(
			name: nameof(IsPressed),
			propertyType: typeof(bool),
			ownerType: typeof(ButtonBase),
			typeMetadata: new FrameworkPropertyMetadata(default(bool)));

		partial void RegisterEvents();

		private void OnCanExecuteChanged()
		{
			this.CoerceValue(IsEnabledProperty);
		}

		private static object CoerceIsEnabled(DependencyObject dependencyObject, object baseValue)
		{
			if (dependencyObject is ButtonBase buttonBase
				&& buttonBase.Command != null
				&& !buttonBase.Command.CanExecute(buttonBase.CommandParameter))
			{
				return false;
			}

			return baseValue;
		}

		public override View ContentTemplateRoot
		{
			get
			{
				return base.ContentTemplateRoot;
			}
			protected set
			{
				base.ContentTemplateRoot = value;

				RegisterEvents();
			}
		}


		// Might be changed if the method does not conflict in UnoViewGroup.
		internal override bool IsViewHit()
		{
			// Overrides the need for a non-null Background (required by base.IsViewHit)
			return true;
		}

		// Allows native buttons (e.g., UIBarButtonItem, IMenuItem) to raise clicks on their associated AppBarButton.
		internal void RaiseClick(PointerRoutedEventArgs args = null)
		{
			OnClick();
		}

		internal void AutomationPeerClick()
		{
			OnClick();
		}

		private void OnClick(PointerRoutedEventArgs args = null)
		{
			Click?.Invoke(this, new RoutedEventArgs(args?.OriginalSource ?? this));

			InvokeCommand();
		}

		internal void InvokeCommand()
		{
			try
			{
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().Debug("Executing command");
				}

				Command.ExecuteIfPossible(CommandParameter);
			}
			catch (Exception e)
			{
				this.Log().Error("Failed to execute command", e);
			}
		}

		private void OnKeyDown(object sender, KeyRoutedEventArgs args)
		{
			// Key presses can be ignored when disabled or in ClickMode.Hover
			if (IsEnabled && ClickMode != ClickMode.Hover)
			{
				if (IsPressKey(args.Key))
				{
					if (!HasPointerCapture)
					{
						IsPressed = true;

						if (ClickMode == ClickMode.Press)
						{
							OnClick();
						}

						args.Handled = true;
					}
				}
				else
				{
					//Any other keys pressed are irrelevant
					IsPressed = false;
				}
			}
		}

		private void OnKeyUp(object sender, KeyRoutedEventArgs args)
		{
			// Key presses can be ignored when disabled or in ClickMode.Hover
			if (IsEnabled && ClickMode != ClickMode.Hover && IsPressKey(args.Key))
			{
				// If the pointer isn't in use, raise the Click event if we're in the
				// correct click mode
				if (!HasPointerCapture)
				{
					if (IsPressed && ClickMode == ClickMode.Release)
					{
						OnClick();
					}

					IsPressed = false;
				}

				args.Handled = true;
			}
		}

		private bool IsPressKey(VirtualKey key) =>
				key == VirtualKey.Space ||
				key == VirtualKey.Enter ||
				key == VirtualKey.Execute ||
				key == VirtualKey.GamepadA;
	}
}
