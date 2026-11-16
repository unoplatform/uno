using System;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests;

public sealed partial class XBindTeardown_Setup : Page
{
	public static event RoutedEventHandler VMAccessDetected;

	public XBindTeardown_Setup()
	{
		this.InitializeComponent();
	}

	public XBindTeardown_Setup_Data VM
	{
		get
		{
			if (DataContext is { })
			{
			}
			else
			{
			}

			VMAccessDetected?.Invoke(this, new());
			return ((XBindTeardown_Setup_Data_Wrapper)DataContext).Data;
		}
	}

	public record XBindTeardown_Setup_Data_Wrapper(XBindTeardown_Setup_Data Data);
	public class XBindTeardown_Setup_Data : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		private string _text;
		public string Text
		{
			get => _text;
			set
			{
				if (_text != value)
				{
					_text = value;
					PropertyChanged?.Invoke(this, new(nameof(Text)));
				}
			}
		}
	}
}
