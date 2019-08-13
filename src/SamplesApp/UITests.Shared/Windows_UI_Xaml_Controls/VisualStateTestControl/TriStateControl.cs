using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
#if NETFX_CORE
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
#elif XAMARIN
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.Globalization;
#endif

namespace UITests.Shared.Windows_UI_Xaml_Controls.VisualStateTestControl
{
	[TemplateVisualState(GroupName = __TriStates, Name = __First)]
	[TemplateVisualState(GroupName = __TriStates, Name = __Second)]
	[TemplateVisualState(GroupName = __TriStates, Name = __Third)]
	public partial class TriStateControl : Control
	{
		private const string __TriStates = "TriStates";
		private const string __First = "First";
		private const string __Second = "Second";
		private const string __Third = "Third";

		public bool IsFirst
		{
			get { return (bool)GetValue(IsFirstProperty); }
			set { SetValue(IsFirstProperty, value); }
		}

		public static readonly DependencyProperty IsFirstProperty =
			DependencyProperty.Register("IsFirst", typeof(bool), typeof(TriStateControl), new PropertyMetadata(true, (s, e) => ((TriStateControl)s).UpdateTriState(true)));

		public bool IsSecond
		{
			get { return (bool)GetValue(IsSecondProperty); }
			set { SetValue(IsSecondProperty, value); }
		}

		public static readonly DependencyProperty IsSecondProperty =
			DependencyProperty.Register("IsSecond", typeof(bool), typeof(TriStateControl), new PropertyMetadata(false, (s, e) => ((TriStateControl)s).UpdateTriState(true)));

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			UpdateTriState(false);
		}

		private void UpdateTriState(bool useTransitions)
		{
			if (IsFirst)
			{
				VisualStateManager.GoToState(this, __First, useTransitions);
				return;
			}

			if (IsSecond)
			{
				VisualStateManager.GoToState(this, __Second, useTransitions);
				return;
			}

			VisualStateManager.GoToState(this, __Third, useTransitions);
		}
	}
}
