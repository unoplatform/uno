using Uno.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.Foundation.Logging;

namespace Uno.UI.Samples.Content.UITests.ContentControlTestsControl
{
	public partial class FindNameTestControl : Control
	{
#pragma warning disable CS0109 
		private new readonly Logger _log = Uno.Foundation.Logging.LogExtensionPoint.Log(typeof(FindNameTestControl));
#pragma warning restore CS0109

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
					_log.Warn($"{buttonName} template part not found.");
				}
			}
		}
	}
}
