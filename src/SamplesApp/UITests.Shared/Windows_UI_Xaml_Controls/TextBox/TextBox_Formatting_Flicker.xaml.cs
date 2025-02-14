using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Uno.Extensions;
using Uno.UI.Extensions;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Uno.UI.Samples.Controls;

#if __ANDROID__
using Android.Text;
using Java.Lang;
#endif

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Controls.TextBoxTests
{
	[SampleControlInfo("TextBox", "TextBox_Formatting_Flicker", description: "Continuing to enter value past the max length specified, should not cause the text box content to be changed again.")]
	public sealed partial class TextBox_Formatting_Flicker : UserControl
	{
		private int _textChangedCounter = 0;

		public TextBox_Formatting_Flicker()
		{
			this.InitializeComponent();

			SomeTextBox.MaxLength = $"modified {0:D3} times".Length;
			SomeTextBox.TextChanged += (s, e) => Scoreboard.Text = $"TextChanged: {++_textChangedCounter}";
		}

#if __ANDROID__
		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			var filters = new IInputFilter[] { new CustomInputFilter() };

			var view = SomeTextBox.FindFirstChild<TextBoxView>();
			if (view != null)
			{
				SetFilter(view);
			}
			else
			{
				SomeTextBox.Loaded += (s, e) =>
				{
					SomeTextBox.ApplyTemplate();
					SomeTextBox.FindFirstChild<TextBoxView>()?.Apply(SetFilter);
				};
			}

			void SetFilter(TextBoxView tbv) => tbv.SetFilters(new IInputFilter[] { new CustomInputFilter() });
		}

		private class CustomInputFilter : Java.Lang.Object, Android.Text.IInputFilter
		{
			private int _counter = 0;

			public ICharSequence FilterFormatted(ICharSequence source, int start, int end, ISpanned dest, int dstart, int dend)
			{
				return new Java.Lang.String($"modified {++_counter:D3} times");
			}
		}
#endif
	}
}
