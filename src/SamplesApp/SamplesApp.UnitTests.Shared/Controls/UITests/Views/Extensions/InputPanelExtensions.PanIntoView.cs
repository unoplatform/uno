using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace SamplesApp.UnitTests.Controls.UITests.Views.Extensions
{
	public partial class InputPanelExtensions
	{
		private static FrameworkElement _element;

		/// <summary>
		/// Get value of IsPanIntoView
		/// </summary>
		/// <param name="obj">FrameworkElement</param>
		/// <returns>Value of IsPanIntoView</returns>
		public static bool GetIsPanIntoView(DependencyObject obj)
		{
			return (bool)obj.GetValue(IsPanIntoViewProperty);
		}

		/// <summary>
		/// Set value of IsPanIntoView
		/// </summary>
		/// <param name="obj">FrameworkElement</param>
		/// <param name="value">New value of IsPanIntoView</param>
		public static void SetIsPanIntoView(DependencyObject obj, bool value)
		{
			obj.SetValue(IsPanIntoViewProperty, value);
		}

		/// <summary>
		/// Property for IsPanIntoView, which only triggers the first time the FrameworkElement is displayed 
		/// </summary>
		public static DependencyProperty IsPanIntoViewProperty { get; } =
			DependencyProperty.RegisterAttached("IsPanIntoView", typeof(bool), typeof(InputPanelExtensions), new PropertyMetadata(false, IsPanIntoViewChanged));

		/// <summary>
		/// Event raised when IsPanIntoViewChanged is changed. It only triggers once
		/// </summary>
		/// <param name="d">FrameworkElement</param>
		/// <param name="e">Event arguments</param>
		private static void IsPanIntoViewChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			_element = d as FrameworkElement;

			InputPane.GetForCurrentView().Showing += onKeyboardShowing;
			InputPane.GetForCurrentView().Hiding += onKeyboardHidding;
		}

		/// <summary>
		/// Event raised when the keyboard is showing
		/// </summary>
		/// <param name="sender">InputPane</param>
		/// <param name="args">InputPane Event Arguments</param>
		private static void onKeyboardShowing(InputPane sender, InputPaneVisibilityEventArgs args)
		{
			_element.RenderTransform = new TranslateTransform
			{
				Y = -args.OccludedRect.Height
			};
		}

		/// <summary>
		/// Event raised when the keyboard is hidden
		/// </summary>
		/// <param name="sender">InputPane</param>
		/// <param name="args">InputPane Event Arguments</param>
		private static void onKeyboardHidding(InputPane sender, InputPaneVisibilityEventArgs args)
		{
			_element.RenderTransform = new TranslateTransform
			{
				Y = args.OccludedRect.Height
			};
		}
	}
}
