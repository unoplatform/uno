#nullable enable

using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

// MaxLength limits only user input: text set from code is kept whole, as on WinUI.
// The expected values were recorded on WinUI (Windows App SDK 1.8) with an app that drives a TextBox through its own APIs, the
// WinUI recording app, unless a test says otherwise.
[TestClass]
[RunsOnUIThread]
public partial class Given_TextBox_MaxLength
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25997")]
	public async Task When_Text_Set_From_Code_Is_Longer_Than_MaxLength()
	{
		try
		{
			var SUT = new TextBox { MaxLength = 5, Text = "0123456789" };
			Assert.AreEqual("0123456789", SUT.Text, "before the TextBox is loaded");

			await UITestHelper.Load(SUT);
			Assert.AreEqual("0123456789", SUT.Text, "once the TextBox is loaded");

			SUT.Text = "abcdefghij";
			Assert.AreEqual("abcdefghij", SUT.Text, "set while the TextBox is loaded");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25997")]
	public async Task When_Text_Set_From_Code_Is_Longer_Than_MaxLength_Then_Events_Are_Raised()
	{
		try
		{
			var SUT = new TextBox { MaxLength = 5 };
			await UITestHelper.Load(SUT);
			string? beforeTextChangingNewText = null;
			var textChanging = 0;
			var textChanged = 0;
			SUT.BeforeTextChanging += (_, e) => beforeTextChangingNewText = e.NewText;
			SUT.TextChanging += (_, _) => textChanging++;
			SUT.TextChanged += (_, _) => textChanged++;

			SUT.Text = "0123456789";
			await WindowHelper.WaitForIdle();

			Assert.AreEqual("0123456789", beforeTextChangingNewText);
			Assert.AreEqual(1, textChanging);
			Assert.AreEqual(1, textChanged);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25997")]
	public async Task When_Bound_Text_Is_Longer_Than_MaxLength()
	{
		try
		{
			// A single editor reused for a list of items: moving to an item with a long value must show that value, not keep the
			// previous item's text, which the next edit would otherwise write into the current item.
			var shortItem = new BoundItem { Value = "abc" };
			var longItem = new BoundItem { Value = "0123456789" };
			var SUT = new TextBox { MaxLength = 5, DataContext = shortItem };
			SUT.SetBinding(TextBox.TextProperty, new Binding { Path = new(nameof(BoundItem.Value)), Mode = BindingMode.TwoWay });

			await UITestHelper.Load(SUT);
			Assert.AreEqual("abc", SUT.Text);

			SUT.DataContext = longItem;
			Assert.AreEqual("0123456789", SUT.Text);

			SUT.DataContext = shortItem;
			Assert.AreEqual("abc", SUT.Text);
			Assert.AreEqual("abc", shortItem.Value);
			Assert.AreEqual("0123456789", longItem.Value);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25997")]
	public async Task When_SelectedText_Set_From_Code_Goes_Past_MaxLength()
	{
		try
		{
			var SUT = new TextBox { MaxLength = 5, Text = "01234" };
			await UITestHelper.Load(SUT);

			SUT.Select(5, 0);
			SUT.SelectedText = "XYZ";

			// WinUI also selects the inserted text, (5,3); Uno selects (0,3), a separate and older bug.
			Assert.AreEqual("01234XYZ", SUT.Text);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25997")]
	public async Task When_MaxLength_Lowered_Below_Text_Length()
	{
		try
		{
			var SUT = new TextBox { Text = "0123456789" };
			await UITestHelper.Load(SUT);

			SUT.MaxLength = 5;

			Assert.AreEqual("0123456789", SUT.Text);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25997")]
	public async Task When_Password_Set_From_Code_Is_Longer_Than_MaxLength()
	{
		try
		{
			var SUT = new PasswordBox { MaxLength = 5, Password = "0123456789" };
			Assert.AreEqual("0123456789", SUT.Password, "before the PasswordBox is loaded");

			await UITestHelper.Load(SUT);
			Assert.AreEqual("0123456789", SUT.Password, "once the PasswordBox is loaded");

			SUT.Password = "abcdefghij";
			Assert.AreEqual("abcdefghij", SUT.Password, "set while the PasswordBox is loaded");
#if __UNO__
			Assert.AreEqual(10, SUT.Core.TextBoxView.DisplayBlock.Text.Length, "the PasswordBox shows one mask character per password character");
#endif
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	// The attribute is required when running WinUI. See:
	// https://github.com/microsoft/microsoft-ui-xaml/issues/4723#issuecomment-812753123
	[Microsoft.UI.Xaml.Data.Bindable]
	public sealed partial class BoundItem : INotifyPropertyChanged
	{
		private string _value = "";

		public string Value
		{
			get => _value;
			set
			{
				_value = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
			}
		}

		public event PropertyChangedEventHandler? PropertyChanged;
	}
}
