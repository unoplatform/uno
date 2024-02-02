// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using MUXControlsTestApp.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation.Metadata;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Common;
using Private.Infrastructure;
#if USING_TAEF
using WEX.TestExecution;
using WEX.TestExecution.Markup;
using WEX.Logging.Interop;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
#endif

namespace MUXControlsTestApp
{
	// This type is used for Interaction tests that do verification and log results to a textbox which the
	// MITA code can read back.
	public class ResultsLogger : IDisposable
	{
		string _stateName;
		List<string> errors = new List<string>();
		List<string> messages = new List<string>();
		TextBox _testResult;

		public ResultsLogger(string stateName, TextBox testResult)
		{
			_stateName = stateName;
			_testResult = testResult;
		}

		public void Dispose()
		{
			if (errors.Count == 0)
			{
				_testResult.Text = _stateName + ": Passed";
			}
			else
			{
				_testResult.Text = _stateName + ": Failed: " + String.Join(", ", errors);
			}

			_testResult.Text += (" [" + String.Join(", ", messages) + "]");
		}

		public void LogError(string error)
		{
			errors.Add(error);
		}

		public void LogMessage(string message)
		{
			messages.Add(message);
		}

		public void Verify(bool condition, string message)
		{
			if (!condition)
			{
				LogError(message);
			}
		}
	}

	public static class ComboBoxExtensions
	{
		public static string GetSelectedText(this ComboBox comboBox)
		{
			var item = comboBox.SelectedItem as ComboBoxItem;
			if (item != null)
			{
				return (string)item.Content;
			}

			return "";
		}
	}

}

namespace MUXControlsTestApp
{
	public class App
	{
		public static UIElement TestContentRoot
		{
			get => TestServices.WindowHelper.WindowContent as UIElement;
			set => TestServices.WindowHelper.WindowContent = value;
		}

		public static Application Current => Application.Current;
	}
}

namespace MUXControlsTestApp.Utilities
{
	public static class TestUtilities
	{
		public static int DefaultWaitMs = Debugger.IsAttached ? 120000 : 5000;

		public static void SetAsVisualTreeRoot(FrameworkElement element)
		{
			IdleSynchronizer.Wait();

			AutoResetEvent loadedEvent = new AutoResetEvent(false);

			RunOnUIThread.Execute(() =>
			{
				element.Loaded += (sender, args) => loadedEvent.Set();
				Log.Comment("Setting visual tree root and waiting for Loaded to be raised...");
				MUXControlsTestApp.App.TestContentRoot = element;
			});

			WaitForEvent(loadedEvent);
			IdleSynchronizer.Wait();
			Log.Comment("Loaded raised.");
		}

		public static void ClearVisualTreeRoot()
		{
			IdleSynchronizer.Wait();

			AutoResetEvent unloadedEvent = new AutoResetEvent(false);

			RunOnUIThread.Execute(() =>
			{
				FrameworkElement rootElement = MUXControlsTestApp.App.TestContentRoot as FrameworkElement;

				if (rootElement != null
				//UNO: // In MUXControlsTestApp.App.TestContentRoot setter, we set the content to MainPage if value is null.
				//UNO: // Therefore UnloadedEvent will never be triggered if rootElement type is MainPage, no need to add event here.
				//UNO: && !(rootElement is MainPage)
				)
				{
					rootElement.Unloaded += (sender, args) => unloadedEvent.Set();
				}
				else
				{
					// If we don't have a FrameworkElement root, then we'll just set the unloaded event
					// to make sure that we don't wait on an event that won't actually come.
					unloadedEvent.Set();
				}

				Log.Comment("Clearing visual tree root and waiting for Unloaded to be raised...");
				MUXControlsTestApp.App.TestContentRoot = null;

			});

			WaitForEvent(unloadedEvent);
			IdleSynchronizer.Wait();
			Log.Comment("Unloaded raised.");
		}

		public static void WaitForEvent(AutoResetEvent e)
		{
			if (!e.WaitOne(TimeSpan.FromMilliseconds(DefaultWaitMs)))
			{
				throw new("Event was not raised.");
			}
		}

		public static IList<T> FindDescendents<T>(DependencyObject root)
#if WINAPPSDK
			where T : DependencyObject
#else
			where T : class, DependencyObject
#endif
		{
			List<T> descendentsList = new List<T>();

			T rootAsT = root as T;
			if (rootAsT != null)
			{
				descendentsList.Add(rootAsT);
			}

			// Popup is a special case as its content isn't actually stored as a visual child of Popup itself -
			// instead, its content is added as a child of the popup root.  As such, we need to special-case Popup.
			Popup popup = root as Popup;

			if (popup != null)
			{
				descendentsList.AddRange(FindDescendents<T>(popup.Child));
			}
			else
			{
				for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
				{
					descendentsList.AddRange(FindDescendents<T>(VisualTreeHelper.GetChild(root, i)));
				}
			}

			return descendentsList;
		}

		public static string ProcessTestXamlForRepo(string xamlString)
		{
			return xamlString;
		}
	}

	public static class VisualStateHelper
	{
		public static string GetVisualStateGroupsNameString(FrameworkElement element, String separator = " ")
		{
			return string.Join(separator, GetVisualStateGroupsName(element));
		}

		public static string GetCurrentVisualStateNameString(FrameworkElement element, String separator = " ")
		{
			return string.Join(separator, GetCurrentVisualStateName(element));
		}

		private static FrameworkElement GetAndAssertFirstChildAsFrameworkElement(FrameworkElement element)
		{
			var child = (FrameworkElement)VisualTreeHelper.GetChild(element, 0);
			if (child == null)
			{
				throw new("First child doesn't exist or is not a FrameworkElement");
			}
			return child;
		}

		public static IEnumerable<string> GetVisualStateGroupsName(FrameworkElement element)
		{
			var groups = VisualStateManager.GetVisualStateGroups(GetAndAssertFirstChildAsFrameworkElement(element));
			return groups.Where(group => group != null && group.Name != null).
				Select(group => group.Name);
		}

		public static IEnumerable<string> GetCurrentVisualStateName(FrameworkElement element)
		{
			var groups = VisualStateManager.GetVisualStateGroups(GetAndAssertFirstChildAsFrameworkElement(element));
			return groups.Where(group => group != null && group.CurrentState != null).
				Select(group => group.CurrentState.Name);
		}

		public static bool ContainsVisualState(FrameworkElement element, string visualStateName)
		{
			return GetCurrentVisualStateName(element).Contains(visualStateName);
		}

		public static bool ContainsVisualStateGroup(FrameworkElement element, string visualStateGroupName)
		{
			return GetVisualStateGroupsName(element).Contains(visualStateGroupName);
		}
	}
}
