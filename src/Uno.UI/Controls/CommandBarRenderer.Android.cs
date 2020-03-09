#if __ANDROID__
using Android.Graphics.Drawables;
using Android.Support.V7.Widget;
using Android.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.Disposables;
using Uno.UI.Extensions;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Android.Support.V4.Graphics.Drawable;
using Android.Support.V7.App;
using Android.App;
using Uno.Extensions;
using Uno.Logging;
using Microsoft.Extensions.Logging;
using Android.Views.InputMethods;
using Android.Content;

namespace Uno.UI.Controls
{
	internal partial class CommandBarRenderer : Renderer<CommandBar, Toolbar>
	{
		private static DependencyProperty SubtitleProperty = ToolkitHelper.GetProperty("Uno.UI.Toolkit.CommandBarExtensions", "Subtitle");
		private static DependencyProperty NavigationCommandProperty = ToolkitHelper.GetProperty("Uno.UI.Toolkit.CommandBarExtensions", "NavigationCommand");
		private static DependencyProperty BackButtonVisibilityProperty = ToolkitHelper.GetProperty("Uno.UI.Toolkit.CommandBarExtensions", "BackButtonVisibility");
		private static DependencyProperty BackButtonForegroundProperty = ToolkitHelper.GetProperty("Uno.UI.Toolkit.CommandBarExtensions", "BackButtonForeground");
		private static DependencyProperty BackButtonIconProperty = ToolkitHelper.GetProperty("Uno.UI.Toolkit.CommandBarExtensions", "BackButtonIcon");

		private static string _actionBarUpDescription;
		private static string ActionBarUpDescription
		{
			get
			{
				if (_actionBarUpDescription == null)
				{
					if (ContextHelper.Current is Activity activity && activity.Resources.GetIdentifier("action_bar_up_description", "string", "android") is int resourceId)
					{
						_actionBarUpDescription = activity.Resources.GetString(resourceId);
					}
					else
					{
						if (typeof(CommandBarRenderer).Log().IsEnabled(LogLevel.Error))
						{
							typeof(CommandBarRenderer).Log().Error("Couldn't resolve resource 'action_bar_up_description'.");
						}
					}
				}

				return _actionBarUpDescription;
			}
		}

		private Android.Graphics.Color? _originalTitleTextColor;
		private Android.Graphics.Drawables.Drawable _originalBackground;
		private Border _contentContainer;

		public CommandBarRenderer(CommandBar element) : base(element) { }

		protected override Toolbar CreateNativeInstance() => new Toolbar(UI.ContextHelper.Current);

