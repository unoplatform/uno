using Uno.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.Logging;

namespace Uno.UI.Samples.Content.UITests.ContentControlTestsControl
{
	public partial class FindNameTestControl : Control
	{
		private static readonly string[] ButtonNames = { "ButtonOutsideScrollViewer", "ButtonInsideScrollViewer", "ButtonInsideContentControl" };

		public FindNameTestControl()
		{

		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			var control = this;
			foreach (var buttonName in ButtonNames)
			{
				var button = control.GetTemplateChild(buttonName) as Button;
				if (button != null)
				{
					button.Content = buttonName + "OnApply";
				}
				else
				{
					this.Log().Warn($"{buttonName} template part not found.");
				}
			}
		}
	}
}
