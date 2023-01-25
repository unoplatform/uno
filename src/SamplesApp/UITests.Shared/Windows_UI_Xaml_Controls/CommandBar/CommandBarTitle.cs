using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.CommandBar
{
	/// <summary>
	/// Control with multiple parts, to test CommandBar
	/// </summary>
	public partial class CommandBarTitle : Control
	{
		public CommandBarTitle()
		{
			this.DefaultStyleKey = typeof(CommandBarTitle);
		}


		public string MainTitle
		{
			get { return (string)GetValue(MainTitleProperty); }
			set { SetValue(MainTitleProperty, value); }
		}

		public static DependencyProperty MainTitleProperty { get; } =
			DependencyProperty.Register("MainTitle", typeof(string), typeof(CommandBarTitle), new PropertyMetadata(string.Empty));

		public string SubTitle1
		{
			get { return (string)GetValue(SubTitle1Property); }
			set { SetValue(SubTitle1Property, value); }
		}

		public static DependencyProperty SubTitle1Property { get; } =
			DependencyProperty.Register("SubTitle1", typeof(string), typeof(CommandBarTitle), new PropertyMetadata(string.Empty));

		public string SubTitle2
		{
			get { return (string)GetValue(SubTitle2Property); }
			set { SetValue(SubTitle2Property, value); }
		}

		public static DependencyProperty SubTitle2Property { get; } =
			DependencyProperty.Register("SubTitle2", typeof(string), typeof(CommandBarTitle), new PropertyMetadata(string.Empty));
	}
}
