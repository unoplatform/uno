// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.


// IconSource is implemented in WUX in the OS repo, so we don't need to
// include IconSource.h on that side.
namespace Windows.UI.Xaml.Controls
{
	public partial class SwipeItem
	{
		static double s_swipeItemWidth = 68.0;
		static double s_swipeItemHeight = 60.0;

		SwipeItem()
		{
			__RP_Marker_ClassById(RuntimeProfiler.ProfId_SwipeItem);
		}

#pragma endregion


		void InvokeSwipe(winrt.SwipeControl& swipeControl)
		{
			var eventArgs = winrt.new SwipeItemInvokedEventArgs();
			eventArgs.SwipeControl(swipeControl);
			m_invokedEventSource(this, eventArgs);

			if (s_CommandProperty)
			{
				var command = Command().as<winrt.ICommand > ();
				var param = CommandParameter();

				if (command && command.CanExecute(param))
				{
					command.Execute(param);
				}
			}

			// It stays open when onInvoked is expand.
			if (BehaviorOnInvoked() == winrt.SwipeBehaviorOnInvoked.Close ||
				BehaviorOnInvoked() == winrt.SwipeBehaviorOnInvoked.Auto)
			{
				swipeControl.Close();
			}
		}

		void OnPropertyChanged(winrt.DependencyPropertyChangedEventArgs& args)
		{
			if (args.Property() == winrt.CommandProperty())
			{
				OnCommandChanged(args.OldValue(). as<winrt.ICommand > (), args.NewValue().as<winrt.ICommand > ());
			}
		}

		void OnCommandChanged(winrt.ICommand& /oldCommand/, winrt.ICommand& newCommand)
		{
			if (var newUICommand = newCommand.try_as<winrt.XamlUICommand>())
			{
				CommandingHelpers.BindToLabelPropertyIfUnset(newUICommand, this, winrt.TextProperty());
				CommandingHelpers.BindToIconSourcePropertyIfUnset(newUICommand, this, winrt.IconSourceProperty());
			}
		}

		void GenerateControl(winrt.AppBarButton& appBarButton, winrt.Style& swipeItemStyle)
		{
			appBarButton.Style(swipeItemStyle);
			if (Background())
			{
				appBarButton.Background(Background());
			}

			if (Foreground())
			{
				appBarButton.Foreground(Foreground());
			}

			if (IconSource())
			{
				appBarButton.Icon(IconSource().CreateIconElement());
			}

			appBarButton.Label(Text());
			AttachEventHandlers(appBarButton);
		}


		void AttachEventHandlers(winrt.AppBarButton& appBarButton)
		{
			var weakThis = get_weak();
			appBarButton.Tapped({
				[weakThis]
				(auto

				&sender, auto & args) {
					if (var temp = weakThis.get()) temp.OnItemTapped(sender, args);
				}
			});
			appBarButton.PointerPressed({
				[weakThis]
				(auto

				&sender, auto & args) {
					if (var temp = weakThis.get()) temp.OnPointerPressed(sender, args);
				}
			});
		}

		void OnItemTapped(
			winrt.DependencyObject& sender,

		winrt.TappedRoutedEventArgs& args)
		{
			var current = winrt.VisualTreeHelper.GetParent(sender.try_as<winrt.DependencyObject>());
			while (current)
			{
				var control = current.try_as<winrt.SwipeControl>();
				if (control)
				{
					InvokeSwipe(control);
					args.Handled(true);
				}

				current = winrt.VisualTreeHelper.GetParent(current);
			}
		}

		void OnPointerPressed(
			winrt.DependencyObject& /sender/,

		winrt.PointerRoutedEventArgs& args)
		{
			if (args.Pointer().PointerDeviceType() == winrt.Devices.Input.PointerDeviceType.Touch)
			{
				// if we press an item, we want to handle it and not let the parent SwipeControl receive the input
				args.Handled(true);
			}
		}
	}
}
