using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_ViewManagement
{
	[SampleControlInfo("Windows.UI.ViewManagement", "TitleBar_Color", description: "Allows changing title bar color dynamically")]
	public sealed partial class TitleBarColorTests : UserControl
	{
		public TitleBarColorTests()
		{
			this.InitializeComponent();
		}

		private void SetColor_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var colorString = StringToColor(BackgroundColorTextBox.Text);
				ApplicationView.GetForCurrentView().TitleBar.BackgroundColor = colorString;
			}
			catch (Exception ex)
			{
				ErrorMessage.Text = ex.Message;
			}
		}

		private void ResetDefault_Click(object sender, RoutedEventArgs e)
		{
			ApplicationView.GetForCurrentView().TitleBar.BackgroundColor = null;
		}

		/// <summary>
		/// ToColor method from the Windows Community Toolkit <see href="https://github.com/windows-toolkit/WindowsCommunityToolkit/blob/master/Microsoft.Toolkit.Uwp/Helpers/ColorHelper.cs"/>
		/// </summary>
		/// <param name="colorString">Color string</param>
		/// <returns>Windows Color</returns>
		private static Color StringToColor(string colorString)
		{
			if (string.IsNullOrEmpty(colorString))
			{
				throw new ArgumentException(nameof(colorString));
			}

			if (colorString[0] == '#')
			{
				switch (colorString.Length)
				{
					case 9:
						{
							var cuint = Convert.ToUInt32(colorString.Substring(1), 16);
							var a = (byte)(cuint >> 24);
							var r = (byte)((cuint >> 16) & 0xff);
							var g = (byte)((cuint >> 8) & 0xff);
							var b = (byte)(cuint & 0xff);

							return Color.FromArgb(a, r, g, b);
						}

					case 7:
						{
							var cuint = Convert.ToUInt32(colorString.Substring(1), 16);
							var r = (byte)((cuint >> 16) & 0xff);
							var g = (byte)((cuint >> 8) & 0xff);
							var b = (byte)(cuint & 0xff);

							return Color.FromArgb(255, r, g, b);
						}

					case 5:
						{
							var cuint = Convert.ToUInt16(colorString.Substring(1), 16);
							var a = (byte)(cuint >> 12);
							var r = (byte)((cuint >> 8) & 0xf);
							var g = (byte)((cuint >> 4) & 0xf);
							var b = (byte)(cuint & 0xf);
							a = (byte)(a << 4 | a);
							r = (byte)(r << 4 | r);
							g = (byte)(g << 4 | g);
							b = (byte)(b << 4 | b);

							return Color.FromArgb(a, r, g, b);
						}

					case 4:
						{
							var cuint = Convert.ToUInt16(colorString.Substring(1), 16);
							var r = (byte)((cuint >> 8) & 0xf);
							var g = (byte)((cuint >> 4) & 0xf);
							var b = (byte)(cuint & 0xf);
							r = (byte)(r << 4 | r);
							g = (byte)(g << 4 | g);
							b = (byte)(b << 4 | b);

							return Color.FromArgb(255, r, g, b);
						}

					default:
						throw new FormatException(string.Format("The {0} string passed in the colorString argument is not a recognized Color format.", colorString));
				}
			}

			if (colorString.Length > 3 && colorString[0] == 's' && colorString[1] == 'c' && colorString[2] == '#')
			{
				var values = colorString.Split(',');

				if (values.Length == 4)
				{
					var scA = double.Parse(values[0].Substring(3));
					var scR = double.Parse(values[1]);
					var scG = double.Parse(values[2]);
					var scB = double.Parse(values[3]);

					return Color.FromArgb((byte)(scA * 255), (byte)(scR * 255), (byte)(scG * 255), (byte)(scB * 255));
				}

				if (values.Length == 3)
				{
					var scR = double.Parse(values[0].Substring(3));
					var scG = double.Parse(values[1]);
					var scB = double.Parse(values[2]);

					return Color.FromArgb(255, (byte)(scR * 255), (byte)(scG * 255), (byte)(scB * 255));
				}

				throw new FormatException(string.Format("The {0} string passed in the colorString argument is not a recognized Color format (sc#[scA,]scR,scG,scB).", colorString));
			}

			var prop = typeof(Colors).GetTypeInfo().GetDeclaredProperty(colorString);

			if (prop != null)
			{
				return (Color)prop.GetValue(null);
			}

			throw new FormatException(string.Format("The {0} string passed in the colorString argument is not a recognized Color.", colorString));
		}

	}
}
