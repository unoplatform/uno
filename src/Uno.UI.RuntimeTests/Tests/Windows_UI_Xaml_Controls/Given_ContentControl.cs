using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.Extensions;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	public partial class Given_ContentControl
	{
#if !WINDOWS_UWP // Cannot create a DataTemplate on UWP
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Binding_Within_Control_Template()
		{
			var tb = new TextBlock()
				.Apply(tb => tb.SetBinding(TextBlock.TextProperty, new Binding { Path = "UserName" }));

			var simpleView = new Grid()
				.Apply(sv => sv.SetBinding(Grid.DataContextProperty, new Binding { Path = "SignIn" }));

			simpleView.Children.Add(tb);

			var contentControl = new ContentControl
			{
				ContentTemplate = new Windows.UI.Xaml.DataTemplate(() => simpleView),
			}.Apply(cc => cc.SetBinding(ContentControl.ContentProperty, new Binding()));

			var grid = new Grid()
			{
				DataContext = new ViewModel(),
			};

			grid.Children.Add(contentControl);
			TestServices.WindowHelper.WindowContent = grid;

			await TestServices.WindowHelper.WaitFor(() => tb.Text == "Steve");
		}

		private class SignInViewModel
		{
			public string UserName { get; set; } = "Steve";
		}

		private class ViewModel
		{
			public SignInViewModel SignIn { get; set; } = new SignInViewModel();
		}
#endif
	}
}