		protected override IEnumerable<IDisposable> Initialize()
		{
			_originalBackground = Native.Background;
			_originalTitleTextColor = Native.GetTitleTextColor();

			// Content
			// This allows custom Content to be properly laid out inside the native Toolbar.
			_contentContainer = new Border()
			{
				Visibility = Visibility.Collapsed,
				// This container requires a fixed height to be properly laid out by its native parent.
				// According to Google's Material Design Guidelines, the Toolbar must have a minimum height of 48.
				// https://material.io/guidelines/layout/structure.html
				Height = 48,
				Name = "CommandBarRendererContentHolder",

				// Set the alignment so that the measured sized
				// returned is size of the child, not the available
				// size provided to the ToolBar view.
				VerticalAlignment = VerticalAlignment.Top,
				HorizontalAlignment = HorizontalAlignment.Left,
			};

			_contentContainer.SetParent(Element);
			Native.AddView(_contentContainer);
			yield return Disposable.Create(() => Native.RemoveView(_contentContainer));
			yield return _contentContainer.RegisterParentChangedCallback(this, OnContentContainerParentChanged);

			// Commands.Click
			Native.MenuItemClick += Native_MenuItemClick;
			yield return Disposable.Create(() => Native.MenuItemClick -= Native_MenuItemClick);

			// NavigationCommand.Click
			Native.NavigationClick += Native_NavigationClick;
			yield return Disposable.Create(() => Native.NavigationClick -= Native_NavigationClick);

			// Commands
			VectorChangedEventHandler<ICommandBarElement> OnVectorChanged = (s, e) => Invalidate();
			Element.PrimaryCommands.VectorChanged += OnVectorChanged;
			Element.SecondaryCommands.VectorChanged += OnVectorChanged;
			yield return Disposable.Create(() => Element.PrimaryCommands.VectorChanged -= OnVectorChanged);
			yield return Disposable.Create(() => Element.SecondaryCommands.VectorChanged -= OnVectorChanged);

			// Properties
			yield return Element.RegisterDisposableNestedPropertyChangedCallback(
				(s, e) => Invalidate(),
				new[] { CommandBar.PrimaryCommandsProperty },
				new[] { CommandBar.SecondaryCommandsProperty },
				new[] { CommandBar.ContentProperty },
				new[] { CommandBar.ForegroundProperty },
				new[] { CommandBar.ForegroundProperty, SolidColorBrush.ColorProperty },
				new[] { CommandBar.ForegroundProperty, SolidColorBrush.OpacityProperty },
				new[] { CommandBar.BackgroundProperty },
				new[] { CommandBar.BackgroundProperty, SolidColorBrush.ColorProperty },
				new[] { CommandBar.BackgroundProperty, SolidColorBrush.OpacityProperty },
				new[] { CommandBar.VisibilityProperty },
				new[] { CommandBar.PaddingProperty },
				new[] { CommandBar.OpacityProperty },
				new[] { SubtitleProperty },
				new[] { NavigationCommandProperty },
				new[] { BackButtonVisibilityProperty },
				new[] { BackButtonForegroundProperty },
				new[] { BackButtonIconProperty }
			);
		}

		private void OnContentContainerParentChanged(object instance, object key, DependencyObjectParentChangedEventArgs args)
		{
			// Even though we set the CommandBar as the parent of the _contentContainer,
			// it will change to the native control when the view is added.
			// This control is the visual parent but is not a DependencyObject and will not propagate the DataContext.
			// In order to ensure the DataContext is propagated properly, we restore the CommandBar
			// parent that can propagate the DataContext.
			if (args.NewParent != Element)
			{
				_contentContainer.SetParent(Element);
			}
		}

