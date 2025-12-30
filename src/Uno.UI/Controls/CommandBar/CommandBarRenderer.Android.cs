#nullable enable
using Android.Graphics.Drawables;
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
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Android.App;
using Uno.Extensions;
using Uno.Foundation.Logging;

using Android.Views.InputMethods;
using Android.Content;
using AndroidX.AppCompat.Widget;
using AndroidX.Core.Graphics.Drawable;
using Java.IO;

namespace Uno.UI.Controls
{
	internal partial class CommandBarRenderer : Renderer<CommandBar, Toolbar>
	{
		private static DependencyProperty SubtitleProperty = ToolkitHelper.GetProperty("Uno.UI.Toolkit.CommandBarExtensions, Uno.UI.Toolkit", "Subtitle");
		private static DependencyProperty NavigationCommandProperty = ToolkitHelper.GetProperty("Uno.UI.Toolkit.CommandBarExtensions, Uno.UI.Toolkit", "NavigationCommand");
		private static DependencyProperty BackButtonVisibilityProperty = ToolkitHelper.GetProperty("Uno.UI.Toolkit.CommandBarExtensions, Uno.UI.Toolkit", "BackButtonVisibility");
		private static DependencyProperty BackButtonForegroundProperty = ToolkitHelper.GetProperty("Uno.UI.Toolkit.CommandBarExtensions, Uno.UI.Toolkit", "BackButtonForeground");
		private static DependencyProperty BackButtonIconProperty = ToolkitHelper.GetProperty("Uno.UI.Toolkit.CommandBarExtensions, Uno.UI.Toolkit", "BackButtonIcon");

