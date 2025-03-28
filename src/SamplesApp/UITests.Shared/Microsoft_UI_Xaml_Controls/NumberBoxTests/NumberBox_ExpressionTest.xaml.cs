using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace UITests.Microsoft_UI_Xaml_Controls.NumberBoxTests
{
	[Sample("NumberBox", "MUX")]
	public sealed partial class NumberBox_ExpressionTest : Page
	{
		const double resetValue = double.NaN;

		private Dictionary<string, double> _expressions;

		public NumberBox_ExpressionTest()
		{
			this.InitializeComponent();

			_expressions = new Dictionary<string, double>
			{ 
			   // Valid expressions. None of these should evaluate to the reset value.
				{ "5", 5 },
				{ "-358", -358 },
				{ "12.34", 12.34 },
				{ "5 + 3", 8 },
				{ "12345 + 67 + 890", 13302 },
				{ "000 + 0011", 11 },
				{ "5 - 3 + 2", 4 },
				{ "3 + 2 - 5", 0 },
				{ "9 - 2 * 6 / 4", 6 },
				{ "9 - -7",  16 },
				{ "9-3*2", 3 },         // no spaces
				{ " 10  *   6  ", 60 }, // extra spaces
				{ "10 /( 2 + 3 )", 2 },
				{ "5 * -40", -200 },
				{ "(1 - 4) / (2 + 1)", -1 },
				{ "3 * ((4 + 8) / 2)", 18 },
				{ "23 * ((0 - 48) / 8)", -138 },
				{ "((74-71)*2)^3", 216 },
				{ "2 - 2 ^ 3", -6 },
				{ "2 ^ 2 ^ 2 / 2 + 9", 17 },
				{ "5 ^ -2", 0.04 },
				{ "5.09 + 14.333", 19.423 },
				{ "2.5 * 0.35", 0.875 },
				{ "-2 - 5", -7 },       // begins with negative number
				{ "(10)", 10 },         // number in parens
				{ "(-9)", -9 },         // negative number in parens
				{ "0^0", 1 },           // who knew?

				// These should not parse, which means they will reset back to the previous value.
				{ "5x + 3y", resetValue },        // invalid chars
				{ "5 + (3", resetValue },         // mismatched parens
				{ "9 + (2 + 3))", resetValue },
				{ "(2 + 3)(1 + 5)", resetValue }, // missing operator
				{ "9 + + 7", resetValue },        // extra operators
				{ "9 - * 7",  resetValue },
				{ "9 - - 7",  resetValue },
				{ "+9", resetValue },
				//{ "1 / 0", resetValue },          // divide by zero

				// These don't currently work, but maybe should.
				{ "-(3 + 5)", resetValue }, // negative sign in front of parens -- should be -8
			};
		}

		private async void RunButtonClick(object sender, RoutedEventArgs e)
		{
			TestsResults.Children.Clear();
			await Task.Yield();
#if HAS_UNO
			RunButton.IsEnabled = false;
			var errorCount = 0;

			try
			{
				Status.Text = "Running";
				Report("Starting tests...");

				foreach (var expression in _expressions)
				{
					TestNumberBox.Text = "";
					await Task.Yield();

					TestNumberBox.Text = expression.Key;
					await Task.Yield();
					try
					{
						if (expression.Value.IsNaN())
						{
							if (TestNumberBox.Text != "")
							{
								throw new InvalidOperationException($"Expected error, found {TestNumberBox.Text}.");
							}
						}
						else
						{
							if (Math.Abs(TestNumberBox.Value - expression.Value) > 0.0001d)
							{
								throw new InvalidOperationException($"Expected {expression.Value}, found {TestNumberBox.Value}.");
							}
						}
						Report($"expression {expression.Key} = {expression.Value} Success.", isFailed: false);
					}
					catch (Exception ex)
					{
						Report($"expression {expression.Key} = {expression.Value} FAILED:  {ex.Message}", isFailed: true);
						errorCount++;
					}

					await Task.Yield();
				}
			}
			finally
			{
				RunButton.IsEnabled = true;
				Status.Text = errorCount == 0 ? "Success" : $"Failure with {errorCount} failed tests.";
			}
#else
			Report("Doesn't work on UWP", isFailed: true);
#endif
		}

		private void Report(string msg, bool? isFailed = null)
		{
			var color = new SolidColorBrush(Colors.LightGray);
			var prefix = "";

			if (isFailed == true)
			{
				color = new SolidColorBrush(Colors.Red);
				prefix = "❌ ";
			}
			else if (isFailed == false)
			{
				color = new SolidColorBrush(Colors.LightGreen);
				prefix = "✔️ ";
			}

			var ctl = new TextBlock
			{
				Text = prefix + msg,
				Foreground = color,
				Margin = new Thickness(8, 0, 0, 0),
				FontFamily = new FontFamily("Courier New"),
				FontSize = 11,
				TextWrapping = TextWrapping.Wrap
			};

			TestsResults.Children.Add(ctl);
			ctl.StartBringIntoView();
		}
	}
}
