#if XAMARIN_IOS
using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI.Common;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;

namespace Windows.UI.Xaml.Controls
{
	//TODO: should inherit from PickerFlyoutBase
	public partial class TimePickerFlyout : Flyout
	{
		private bool _hasAcceptButton = false;

		public TimePickerFlyout()
		{
			InitializeContent();
		}

		private void InitializeContent()
		{
			Content = new TimePickerSelector();
			BindToContent(nameof(Time));
			BindToContent(nameof(ClockIdentifier));

			AttachAcceptCommand((TimePickerSelector)Content);

			this.Closed += (_, __) =>
			{
				// If no accept button is explicitly part of the template, then update the set Time on light dismiss
				if (!_hasAcceptButton)
				{
					(Content as TimePickerSelector)?.UpdateTime();
				}
			};
		}

		protected override Control CreatePresenter()
		{
			var presenter = new TimePickerFlyoutPresenter()
			{
				Style = FlyoutPresenterStyle,
				Content = Content
			};

			AttachAcceptCommand(presenter);

			return presenter;
		}

		/// <summary>
		/// Propagate 2-way binding to property on TimePickerSelector of the same name
		/// </summary>
		/// <param name="propertyName"></param>
		private void BindToContent(string propertyName)
		{
			this.Binding(propertyName, propertyName, Content, BindingMode.TwoWay);
		}

		private void AttachAcceptCommand(IFrameworkElement rootControl)
		{
			var acceptButton = rootControl.FindName("AcceptButton") as Button;
			if (acceptButton != null && acceptButton.Command == null)
			{
				acceptButton.Command = new DelegateCommand(Accept);
				_hasAcceptButton = true;
			}
		}

		private void Accept()
		{
			// Update the bound Time when the flyout is dismissed, as on Windows.
			(Content as TimePickerSelector)?.UpdateTime();
            Hide();
		}
	}
}

#endif