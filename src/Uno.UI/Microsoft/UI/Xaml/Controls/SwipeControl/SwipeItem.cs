// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// IconSource is implemented in WUX in the OS repo, so we don't need to
// include IconSource.h on that side.

// Imported in uno on 2021/03/21 from commit 307bd99682cccaa128483036b764c0b7c862d666
// https://github.com/microsoft/microsoft-ui-xaml/blob/307bd99682cccaa128483036b764c0b7c862d666/dev/SwipeControl/SwipeItem.cpp

using System;
using System.Windows.Input;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

#if HAS_UNO_WINUI
using Microsoft.UI.Input;
#else
using Windows.Devices.Input;
using Windows.UI.Input;
#endif

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	public partial class SwipeItem : DependencyObject
	{
		//static double s_swipeItemWidth = 68.0;
		//static double s_swipeItemHeight = 60.0;

		public SwipeItem()
		{
		}

		internal void InvokeSwipe(SwipeControl swipeControl)
		{
			var eventArgs = new SwipeItemInvokedEventArgs(swipeControl);

			m_invokedEventSource?.Invoke(this, eventArgs);

			if (CommandProperty is { })
			{
				var command = Command as ICommand;
				var param = CommandParameter;

				if (command is { } && command.CanExecute(param))
				{
					command.Execute(param);
				}
			}

			// It stays open when onInvoked is expand.
			if (BehaviorOnInvoked == SwipeBehaviorOnInvoked.Close ||
				BehaviorOnInvoked == SwipeBehaviorOnInvoked.Auto)
			{
				swipeControl.Close();
			}
		}

		private void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
		{
			if (args.Property == CommandProperty)
			{
				OnCommandChanged(args.OldValue as ICommand, args.NewValue as ICommand);
			}
		}

		private void OnCommandChanged(ICommand oldCommand, ICommand newCommand)
		{
			if (newCommand is XamlUICommand newUICommand)
			{
				CommandingHelpers.BindToLabelPropertyIfUnset(newUICommand, this, TextProperty);
				CommandingHelpers.BindToIconSourcePropertyIfUnset(newUICommand, this, IconSourceProperty);
			}
		}

		internal void GenerateControl(AppBarButton appBarButton, Style swipeItemStyle)
		{
			appBarButton.Style(swipeItemStyle);
			if (Background is { })
			{
				appBarButton.Background = Background;
			}

			if (Foreground is { })
			{
				appBarButton.Foreground = Foreground;
			}

			if (IconSource is { })
			{
				appBarButton.Icon = IconSource.CreateIconElement();
			}

			appBarButton.Label = Text;
			AttachEventHandlers(appBarButton);
		}

		private void AttachEventHandlers(AppBarButton appBarButton)
		{
			var weakThis = new WeakReference<SwipeItem>(this);
			appBarButton.Tapped += (sender, args) =>
			{
				if (weakThis.TryGetTarget(out var temp))
				{
					temp.OnItemTapped(sender, args);
				}
			};
			appBarButton.PointerPressed += (sender, args) =>
			{
				if (weakThis.TryGetTarget(out var temp))
				{
					temp.OnPointerPressed(sender, args);
				}
			};
		}

		private void OnItemTapped(
			object sender,
			TappedRoutedEventArgs args)
		{
			var current = VisualTreeHelper.GetParent(sender as DependencyObject);
			while (current is { })
			{
				var control = current as SwipeControl;
				if (control is { })
				{
					InvokeSwipe(control);
					args.Handled = true;
				}

				current = VisualTreeHelper.GetParent(current);
			}
		}

		private void OnPointerPressed(
			object sender,
			PointerRoutedEventArgs args)
		{
			if (args.Pointer.PointerDeviceType == PointerDeviceType.Touch)
			{
				// if we press an item, we want to handle it and not let the parent SwipeControl receive the input
				args.Handled = true;
			}
		}
	}
}
