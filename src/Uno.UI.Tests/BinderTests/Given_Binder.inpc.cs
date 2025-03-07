using CommonServiceLocator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Uno.Extensions;
using Uno.UI.DataBinding;
using Windows.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Uno.Disposables;
using Uno.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Microsoft.Extensions.Logging;
using Windows.UI.Xaml.Controls;
using System.Threading;
using Uno.UI.Xaml;
using System.Diagnostics;
using System.Globalization;

namespace Uno.UI.Tests.BinderTests
{
	[TestClass]
	public partial class Given_Binder_INPC
	{
		[TestMethod]
		public void When_INPC_Update_SameReference()
		{
			var SUT = new Binder_INPC_Data();
			SUT.SetBinding(Binder_INPC_Data.MyValueProperty, new Binding { Path = new PropertyPath("Class1.Value") });

			var master = new Binder_INPC_Base_Class();
			SUT.DataContext = master;

			Assert.AreEqual(SUT.MyValue, master.Class1.Value);
			Assert.AreEqual(2, master.Class1.ValueGetCount);
			Assert.AreEqual(0, master.Class1.ValueSetCount);

			master.Update();

			Assert.AreEqual(SUT.MyValue, master.Class1.Value);
			Assert.AreEqual(4, master.Class1.ValueGetCount);
			Assert.AreEqual(1, master.Class1.ValueSetCount);
		}

		[TestMethod]
		public void When_INPC_Update_SameReference_Converter()
		{
			var SUT = new Binder_INPC_Data();
			SUT.SetBinding(
				Binder_INPC_Data.MyValueProperty,
				new Binding
				{
					Path = new PropertyPath("Value"),
					Converter = new Binder_INPC_DummyConverter()
				});

			var master = new Binder_INPC_Base_Class();
			SUT.DataContext = master;

			Assert.AreEqual(1, master.ValueGetCount);
			Assert.AreEqual(0, master.ValueSetCount);

			master.RaiseUpdated();

			Assert.AreEqual(SUT.MyValue, master.Value);
			Assert.AreEqual(3, master.ValueGetCount);
			Assert.AreEqual(0, master.ValueSetCount);
		}

		[TestMethod]
		public void When_INPC_Raise_All_Updated()
		{
			var SUT = new Binder_INPC_Data();
			SUT.SetBinding(
				Binder_INPC_Data.MyValueProperty,
				new Binding
				{
					Path = new PropertyPath("Class1.Value")
				});

			SUT.SetBinding(
				Binder_INPC_Data.MyValue2Property,
				new Binding
				{
					Path = new PropertyPath("Value"),
					Converter = new Binder_INPC_DummyConverter()
				});

			var master = new Binder_INPC_Base_Class();
			SUT.DataContext = master;

			master.RaiseAllUpdated();

			Assert.AreEqual(SUT.MyValue, master.Class1.Value);
			Assert.AreEqual(SUT.MyValue2, master.Value);
		}
	}

	public partial class Binder_INPC_Data : DependencyObject
	{
		public string MyValuePropertyValueDuringChange { get; private set; }

		public string MyValue
		{
			get => GetMyValueValue();
			set => SetMyValueValue(value);
		}

		[GeneratedDependencyProperty(DefaultValue = "")]
		public static DependencyProperty MyValueProperty { get; } = CreateMyValueProperty();

		private void OnMyValueChanged(string oldValue, string newValue)
		{
			MyValuePropertyValueDuringChange = MyValue;
		}

		public string MyValue2
		{
			get => GetMyValue2Value();
			set => SetMyValue2Value(value);
		}

		[GeneratedDependencyProperty(DefaultValue = "")]
		public static DependencyProperty MyValue2Property { get; } = CreateMyValue2Property();
	}

	public class Binder_INPC_Base_Class : Binder_INPC_BaseViewModel
	{
		private string _value = Guid.NewGuid().ToString();
		public int ValueGetCount { get; private set; }
		public int ValueSetCount { get; private set; }

		public Binder_INPC_Class1 Class1 { get; set; } = new Binder_INPC_Class1();
		public string Value
		{
			get
			{
				ValueGetCount++;
				return _value;
			}
			set
			{
				ValueSetCount++;
				_value = value;
			}
		}

		public void Update()
		{
			Class1.Value = (Binder_INPC_Class1.IndexCounter++).ToString(CultureInfo.InvariantCulture);
			Value = (Binder_INPC_Class1.IndexCounter++).ToString(CultureInfo.InvariantCulture);
			RaiseUpdated();
		}

		public void RaiseUpdated()
		{
			OnPropertyChanged(nameof(Class1));
			OnPropertyChanged(nameof(Value));
		}

		public void RaiseAllUpdated()
		{
			Class1.Value = (Binder_INPC_Class1.IndexCounter++).ToString(CultureInfo.InvariantCulture);
			Value = (Binder_INPC_Class1.IndexCounter++).ToString(CultureInfo.InvariantCulture);
			OnPropertyChanged(string.Empty);
		}
	}

	public class Binder_INPC_Class1
	{
		public static int IndexCounter = 0;
		private string _value = (IndexCounter++).ToString(CultureInfo.InvariantCulture);
		public int ValueGetCount { get; set; }
		public int ValueSetCount { get; set; }

		public string Value
		{
			get
			{
				ValueGetCount++;
				return _value;
			}
			set
			{
				ValueSetCount++;
				_value = value;
			}
		}
	}

	public class Binder_INPC_BaseViewModel : System.ComponentModel.INotifyPropertyChanged
	{
		public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
			=> PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
	}

	public class Binder_INPC_DummyConverter : IValueConverter
	{
		public static int ConvertCount { get; set; }
		public static int ConvertBackCount { get; set; }


		public object Convert(object value, Type targetType, object parameter, string language)
		{
			ConvertCount++;
			return value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			ConvertBackCount++;
			return value;
		}
	}
}
