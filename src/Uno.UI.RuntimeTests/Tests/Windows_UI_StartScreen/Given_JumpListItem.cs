#if __ANDROID__ || __IOS__ || WINAPPSDK
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.StartScreen;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_StartScreen
{
	[TestClass]
	public class Given_JumpListItem
	{
		[TestMethod]
		public void When_Arguments_Are_Null()
		{
			Assert.ThrowsException<ArgumentNullException>(
				() => JumpListItem.CreateWithArguments(null, "Hello"));
		}

		[TestMethod]
		public void When_Arguments_Are_Empty()
		{
			var item = JumpListItem.CreateWithArguments(string.Empty, "Hello");
			Assert.AreEqual(string.Empty, item.Arguments);
		}

		[TestMethod]
		public void When_DisplayName_Is_Null()
		{
			Assert.ThrowsException<ArgumentNullException>(
				() => JumpListItem.CreateWithArguments("Hello", null));
		}

		[TestMethod]
		public void When_DisplayName_Is_Empty()
		{
			var item = JumpListItem.CreateWithArguments("Hello", string.Empty);
			Assert.AreEqual(string.Empty, item.DisplayName);
		}

		[TestMethod]
		public void When_DisplayName_Is_Set()
		{
			var item = JumpListItem.CreateWithArguments("Hello", "test");
			Assert.AreEqual("test", item.DisplayName);
		}

		[TestMethod]
		public void When_Arguments_Are_Set()
		{
			var item = JumpListItem.CreateWithArguments("Hello", "test");
			Assert.AreEqual("Hello", item.Arguments);
		}

		[TestMethod]
		public void When_Description_Is_Set_To_Null()
		{
			var item = JumpListItem.CreateWithArguments("Hello", "test");
			Assert.ThrowsException<ArgumentNullException>(
				() => item.Description = null);
		}

		[TestMethod]
		public void When_Instance_Is_Created_Default_Values()
		{
			var item = JumpListItem.CreateWithArguments("Hello", "test");
			Assert.AreEqual(string.Empty, item.Description);
			Assert.AreEqual(null, item.Logo);
			Assert.AreEqual(false, item.RemovedByUser);
			Assert.AreEqual(JumpListItemKind.Arguments, item.Kind);
		}

		[TestMethod]
		public void When_Logo_Uri_Is_Web()
		{
			var item = JumpListItem.CreateWithArguments("Hello", "Test");
			Assert.ThrowsException<ArgumentException>(
				() => item.Logo = new Uri("https://www.example.com/image.png"));
		}

		[TestMethod]
		public void When_Logo_Uri_Is_File_Path()
		{
			var item = JumpListItem.CreateWithArguments("Hello", "Test");
			Assert.ThrowsException<ArgumentException>(
				() => item.Logo = new Uri("C:\\Dev\\Hello.png"));
		}

		[TestMethod]
		public void When_Logo_Uri_Is_Set_To_Null()
		{
			var item = JumpListItem.CreateWithArguments("Hello", "Test");
			item.Logo = null;
			Assert.AreEqual(null, item.Logo);
		}

		[TestMethod]
		public void When_Logo_Uri_Is_ms_appdata()
		{
			var item = JumpListItem.CreateWithArguments("Hello", "Test");
			Assert.ThrowsException<ArgumentException>(
				() => item.Logo = new Uri("ms-appdata:///Test.png"));
		}

		[TestMethod]
		public void When_Logo_Uri_Is_ms_appx()
		{
			var item = JumpListItem.CreateWithArguments("Hello", "Test");
			item.Logo = new Uri("ms-appx:///Test.png");
			Assert.AreEqual("ms-appx:///Test.png", item.Logo.AbsoluteUri);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Description_Is_Empty_No_Exception()
		{
			try
			{
				var item = JumpListItem.CreateWithArguments("Hello", "Test");
				item.Description = string.Empty;
				var jumpList = await JumpList.LoadCurrentAsync();
				jumpList.Items.Clear();
				jumpList.Items.Add(item);
				await jumpList.SaveAsync();
				// reload from native
				var newJumpList = await JumpList.LoadCurrentAsync();
				Assert.AreEqual(string.Empty, newJumpList.Items.First().Description);
			}
			finally
			{
				var jumpList = await JumpList.LoadCurrentAsync();
				jumpList.Items.Clear();
				await jumpList.SaveAsync();
			}
		}
	}
}
#endif