		private static string? _actionBarUpDescription;
		private static string? ActionBarUpDescription
		{
			get
			{
				if (_actionBarUpDescription == null)
				{
					if (ContextHelper.Current is Activity activity
						&& activity.Resources?.GetIdentifier("action_bar_up_description", "string", "android") is { } resourceId)
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
		private Android.Graphics.Drawables.Drawable? _originalBackground;
		private Border? _contentContainer;

		public CommandBarRenderer(CommandBar element) : base(element) { }

		protected override Toolbar CreateNativeInstance() => new Toolbar(ContextHelper.Current);

		protected override IEnumerable<IDisposable> Initialize()
		{
			var native = Native;
			_originalBackground = native.Background;
			_originalTitleTextColor = native.GetTitleTextColor();

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

			var element = Element;
			_contentContainer.SetParent(element);
			native.AddView(_contentContainer);
			yield return Disposable.Create(() => native.RemoveView(_contentContainer));
			yield return _contentContainer.RegisterParentChangedCallback(this, OnContentContainerParentChanged!);

			// Commands.Click
			native.MenuItemClick += Native_MenuItemClick;
			yield return Disposable.Create(() => native.MenuItemClick -= Native_MenuItemClick);

			// NavigationCommand.Click
			native.NavigationClick += Native_NavigationClick;
			yield return Disposable.Create(() => native.NavigationClick -= Native_NavigationClick);

			// Commands
			VectorChangedEventHandler<ICommandBarElement> OnVectorChanged = (s, e) => Invalidate();
			element.PrimaryCommands.VectorChanged += OnVectorChanged;
			element.SecondaryCommands.VectorChanged += OnVectorChanged;
			yield return Disposable.Create(() => element.PrimaryCommands.VectorChanged -= OnVectorChanged);
			yield return Disposable.Create(() => element.SecondaryCommands.VectorChanged -= OnVectorChanged);

			// Properties
			yield return element.RegisterDisposableNestedPropertyChangedCallback(
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
				new[] { CommandBar.HorizontalContentAlignmentProperty },
				new[] { CommandBar.VerticalContentAlignmentProperty },
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
			if (_contentContainer == null)
			{
				throw new InvalidOperationException();
			}
			var native = Native;
			var element = Element;

			// Content
			var content = element.Content;
			native.Title = content as string;
			if (_contentContainer.Child != content && content is UIElement contentAsUIElement && contentAsUIElement.Parent is UIElement parent)
			{
				parent.RemoveChild(contentAsUIElement);
			}

			_contentContainer.Child = content as UIElement;
			_contentContainer.VerticalAlignment = element.VerticalContentAlignment;
			_contentContainer.HorizontalAlignment = element.HorizontalContentAlignment;
			_contentContainer.Visibility = content is UIElement
				? Visibility.Visible
				: Visibility.Collapsed;

			// CommandBarExtensions.Subtitle
			native.Subtitle = element.GetValue(SubtitleProperty) as string;

			// Background
			var backgroundColor = Brush.GetColorWithOpacity(element.Background);
			if (backgroundColor != null)
			{
				native.SetBackgroundColor((Android.Graphics.Color)backgroundColor);
			}
			else
			{
				native.Background = _originalBackground ?? new ColorDrawable(Color.FromArgb(255, 250, 250, 250));
			}

			// Foreground
			var foregroundColor = Brush.GetColorWithOpacity(element.Foreground);
			if (foregroundColor != null)
			{
				native.SetTitleTextColor((Android.Graphics.Color)foregroundColor);
			}
			else if (_originalTitleTextColor != null)
			{
				native.SetTitleTextColor(_originalTitleTextColor.Value);
			}

			// PrimaryCommands & SecondaryCommands
			var currentMenuItemIds = GetMenuItems(native.Menu!)
				.Select(i => i!.ItemId);
			var intendedMenuItemIds = element.PrimaryCommands
				.Concat(element.SecondaryCommands)
				.OfType<AppBarButton>()
				.Select(i => i.GetHashCode());

			if (!currentMenuItemIds.SequenceEqual(intendedMenuItemIds))
			{
				if (native.Menu is not null)
				{
					native.Menu.Clear();
					foreach (var command in element.PrimaryCommands.Concat(element.SecondaryCommands).OfType<AppBarButton>())
					{
#pragma warning disable 618
						var menuItem = native.Menu.Add(0, command.GetHashCode(), Menu.None, null);
#pragma warning restore 618

						var renderer = command.GetRenderer(() => new AppBarButtonRenderer(command));
						renderer.Native = menuItem;

						// This ensures that Behaviors expecting this button to be in the logical tree work. 
						command.SetParent(element);
					}
				}
				else
				{
					if (typeof(CommandBarRenderer).Log().IsEnabled(LogLevel.Error))
					{
						typeof(CommandBarRenderer).Log().Error("Unable to determine ind the native menu.");
					}
				}
			}

			// CommandBarExtensions.NavigationCommand
			if (element.GetValue(NavigationCommandProperty) is AppBarButton navigationCommand)
			{
				var renderer = navigationCommand.GetRenderer(() => new NavigationAppBarButtonRenderer(navigationCommand));
				renderer.Native = native;

				// This ensures that Behaviors expecting this button to be in the logical tree work. 
				navigationCommand.SetParent(element);
			}
			// CommandBarExtensions.BackButtonVisibility
			else if ((Visibility)element.GetValue(BackButtonVisibilityProperty) == Visibility.Visible)
			{
				// CommandBarExtensions.BackButtonIcon
				if (element.GetValue(BackButtonIconProperty) is BitmapIcon bitmapIcon)
				{
					native.NavigationIcon = DrawableHelper.FromUri(bitmapIcon.UriSource);
				}
				else
				{
					native.NavigationIcon = new AndroidX.AppCompat.Graphics.Drawable.DrawerArrowDrawable(ContextHelper.Current)
					{
						// 0 = menu icon
						// 1 = back icon
						Progress = 1,
					};
				}

				// CommandBarExtensions.BackButtonForeground
				var backButtonForeground = Brush.GetColorWithOpacity(element.GetValue(BackButtonForegroundProperty) as Brush);
				if (backButtonForeground != null)
				{
					switch (native.NavigationIcon)
					{
						case AndroidX.AppCompat.Graphics.Drawable.DrawerArrowDrawable drawerArrowDrawable:
							drawerArrowDrawable.Color = (Android.Graphics.Color)backButtonForeground;
							break;
						case Drawable drawable:
							DrawableCompat.SetTint(drawable, (Android.Graphics.Color)backButtonForeground);
							break;
					}
				}

				native.NavigationContentDescription = ActionBarUpDescription;
			}
			else
			{
				native.NavigationIcon = null;
				native.NavigationContentDescription = null;
			}

			// Padding
			var physicalPadding = element.Padding.LogicalToPhysicalPixels();
			native.SetPadding(
				(int)physicalPadding.Left,
				(int)physicalPadding.Top,
				(int)physicalPadding.Right,
				(int)physicalPadding.Bottom
			);

			// Opacity
			native.Alpha = (float)element.Opacity;
		}

		private IEnumerable<IMenuItem?> GetMenuItems(Android.Views.IMenu menu)
		{
			for (int i = 0; i < menu.Size(); i++)
			{
				yield return menu.GetItem(i);
			}
		}

		private void Native_MenuItemClick(object? sender, Toolbar.MenuItemClickEventArgs e)
		{
			CloseKeyboard();

			if (e.Item is not null)
			{
				var hashCode = e.Item.ItemId;
				var appBarButton = Element.PrimaryCommands
					.Concat(Element.SecondaryCommands)
					.OfType<AppBarButton>()
					.FirstOrDefault(c => hashCode == c.GetHashCode());

				appBarButton?.RaiseClick();
			}
			else
			{
				if (typeof(CommandBarRenderer).Log().IsEnabled(LogLevel.Error))
				{
					typeof(CommandBarRenderer).Log().Error("Unable to determine clicked item native ID.");
				}
			}
		}

		private void Native_NavigationClick(object? sender, Toolbar.NavigationClickEventArgs e)
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
			if ((ContextHelper.Current as Activity)?.CurrentFocus is { } focused)
			{
				var imm = ContextHelper.Current.GetSystemService(Context.InputMethodService) as InputMethodManager;
				imm?.HideSoftInputFromWindow(focused.WindowToken, HideSoftInputFlags.None);
			}
		}
	}
}
