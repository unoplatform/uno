using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MUXControlsTestApp.Utilities;
using Uno.Disposables;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;
using Uno.Extensions;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.MUX.Helpers
{
	internal static class ControlHelper
	{
		internal static async Task DoClickUsingTap(ButtonBase button)
		{
			var clickEvent = new TaskCompletionSource<object>();

			void OnButtonOnClick(object sender, RoutedEventArgs args)
			{
				clickEvent.TrySetResult(null);
			}


			await RunOnUIThread.ExecuteAsync(async () =>
			{
				button.Click += OnButtonOnClick;

				await InputHelper.Tap(button);

			});

			using var _ = Disposable.Create(() => button.Click -= OnButtonOnClick);

			await clickEvent.Task;
		}

		internal static async Task DoClickUsingAP(ButtonBase button)
		{
			var clickEvent = new TaskCompletionSource<object>();

			void OnButtonOnClick(object sender, RoutedEventArgs args)
			{
				clickEvent.TrySetResult(null);
			}


			await RunOnUIThread.ExecuteAsync(() =>
			{
				button.Click += OnButtonOnClick;

				var buttonAP = FrameworkElementAutomationPeer.CreatePeerForElement(button) as ButtonAutomationPeer;

				buttonAP.Invoke();
			});

			using var _ = Disposable.Create(() => button.Click -= OnButtonOnClick);

			await clickEvent.Task;
		}


		public static async Task ClickFlyoutCloseButton(DependencyObject element, bool isAccept)
		{
			// The Flyout close button could either be a part of the Flyout itself or it could be in the AppBar
			// We look in both places.
			ButtonBase button = default;

			await RunOnUIThread.ExecuteAsync(() =>
			{
				string buttonName = isAccept ? "AcceptButton" : "DismissButton";

				button = TreeHelper.GetVisualChildByNameFromOpenPopups(buttonName, element) as ButtonBase;
				;

				//if (button == null)
				//{
				//	var cmdBar = TestServices.Utilities.RetrieveOpenBottomCommandBar();
				//	if (cmdBar != null)
				//	{
				//		button = cmdBar.PrimaryCommands[isAccept ? 0 : 1] as ButtonBase;
				//	}
				//}
			});

			Assert.IsNotNull(button, "Close button not found");

			await DoClickUsingAP(button);
		}

		public static void ValidateUIElementTree(
			Size windowSizeOverride,
			double scale,
			Func<Task<Panel>> setup,
			Func<Task> cleanup = null,
			bool disableHittestingOnRoot = true,
			bool ignorePopups = false)
		{
			throw new NotImplementedException();
		}

		public static async Task<Rect> GetBounds(FrameworkElement element)
		{
			var rect = new Rect();
			await RunOnUIThread.ExecuteAsync(() =>
			{
				var point1 = element.TransformToVisual(null).TransformPoint(new Point(0, 0));
				var point2 = element.TransformToVisual(null).TransformPoint(new Point(element.ActualWidth, element.ActualHeight));

				rect.X = Math.Min(point1.X, point2.X);
				rect.Y = Math.Min(point1.Y, point2.Y);
				rect.Width = Math.Abs(point1.X - point2.X);
				rect.Height = Math.Abs(point1.Y - point2.Y);
			});

			return rect;
		}

		public static async Task<bool> IsInVisualState(Control control, string visualStateGroupName, string visualStateName)
		{
			bool result = false;
			await RunOnUIThread.ExecuteAsync(() =>
			{
				var rootVisual = (FrameworkElement)VisualTreeHelper.GetChild(control, 0);
				var visualStateGroup = GetVisualStateGroup(rootVisual, visualStateGroupName);
				result = visualStateGroup != null && visualStateName == visualStateGroup.CurrentState.Name;
			});
			return result;
		}

		public static void RemoveItem<T>(IList<T> items, T item)
		{
			var index = items?.IndexOf(item) ?? -1;
			if (index == -1)
			{
				throw new ArgumentOutOfRangeException("The item was not in the collection.");
			}
			items.RemoveAt(index);
		}

		private static VisualStateGroup GetVisualStateGroup(FrameworkElement control, string groupName)
		{
			VisualStateGroup group = null;
			var visualStateGroups = VisualStateManager.GetVisualStateGroups(control);
			if (visualStateGroups == null && control is ContentControl contentControl)
			{
				visualStateGroups = VisualStateManager.GetVisualStateGroups(contentControl);
			}

			if (visualStateGroups == null)
			{
				return group;
			}

			foreach (var visualStateGroup in visualStateGroups)
			{
				if (visualStateGroup.Name == groupName)
				{
					group = visualStateGroup;
					return group;
				}
			}
			return group;
		}

		internal static async Task<Point> GetCenterOfElementAsync(FrameworkElement element)
		{
			Point offsetFromCenter = default;

			return await GetOffsetCenterOfElementAsync(element, offsetFromCenter);
		}

		static async Task<Point> GetOffsetCenterOfElementAsync(FrameworkElement element, Point offsetFromCenter)
		{
			Point result = default;
			await RunOnUIThread(() =>
			{
				var offsetCenterLocal = new Point((element.ActualWidth / 2) + offsetFromCenter.X, (element.ActualHeight / 2) + offsetFromCenter.Y);
				result = element.TransformToVisual(null).TransformPoint(offsetCenterLocal);
			});
			return result;
		}
	}
}
