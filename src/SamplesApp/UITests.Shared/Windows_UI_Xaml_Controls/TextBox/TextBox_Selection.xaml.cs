using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Uno.Disposables;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Uno.Extensions;
using Uno.UI.Common;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Uno.UI.Samples.Content.UITests.TextBoxControl
{
	[SampleControlInfoAttribute("TextBox", "TextBox_Selection", typeof(Uno.UI.Samples.Presentation.SamplePages.TextBoxViewModel))]
	public sealed partial class TextBox_Selection : UserControl
    {
		private SerialDisposable _subscriptions = new SerialDisposable();

		public TextBox_Selection()
        {
            this.InitializeComponent();

#if XAMARIN_IOS || XAMARIN_ANDROID
			var textBox = new TextBox() { PlaceholderText = "Selection", AcceptsReturn = true };
			var startTextBox = new TextBox() { PlaceholderText = "Start" };
			var lengthTextBox = new TextBox() { PlaceholderText = "Length" };
			var update = new Button()
			{
				Content = "Update",
				Command = new Common.DelegateCommand(() =>
				{
					int start, length;
					int.TryParse(startTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out start);
					int.TryParse(lengthTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out length);

					textBox.SelectionStart = start;
					textBox.SelectionLength = length;
				})
			};

			Observable
				.FromEventPattern(textBox, "SelectionChanged", Schedulers.Default)
				.Subscribe(
					_ =>
					{
						startTextBox.Text = textBox.SelectionStart.ToString();
						lengthTextBox.Text = textBox.SelectionLength.ToString();
					},
					ex => { ex.ToString(); }
				)
				.DisposeWith(_subscriptions);

			Content = new StackPanel
			{
				Children =
				{
					textBox,
					startTextBox,
					lengthTextBox,
					update
				}
			};
		}


        protected override void OnUnloaded()
        {
            base.OnUnloaded();

            _subscriptions.Dispose();
#endif
		}
	}
}
