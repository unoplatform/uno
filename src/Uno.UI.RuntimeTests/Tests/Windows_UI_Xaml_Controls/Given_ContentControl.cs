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
#if NETFX_CORE
using Uno.UI.Extensions;
#elif __IOS__
using UIKit;
#elif __MACOS__
using AppKit;
#else
using Uno.UI;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public partial class Given_ContentControl
	{
		private ResourceDictionary _testsResources;

		public DataTemplate DataContextBindingDataTemplate => _testsResources["DataContextBindingDataTemplate"] as DataTemplate;

		[TestInitialize]
		public void Init()
		{
			_testsResources = new TestsResources();
		}

		[TestMethod]
		public async Task When_Binding_Within_Control_Template()
		{
			var contentControl = new ContentControl
			{
				ContentTemplate = DataContextBindingDataTemplate,
			}.Apply(cc => cc.SetBinding(ContentControl.ContentProperty, new Binding()));

			var grid = new Grid()
			{
				DataContext = new ViewModel(),
			};

			grid.Children.Add(contentControl);
			TestServices.WindowHelper.WindowContent = grid;

			await TestServices.WindowHelper.WaitForLoaded(grid);

			var tb = grid.FindFirstChild<TextBlock>();

			Assert.IsNotNull(tb);

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
	}
}
