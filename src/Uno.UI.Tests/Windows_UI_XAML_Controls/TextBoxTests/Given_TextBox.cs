using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
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
			
			Assert.ThrowsException<ArgumentNullException>(() => textBox.Text = null);

			Assert.AreEqual("Rhubarb", textBox.Text);
			Assert.AreEqual(1, callbackCount);
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
