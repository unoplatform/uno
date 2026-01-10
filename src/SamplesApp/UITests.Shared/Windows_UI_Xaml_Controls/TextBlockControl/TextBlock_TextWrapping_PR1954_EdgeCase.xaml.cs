using System;
using System.Linq;
using System.Reflection;
using Uno.UI;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

#if __ANDROID__
using Android.Text;
#endif

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236
namespace UITests.Shared.Windows_UI_Xaml_Controls.TextBlockControl
{
	[Sample("TextBlock", nameof(TextBlock_TextWrapping_PR1954_EdgeCase), description: "[Droid]Repro sample for PR1954. When the text is limited to 1 line, due to enough height available for 2 lines, it should take the size of 1 line only. You may need to adjust text lenght and/or container height depending on the device. Expected behavior: text should always be in the center of pink area")]
	public sealed partial class TextBlock_TextWrapping_PR1954_EdgeCase : UserControl
	{
		public TextBlock_TextWrapping_PR1954_EdgeCase()
		{
			this.InitializeComponent();

#if __ANDROID__
			if (TryCalculateTextLinePhysicalHeight() is int physicalHeight && physicalHeight > 0)
			{
				// set container height to a value, plenty for 1 line, but not enough for 2 lines
				this.SampleContainer.Height = ViewHelper.PhysicalToLogicalPixels(physicalHeight) * 1.85;
			}
#endif
		}

#if __ANDROID__
		private int? TryCalculateTextLinePhysicalHeight()
		{
			try
			{
				TextBlock_UpdateLayout(this.SampleText, out var layoutBuilder, new Size(double.PositiveInfinity, double.PositiveInfinity), false);
				var layout = LayoutBuilder_get_Layout(layoutBuilder);

				return layout.GetLineTop(1);
			}
			catch (Exception)
			{
				return default;
			}

			Size TextBlock_UpdateLayout(TextBlock target, out object layoutBuilder, Size availableSize, bool exactWidth)
			{
				var parameterTypes = new[]
				{
					typeof(TextBlock).GetNestedType("LayoutBuilder", BindingFlags.NonPublic).MakeByRefType(),
					typeof(Size),
					typeof(bool),
				};
				var parameters = new object[]
				{
					default,
					new Size(double.PositiveInfinity, double.PositiveInfinity),
					false
				};

				var result = typeof(TextBlock)
					.GetMethod("UpdateLayout", BindingFlags.NonPublic | BindingFlags.Instance, default, parameterTypes, default)
					.Invoke(target, parameters);

				layoutBuilder = parameters[0];
				return (Size)result;
			}
			global::Android.Text.Layout LayoutBuilder_get_Layout(object builder) => (global::Android.Text.Layout)typeof(TextBlock).GetNestedType("LayoutBuilder", BindingFlags.NonPublic)
				.GetProperty("Layout")
				.GetValue(builder);
		}
#endif



		private void AdjustTextLength(object sender, RoutedEventArgs e)
		{
			if (GetButtonDirection(sender) is bool direction)
			{
				var text = SampleText.Text;
				if (text.Length > 0 || direction)
				{
					SampleText.Text = direction
						? text + "Xy"[text.Length % 2]
						: text.Substring(0, text.Length - 1);
				}
			}
		}

		private void AdjustContainerHeight(object sender, RoutedEventArgs e)
		{
			if (GetButtonDirection(sender) is bool direction)
			{
				if (SampleContainer.Height + (direction ? 5 : -5) is var finalHeight && finalHeight > 0)
				{
					SampleContainer.Height = finalHeight;
				}
			}
		}

		private bool? GetButtonDirection(object sender)
		{
			switch ((sender as Microsoft.UI.Xaml.Controls.Button)?.Content)
			{
				case "+": return true;
				case "-": return false;

				default: return default;
			}
		}
	}
}