		protected override void Render()
		{
			// Content
			Native.Title = Element.Content as string;
			_contentContainer.Child = Element.Content as View;
			_contentContainer.Visibility = Element.Content is View
				? Visibility.Visible
				: Visibility.Collapsed;

			// CommandBarExtensions.Subtitle
			Native.Subtitle = Element.GetValue(SubtitleProperty) as string;

			// Background
			var backgroundColor = (Element.Background as SolidColorBrush)?.ColorWithOpacity;
			if (backgroundColor != null)
			{
				Native.SetBackgroundColor((Android.Graphics.Color)backgroundColor);
			}
			else
			{
				Native.Background = _originalBackground ?? new ColorDrawable(Color.FromArgb(255, 250, 250, 250));
			}

			// Foreground
			var foregroundColor = (Element.Foreground as SolidColorBrush)?.ColorWithOpacity;
			if (foregroundColor != null)
			{
				Native.SetTitleTextColor((Android.Graphics.Color)foregroundColor);
			}
			else
			{
				Native.SetTitleTextColor(_originalTitleTextColor.Value);
			}

			// PrimaryCommands & SecondaryCommands
			var currentMenuItemIds = GetMenuItems(Native.Menu).Select(i => i.ItemId);
			var intendedMenuItemIds = Element.PrimaryCommands
				.Concat(Element.SecondaryCommands)
				.OfType<AppBarButton>()
				.Select(i => i.GetHashCode());

			if (!currentMenuItemIds.SequenceEqual(intendedMenuItemIds))
			{
				Native.Menu.Clear();
				foreach (var command in Element.PrimaryCommands.Concat(Element.SecondaryCommands).OfType<AppBarButton>())
				{
					var menuItem = Native.Menu.Add(0, command.GetHashCode(), Menu.None, null);

					var renderer = command.GetRenderer(() => new AppBarButtonRenderer(command));
					renderer.Native = menuItem;

					// This ensures that Behaviors expecting this button to be in the logical tree work. 
					command.SetParent(Element);
				}
			}

			// CommandBarExtensions.NavigationCommand
			var navigationCommand = Element.GetValue(NavigationCommandProperty) as AppBarButton;
			if (navigationCommand != null)
			{
				var renderer = navigationCommand.GetRenderer(() => new NavigationAppBarButtonRenderer(navigationCommand));
				renderer.Native = Native;

				// This ensures that Behaviors expecting this button to be in the logical tree work. 
				navigationCommand.SetParent(Element);
			}
			// CommandBarExtensions.BackButtonVisibility
			else if ((Visibility)Element.GetValue(BackButtonVisibilityProperty) == Visibility.Visible)
			{
				// CommandBarExtensions.BackButtonIcon
				if (Element.GetValue(BackButtonIconProperty) is BitmapIcon bitmapIcon)
				{
					Native.NavigationIcon = DrawableHelper.FromUri(bitmapIcon.UriSource);
				}
				else
				{
					Native.NavigationIcon = new Android.Support.V7.Graphics.Drawable.DrawerArrowDrawable(ContextHelper.Current)
					{
						// 0 = menu icon
						// 1 = back icon
						Progress = 1,
					};
				}

				// CommandBarExtensions.BackButtonForeground
				var backButtonForeground = (Element.GetValue(BackButtonForegroundProperty) as SolidColorBrush)?.ColorWithOpacity;
				if (backButtonForeground != null)
				{
					switch (Native.NavigationIcon)
					{
						case Android.Support.V7.Graphics.Drawable.DrawerArrowDrawable drawerArrowDrawable:
							drawerArrowDrawable.Color = (Android.Graphics.Color)backButtonForeground;
							break;
						case Drawable drawable:
							DrawableCompat.SetTint(drawable, (Android.Graphics.Color)backButtonForeground);
							break;
					}
				}

				Native.NavigationContentDescription = ActionBarUpDescription;
			}
			else
			{
				Native.NavigationIcon = null;
				Native.NavigationContentDescription = null;
			}

			// Padding
			var physicalPadding = Element.Padding.LogicalToPhysicalPixels();
			Native.SetPadding(
				(int)physicalPadding.Left,
				(int)physicalPadding.Top,
				(int)physicalPadding.Right,
				(int)physicalPadding.Bottom
			);

			// Opacity
			Native.Alpha = (float)Element.Opacity;
		}

		private IEnumerable<IMenuItem> GetMenuItems(IMenu menu)
		{
			for (int i = 0; i < menu.Size(); i++)
			{
				yield return menu.GetItem(i);
			}
		}

		private void Native_MenuItemClick(object sender, Toolbar.MenuItemClickEventArgs e)
		{
			CloseKeyboard();

			var hashCode = e.Item.ItemId;
			var appBarButton = Element.PrimaryCommands
				.Concat(Element.SecondaryCommands)
				.OfType<AppBarButton>()
				.FirstOrDefault(c => hashCode == c.GetHashCode());

			appBarButton?.RaiseClick();
		}

		private void Native_NavigationClick(object sender, Toolbar.NavigationClickEventArgs e)
		{
			CloseKeyboard();

			var navigationCommand = Element.GetValue(NavigationCommandProperty) as AppBarButton;
			if (navigationCommand != null)
			{
				navigationCommand.RaiseClick();
			}
			else
			{
				SystemNavigationManager.GetForCurrentView().RequestBack();
			}
		}

		private void CloseKeyboard()
		{
			if ((ContextHelper.Current as Activity)?.CurrentFocus is View focused)
			{
				var imm = (InputMethodManager)ContextHelper.Current.GetSystemService(Context.InputMethodService);
				imm.HideSoftInputFromWindow(focused.WindowToken, HideSoftInputFlags.None);
			}
		}
	}
}
#endif
