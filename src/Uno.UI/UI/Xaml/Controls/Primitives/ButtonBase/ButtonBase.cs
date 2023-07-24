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
using Uno.Foundation.Logging;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.System;
#if __IOS__
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
		public
#if __ANDROID__
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

#if __ANDROID__ || __IOS__
		private void OnCanExecuteChanged()
		{
			this.CoerceValue(IsEnabledProperty);
		}
#endif

		private protected override object CoerceIsEnabled(object baseValue, DependencyPropertyValuePrecedences precedence)
		{
			if (Command != null
				&& !Command.CanExecute(CommandParameter))
			{
				return false;
			}

			return base.CoerceIsEnabled(baseValue, precedence);
		}

		private protected override void OnContentTemplateRootSet() => RegisterEvents();


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

#if false
		private void OnClick(PointerRoutedEventArgs args = null)
		{
			Click?.Invoke(this, new RoutedEventArgs(args?.OriginalSource ?? this));

			InvokeCommand();
		}
#endif

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
	}
}
