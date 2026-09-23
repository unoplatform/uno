using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.RadioButtonPages;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	public partial class Given_RadioButton
	{
		private static async Task<RadioButton> FindRadio(RadioButton_TemplateSwap page, string label)
		{
			RadioButton radio = null;
			await UITestHelper.WaitFor(() => (radio = page.EnumerateDescendants().OfType<RadioButton>().FirstOrDefault(r => Equals(r.Content, label))) is not null, 3000, $"RadioButton '{label}'");
			return radio;
		}

		private static async Task<CheckBox> FindCheckBox(RadioButton_TemplateSwap page, string label)
		{
			CheckBox checkBox = null;
			await UITestHelper.WaitFor(() => (checkBox = page.EnumerateDescendants().OfType<CheckBox>().FirstOrDefault(c => Equals(c.Content, label))) is not null, 3000, $"CheckBox '{label}'");
			return checkBox;
		}

		/// <summary>
		/// The native WinUI head never applies the ContentTemplateSelector of this page's presenter (it shows the default
		/// TextBlock even though the selector returns a template), so the selector path is only exercised on Uno.
		/// </summary>
		private static void SkipSelectorPathOnNativeWinUI()
		{
#if !HAS_UNO
			Assert.Inconclusive("The ContentTemplateSelector path is not applied by the WinUI head in this page.");
#endif
		}

		[TestMethod]
		[RunsOnUIThread]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24354")]
		public async Task When_Radio_Template_Replaced_By_CheckBox_Template()
		{
			SkipSelectorPathOnNativeWinUI();

			var radioField = new PickerModel(PickerKind.Radio, "Radio", "Small", "Medium", "Large");
			var checkBoxField = new PickerModel(PickerKind.CheckBox, "CheckBox", "A", "B", "C");

			var page = new RadioButton_TemplateSwap();

			try
			{
				await UITestHelper.Load(page, x => x.IsLoaded);
				page.Presenter.Content = radioField;

				(await FindRadio(page, "Medium")).IsChecked = true;
				await WindowHelper.WaitForIdle();
				Assert.AreEqual("Medium", radioField.SelectedLabels);

				page.Presenter.Content = checkBoxField;
				await WindowHelper.WaitForIdle();

				(await FindCheckBox(page, "A")).IsChecked = true;
				await WindowHelper.WaitForIdle();
				(await FindCheckBox(page, "B")).IsChecked = true;
				await WindowHelper.WaitForIdle();

				Assert.AreEqual("A,B", checkBoxField.SelectedLabels, "checkbox field");
				Assert.AreEqual("Medium", radioField.SelectedLabels, "radio field");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24354")]
		[DataRow(true)]
		[DataRow(false)]
		public async Task When_Radio_Template_Content_Swapped_Between_Models(bool nullContentFirst)
		{
			if (!nullContentFirst)
			{
				SkipSelectorPathOnNativeWinUI();
			}

			var field1 = new PickerModel(PickerKind.Radio, "Field1", "Small", "Medium", "Large");
			var checkBoxField = new PickerModel(PickerKind.CheckBox, "CheckBox", "A", "B", "C");
			var field2 = new PickerModel(PickerKind.Radio, "Field2", "Red", "Green", "Blue");

			var page = new RadioButton_TemplateSwap();
			var apply = CreateApply(page, nullContentFirst);

			try
			{
				await UITestHelper.Load(page, x => x.IsLoaded);
				apply(field1);

				(await FindRadio(page, "Medium")).IsChecked = true;
				await WindowHelper.WaitForIdle();
				Assert.AreEqual("Medium", field1.SelectedLabels);

				apply(checkBoxField);
				await WindowHelper.WaitForIdle();
				(await FindCheckBox(page, "A")).IsChecked = true;
				await WindowHelper.WaitForIdle();
				(await FindCheckBox(page, "B")).IsChecked = true;
				await WindowHelper.WaitForIdle();
				Assert.AreEqual("A,B", checkBoxField.SelectedLabels, "checkbox field");
				Assert.AreEqual("Medium", field1.SelectedLabels, "field1 after checkbox");

				apply(field2);
				await WindowHelper.WaitForIdle();
				Assert.AreEqual("", field2.SelectedLabels, "field2 after swap");
				(await FindRadio(page, "Green")).IsChecked = true;
				await WindowHelper.WaitForIdle();
				Assert.AreEqual("Green", field2.SelectedLabels, "field2 after select");
				Assert.AreEqual("Medium", field1.SelectedLabels, "field1 after field2 select");

				apply(field1);
				await WindowHelper.WaitForIdle();
				Assert.AreEqual("Medium", field1.SelectedLabels, "field1 after swap back");
				Assert.IsTrue((await FindRadio(page, "Medium")).IsChecked, "Medium radio checked after swap back");

				(await FindRadio(page, "Large")).IsChecked = true;
				await WindowHelper.WaitForIdle();
				Assert.AreEqual("Large", field1.SelectedLabels, "field1 after Large");
				Assert.IsFalse((await FindRadio(page, "Medium")).IsChecked, "Medium radio after Large");
				Assert.AreEqual("Green", field2.SelectedLabels, "field2 after Large");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24354")]
		[DataRow(true)]
		[DataRow(false)]
		public async Task When_Content_Swapped_Repeatedly_Within_One_Tick(bool nullContentFirst)
		{
			if (!nullContentFirst)
			{
				SkipSelectorPathOnNativeWinUI();
			}

			var field1 = new PickerModel(PickerKind.Radio, "Field1", "Small", "Medium", "Large");
			var field2 = new PickerModel(PickerKind.Radio, "Field2", "Red", "Green", "Blue");

			var page = new RadioButton_TemplateSwap();
			var apply = CreateApply(page, nullContentFirst);

			try
			{
				await UITestHelper.Load(page, x => x.IsLoaded);
				apply(field1);

				// Several trees for field1 are created and discarded before Loaded gets a chance to fire.
				apply(field2);
				apply(field1);
				apply(field2);
				apply(field1);
				await WindowHelper.WaitForIdle();

				(await FindRadio(page, "Medium")).IsChecked = true;
				await WindowHelper.WaitForIdle();
				Assert.AreEqual("Medium", field1.SelectedLabels, "field1 after Medium");
				Assert.IsTrue((await FindRadio(page, "Medium")).IsChecked, "Medium radio");

				(await FindRadio(page, "Large")).IsChecked = true;
				await WindowHelper.WaitForIdle();
				Assert.AreEqual("Large", field1.SelectedLabels, "field1 after Large");
				Assert.IsTrue((await FindRadio(page, "Large")).IsChecked, "Large radio");
				Assert.IsFalse((await FindRadio(page, "Medium")).IsChecked, "Medium radio after Large");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24354")]
		[DataRow(true)]
		[DataRow(false)]
		public async Task When_Discarded_Tree_Shares_GroupName(bool enteredTree)
		{
			var live1 = new RadioButton { GroupName = "G", Content = "L1" };
			var live2 = new RadioButton { GroupName = "G", Content = "L2" };
			var host = new Grid { Children = { new StackPanel { Children = { live1, live2 } } } };

			try
			{
				await UITestHelper.Load(host);

				var ghost1 = new RadioButton { GroupName = "G", Content = "G1" };
				var ghost2 = new RadioButton { GroupName = "G", Content = "G2" };
				var ghostPanel = new StackPanel { Children = { ghost1, ghost2 } };
				if (enteredTree)
				{
					// Enters and leaves in the same tick, so Loaded never fires for it.
					host.Children.Add(ghostPanel);
					host.Children.Remove(ghostPanel);
				}
				await WindowHelper.WaitForIdle();

				live1.IsChecked = true;
				ghost1.IsChecked = true;
				Assert.IsTrue(live1.IsChecked, "live1 after ghost1 checked");

				live2.IsChecked = true;
				Assert.IsTrue(live2.IsChecked, "live2");
				Assert.IsFalse(live1.IsChecked, "live1 after live2 checked");
				Assert.IsTrue(ghost1.IsChecked, "ghost1 after live2 checked");

				ghost2.IsChecked = true;
				Assert.IsTrue(live2.IsChecked, "live2 after ghost2 checked");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		/// <summary>
		/// Applies a model either through the ContentTemplateSelector or by clearing Content and ContentTemplate first
		/// and re-applying both, the way an app works around the presenter rebinding its outgoing tree.
		/// </summary>
		private static System.Action<PickerModel> CreateApply(RadioButton_TemplateSwap page, bool nullContentFirst)
		{
			if (!nullContentFirst)
			{
				return model => page.Presenter.Content = model;
			}

			var selector = page.Selector;
			page.Presenter.ContentTemplateSelector = null;

			return model =>
			{
				page.Presenter.Content = null;
				page.Presenter.ContentTemplate = null;
				page.Presenter.ContentTemplate = selector.SelectTemplate(model, page.Presenter);
				page.Presenter.Content = model;
			};
		}
	}
}
