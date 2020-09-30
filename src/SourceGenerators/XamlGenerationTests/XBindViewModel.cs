using System.ComponentModel;
using System.Globalization;

namespace XamlGenerationTests.Shared
{
	public class XBindViewModel : INotifyPropertyChanged
	{
		private string _fullName;

		public string FullName
		{
			get
			{
				return _fullName;
			}
			set
			{
				if (_fullName != value)
				{
					_fullName = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("FullName"));
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public void BoundMethodCallNoParameters()
		{
		}

		public string BoundMethodCallWithParameters(string value, bool? isChecked)
		{
			return $"{value} is {(isChecked.HasValue ? isChecked.Value.ToString(CultureInfo.CurrentCulture) : "null")}";
		}

		public string ReadText(string value, bool? isChecked)
		{
			return $"{value} is {(isChecked.HasValue ? isChecked.Value.ToString(CultureInfo.CurrentCulture) : "null")}";
		}

		public void WriteText(string text)
		{ }
	}
}
