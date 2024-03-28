using System;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace Uno.UI.Tests.TextBoxTests
{
	[TestClass]
	public class Given_TextBox
	{
		[TestMethod]
		public void When_TextChanging_Modifying_Text()
		{
			var textBox = new TextBox();
			textBox.TextChanging
				+= (o, e) =>
			  {
				  var tb = o as TextBox;
				  tb.Text = tb.Text + "Street";
			  };
			textBox.Text = "E";

			Assert.AreEqual("EStreet", textBox.Text);
		}

		[TestMethod]
		public void When_Setting_Text_Null()
		{
			var textBox = new TextBox();
			Assert.AreEqual(string.Empty, textBox.Text);
			var callbackCount = 0;
			textBox.RegisterPropertyChangedCallback(TextBox.TextProperty, (o, e) =>
			{
				callbackCount++;
			});

			textBox.Text = "Rhubarb";
			Assert.AreEqual("Rhubarb", textBox.Text);
			Assert.AreEqual(1, callbackCount);

#if !HAS_UNO_WINUI
			// Setting TextBox.Text to null throws an exception in UWP but not WinUI.
			Assert.ThrowsException<ArgumentNullException>(() => textBox.Text = null);
#endif

			Assert.AreEqual("Rhubarb", textBox.Text);
			Assert.AreEqual(1, callbackCount);

#if HAS_UNO_WINUI
			textBox.Text = null;
			Assert.AreEqual("", textBox.Text);
#endif
		}

		[TestMethod]
		public void Calling_Select_With_NegativeValues()
		{
			var textBox = new TextBox();
			Assert.ThrowsException<ArgumentException>(() => textBox.Select(0, -1));
			Assert.ThrowsException<ArgumentException>(() => textBox.Select(-1, 0));
		}

		[TestMethod]
		public void When_Binding_Set_Null()
		{
			var textBox = new TextBox();
			var source = new MySource() { SourceText = "Spinach" };

			textBox.DataContext = source;
			textBox.SetBinding(TextBox.TextProperty, new Binding() { Path = new PropertyPath("SourceText") });

			Assert.AreEqual("Spinach", textBox.Text);

			source.SourceText = null;

			Assert.AreEqual("", textBox.Text);
		}

		[TestMethod]
		public void When_Binding_Set_Non_String()
		{
			var textBox = new TextBox();
			var source = new MySource() { SourceInt = 12 };

			textBox.DataContext = source;
			textBox.SetBinding(TextBox.TextProperty, new Binding() { Path = new PropertyPath("SourceInt") });

			Assert.AreEqual("12", textBox.Text);

			source.SourceInt = 19;

			Assert.AreEqual("19", textBox.Text);
		}

		[TestMethod]
		public void When_BeforeTextChanging_Cancel()
		{
			var textBox = new TextBox();
			textBox.Text = "Mango";
			var textChangingCount = 0;
			var beforeTextChangingCount = 0;
			textBox.BeforeTextChanging += (tb, e) =>
			{
				beforeTextChangingCount++;
				if (e.NewText == "Papaya")
				{
					e.Cancel = true;
				}
			};
			textBox.TextChanging += (tb, e) => textChangingCount++;
			textBox.Text = "Chirimoya";
			Assert.AreEqual("Chirimoya", textBox.Text);
			Assert.AreEqual(1, beforeTextChangingCount);
			Assert.AreEqual(1, textChangingCount);

			textBox.Text = "Papaya";
			Assert.AreEqual("Chirimoya", textBox.Text);
			Assert.AreEqual(2, beforeTextChangingCount);
			Assert.AreEqual(1, textChangingCount);

			textBox.Text = "Chirimoya";
			Assert.AreEqual(2, beforeTextChangingCount);
		}

		[TestMethod]
		public void When_Multi_Line_Text_And_Not_AcceptsReturn()
		{
			var textBox = new TextBox();
			Assert.AreEqual(false, textBox.AcceptsReturn);
			textBox.Text = "Hello\nWorld";
			Assert.AreEqual("Hello", textBox.Text);

			textBox.Text = "Hello\rWorld";
			Assert.AreEqual("Hello", textBox.Text);
		}

		[TestMethod]
		public void When_Multi_Line_Text_And_Not_AcceptsReturn_After_Text_Was_Set()
		{
			var textBox = new TextBox();

			textBox.AcceptsReturn = true;
			textBox.Text = "Hello\nWorld";
			Assert.AreEqual("Hello\nWorld", textBox.Text);

			textBox.AcceptsReturn = false;
			Assert.AreEqual("Hello", textBox.Text);
		}

		[TestMethod]
		public void When_SelectedText_StartZero()
		{
			var sut = new TextBox();
			sut.Text = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

			sut.SelectionStart = 0;
			sut.SelectionLength = 0;
			sut.SelectedText = "1234";

			Assert.AreEqual("1234ABCDEFGHIJKLMNOPQRSTUVWXYZ", sut.Text);
		}

		[TestMethod]
		public void When_SelectedText_EndOfText()
		{
			var sut = new TextBox();
			sut.Text = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

			sut.SelectionStart = 26;
			sut.SelectedText = "1234";

			Assert.AreEqual("ABCDEFGHIJKLMNOPQRSTUVWXYZ1234", sut.Text);
		}

		[TestMethod]
		public void When_SelectedText_MiddleOfText()
		{
			var sut = new TextBox();
			sut.Text = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

			sut.SelectionStart = 2;
			sut.SelectionLength = 22;
			sut.SelectedText = "1234";

			Assert.AreEqual("AB1234YZ", sut.Text);
		}

		[TestMethod]
		public void When_SelectedText_AllTextToEmpty()
		{
			var sut = new TextBox();
			sut.Text = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

			sut.SelectionStart = 0;
			sut.SelectionLength = 26;
			sut.SelectedText = String.Empty;

			Assert.AreEqual(String.Empty, sut.Text);
			Assert.AreEqual(0, sut.SelectionStart);
			Assert.AreEqual(0, sut.SelectionLength);
		}

		public class MySource : System.ComponentModel.INotifyPropertyChanged
		{
			private string _sourceText;

			public string SourceText
			{
				get => _sourceText;
				set
				{
					if (_sourceText != value)
					{
						_sourceText = value;
						OnPropertyChanged();
					}
				}
			}

			private int _sourceInt;

			public int SourceInt
			{
				get => _sourceInt;
				set
				{
					if (_sourceInt != value)
					{
						_sourceInt = value;
						OnPropertyChanged();
					}
				}
			}

			public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

			protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
				=> PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
		}
	}
}
