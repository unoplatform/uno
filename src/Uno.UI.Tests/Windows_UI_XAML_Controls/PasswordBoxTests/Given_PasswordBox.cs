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

namespace Uno.UI.Tests.PasswordBoxTests
{
	[TestClass]
	public class Given_PasswordBox
	{
		[TestMethod]
		public void When_Binding_Set_Null()
		{
			var passwordBox = new PasswordBox();
			var source = new MySource() { SourceText = "Spinach" };

			passwordBox.DataContext = source;
			passwordBox.SetBinding(PasswordBox.PasswordProperty, new Binding() { Path = new PropertyPath("SourceText") });

			Assert.AreEqual("Spinach", passwordBox.Password);

			source.SourceText = null;

			Assert.AreEqual("", passwordBox.Password);
		}

		[TestMethod]
		public void When_Set_DP_Null()
		{
			var passwordBox = new PasswordBox();

			passwordBox.SetValue(PasswordBox.PasswordProperty, "Spinach");
			Assert.AreEqual("Spinach", passwordBox.Password);
			passwordBox.SetValue(PasswordBox.PasswordProperty, null);
			Assert.AreEqual("", passwordBox.Password);
		}

		[TestMethod]
		public void When_Binding_Set_Non_String()
		{
			var passwordBox = new PasswordBox();
			var source = new MySource() { SourceInt = 12 };

			passwordBox.DataContext = source;
			passwordBox.SetBinding(PasswordBox.PasswordProperty, new Binding() { Path = new PropertyPath("SourceInt") });

			Assert.AreEqual("12", passwordBox.Password);

			source.SourceInt = 19;

			Assert.AreEqual("19", passwordBox.Password);
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
