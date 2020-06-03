using Uno.Client;
using Uno.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Uno.Disposables;
using System.Text;
using System.Windows.Input;
using Windows.UI.Input;
using Windows.UI.Xaml.Input;
using Uno.Extensions.Specialized;
using Uno.Logging;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#if XAMARIN_IOS
using View = UIKit.UIView;
#elif __MACOS__
using View = AppKit.NSView;
#elif __ANDROID__
using View = Android.Views.View;
#else
using View = Windows.UI.Xaml.UIElement;
#endif

namespace Windows.UI.Xaml.Controls.Primitives
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
			InitializeProperties();

			Unloaded += (s, e) =>
				IsPressed = false;

			DefaultStyleKey = typeof(ButtonBase);
		}

		public new bool IsPointerOver
		{
			get => base.IsPointerOver;
			set => base.IsPointerOver = value;
		}

		private void InitializeProperties()
		{
			OnIsEnabledChanged(false, IsEnabled);
			PartialInitializeProperties();
		}

		partial void PartialInitializeProperties();

		#region Command (DP)
		public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
			"Command", typeof(ICommand), typeof(ButtonBase), new PropertyMetadata(default(ICommand), OnCommandChanged));

		public ICommand Command
		{
			get { return (ICommand)this.GetValue(CommandProperty); }
			set { this.SetValue(CommandProperty, value); }
		}

		private static void OnCommandChanged(object dependencyobject, DependencyPropertyChangedEventArgs args)
		{
			((ButtonBase)dependencyobject).OnCommandChanged(args.NewValue as ICommand);
		}
		#endregion

		#region CommandParameter
		public static DependencyProperty CommandParameterProperty { get; } =
			DependencyProperty.Register(
				"CommandParameter",
				typeof(object),
				typeof(Controls.Primitives.ButtonBase),
				new FrameworkPropertyMetadata(default(object), OnCommandParameterChanged));

		public object CommandParameter
		{
			get => (object)GetValue(CommandParameterProperty);
			set => SetValue(CommandParameterProperty, value);
		}

		private static void OnCommandParameterChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			((ButtonBase)dependencyObject)?.CoerceValue(IsEnabledProperty);
		}
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
			ownerType: typeof(Controls.Primitives.ButtonBase),
			typeMetadata: new FrameworkPropertyMetadata(ClickMode.Release));

		public static DependencyProperty IsPointerOverProperty { get; } =
		DependencyProperty.Register(
			name: nameof(IsPointerOver),
			propertyType: typeof(bool),
			ownerType: typeof(Controls.Primitives.ButtonBase),
			typeMetadata: new FrameworkPropertyMetadata(default(bool)));

		public static DependencyProperty IsPressedProperty { get; } =
		DependencyProperty.Register(
			name: nameof(IsPressed),
			propertyType: typeof(bool),
			ownerType: typeof(Controls.Primitives.ButtonBase),
			typeMetadata: new FrameworkPropertyMetadata(default(bool)));

		partial void RegisterEvents();

		private void OnCommandChanged(ICommand newCommand)
		{
			_commandCanExecute.Disposable = null;

			if (newCommand != null)
			{
				EventHandler handler = (s, e) => OnCanExecuteChanged();

				newCommand.CanExecuteChanged += handler;

				_commandCanExecute.Disposable = Disposable
					.Create(() =>
					{
						newCommand.CanExecuteChanged -= handler;
					}
				);
			}

			OnCanExecuteChanged();
		}

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

		protected override void OnIsEnabledChanged(bool oldValue, bool newValue)
		{
			base.OnIsEnabledChanged(oldValue, newValue);
			OnIsEnabledChangedPartial(oldValue, newValue);
		}

		partial void OnIsEnabledChangedPartial(bool oldValue, bool newValue);

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

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			RegisterEvents();
		}

		/// <inheritdoc />
		protected override void OnPointerEntered(PointerRoutedEventArgs args)
		{
			if (ClickMode == ClickMode.Hover)
			{
				RaiseClick(args);
			}

			base.OnPointerEntered(args);
		}

		/// <inheritdoc />
		protected override void OnPointerPressed(PointerRoutedEventArgs args)
		{
			var mode = ClickMode;
			if (mode != ClickMode.Hover)
			{
				// Note: even if ClickMode is Press, we capture the pointer and handle the Release args, but we do nothing if Hover

				// Capturing the Pointer ensures that we will be the first element to receive the pointer released event
				// It will also ensure that if we scroll while pressing the button, as the capture will be lost, we won't raise Click.
				var handle = args.GetCurrentPoint(this).Properties.IsLeftButtonPressed && CapturePointer(args.Pointer);
				args.Handled = handle;

				IsPressed = true;

				if (handle && mode == ClickMode.Press)
				{
					RaiseClick(args);
				}
			}

			base.OnPointerPressed(args);

#if !__WASM__
			// TODO: Remove when Focus is implemented properly.
			// Focus the button when pressed down to ensure that any focused TextBox loses focus 
			// so that TwoWay binding (to source) is triggered before the button is released and Click is raised.
			Focus(FocusState.Pointer);
#endif
		}

		/// <inheritdoc />
		protected override void OnPointerReleased(PointerRoutedEventArgs args)
		{
			if (IsCaptured(args.Pointer))
			{
				// The click is raised as soon as the release occurs over the button,
				// no matter the distance from the pressed location nor the delay since pressed.
				var location = args.GetCurrentPoint(this).Position;
				if (location.X >= 0 && location.Y >= 0
					&& location.X <= ActualWidth && location.Y <= ActualHeight)
				{
					if (ClickMode == ClickMode.Release)
					{
						RaiseClick(args); // First raise the click
					}
				}

				IsPressed = false;

				// This should be automatically done by the pointers due to release, but if for any reason
				// the state is invalid, this makes sure to not keep invalid capture longer than needed.
				// Note: This must be done ** after ** the click event (UWP raise CaptureLost event after)
				ReleasePointerCapture(args.Pointer);

				// On UWP the args are handled no matter if the Click was raised or not
				args.Handled = true;
			}

			base.OnPointerReleased(args);
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
			OnClick(args);
		}

		internal void AutomationPeerClick()
		{
			OnClick();
		}

		private void OnClick(PointerRoutedEventArgs args = null)
		{
			Click?.Invoke(this, new RoutedEventArgs(args?.OriginalSource ?? this));

			try
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
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
	}
}
