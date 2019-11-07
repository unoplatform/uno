#if XAMARIN_IOS
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CoreGraphics;
using UIKit;
using Uno.UI;
using Uno.UI.Common;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;

namespace Windows.UI.Xaml.Controls
{
	public partial class DatePickerFlyout : PickerFlyoutBase
	{
		private bool _isInitialized;

		public DatePickerFlyout()
		{
			Opening += DatePickerFlyout_Opening;
			Closed += DatePickerFlyout_Closed;
		}

		protected override void InitializePopupPanel()
		{
			_popup.PopupPanel = new PickerFlyoutPopupPanel(this)
			{
				Visibility = Visibility.Collapsed,
				Background = SolidColorBrushHelper.Transparent
			};
		}

		/// <summary>
		/// This method sets the Content property of the Flyout.
		/// </summary>
		/// <remarks>
		/// Note that for performance reasons, we don't call it in the constructor. Instead, we wait for the popup to be opening.
		/// The native UIDatePicker contained in the DatePickerSelector is known for being slow in general (https://bugzilla.xamarin.com/show_bug.cgi?id=49469).
		/// Using this strategy means that a page containing a DatePicker will no longer be slowed down by this initialization during the page creation.
		/// Instead, you'll see the delay when opening the DatePickerFlyout for the first time.
		/// This is most notable on pages containing multiple DatePicker controls.
		/// </remarks>
		private void InitializeContent()
		{
			if (_isInitialized)
			{
				return;
			}

			_isInitialized = true;

			Content = new DatePickerSelector()
			{
				MinYear = MinYear,
				MaxYear = MaxYear
			};

			BindToContent("MinYear");
			BindToContent("MaxYear");

			//TODO: support Day/Month/YearVisible (Task 17591)

			AttachAcceptCommand((DatePickerSelector)Content);
		}

		#region Content DependencyProperty
		internal IUIElement Content
		{
			get { return (IUIElement)this.GetValue(ContentProperty); }
			set { this.SetValue(ContentProperty, value); }
		}

		internal static readonly DependencyProperty ContentProperty =
			DependencyProperty.Register(
				"Content",
				typeof(IUIElement),
				typeof(DatePickerFlyout),
				new FrameworkPropertyMetadata(default(IUIElement), FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.ValueInheritsDataContext, OnContentChanged));
		private DatePickerFlyoutPresenter _datePickerPresenter;

		private static void OnContentChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			var flyout = dependencyObject as DatePickerFlyout;

			if (flyout._datePickerPresenter != null)
			{
				if (args.NewValue is IDependencyObjectStoreProvider binder)
				{
					binder.Store.SetValue(binder.Store.TemplatedParentProperty, flyout.TemplatedParent, DependencyPropertyValuePrecedences.Local);
					binder.Store.SetValue(binder.Store.DataContextProperty, flyout.DataContext, DependencyPropertyValuePrecedences.Local);
				}

				flyout._datePickerPresenter.Content = args.NewValue;
			}
		}
		#endregion

		private void DatePickerFlyout_Opening(object sender, EventArgs e)
		{
			InitializeContent();

			// The date coerced by UIDatePicker doesn't propagate back to DatePickerSelector (#137137)
			// When the user selected an invalid date, a `ValueChanged` will be raised after coercion to propagate the coerced date.
			// However, when the `Date` is set below, there no `ValueChanged` to propagate the coerced date.
			// To address this, we clamp the date between the valid range.
			var validDate = Date;
			validDate = validDate > MaxYear ? MaxYear : validDate;
			validDate = validDate < MinYear ? MinYear : validDate;

			if (UIKit.UIDevice.CurrentDevice.CheckSystemVersion(10, 0))
			{
				((DatePickerSelector)Content).Date = validDate;
			}
			else
			{
				// This is a workaround for the datepicker crashing on iOS 9 with this error:
				// NSInternalInconsistencyException Reason: UITableView dataSource is not set.
				this.Dispatcher.RunAsync(Core.CoreDispatcherPriority.Normal, async () =>
				{
					await Task.Delay(100);
					((DatePickerSelector)Content).Date = validDate;
				});
			}
		}

		private void DatePickerFlyout_Closed(object sender, EventArgs e)
		{
			Date = ((DatePickerSelector)Content).Date;
		}

		protected override Control CreatePresenter()
		{
			_datePickerPresenter = new DatePickerFlyoutPresenter()
			{
				Content = Content
			};

			AttachAcceptCommand(_datePickerPresenter);

			return _datePickerPresenter;
		}

		private void AttachAcceptCommand(IFrameworkElement rootControl)
		{
			var acceptButton = rootControl.FindName("AcceptButton") as Button;
			if (acceptButton != null && acceptButton.Command == null)
			{
				acceptButton.Command = new DelegateCommand(Hide);
			}
		}

		private void BindToContent(string propertyName)
		{
			this.Binding(propertyName, propertyName, Content, BindingMode.TwoWay);
		}
	}
}

#endif
