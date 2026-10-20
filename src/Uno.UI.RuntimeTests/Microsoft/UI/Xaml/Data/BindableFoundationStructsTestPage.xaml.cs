#nullable enable

using System.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Data;

public sealed partial class BindableFoundationStructsTestPage : Page, INotifyPropertyChanged
{
	private Rect _testRect;
	private Size _testSize;
	private Point _testPoint;

	public BindableFoundationStructsTestPage()
	{
		this.InitializeComponent();
		this.DataContext = this;
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	public Rect TestRect
	{
		get => _testRect;
		set
		{
			if (_testRect != value)
			{
				_testRect = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TestRect)));
			}
		}
	}

	public Size TestSize
	{
		get => _testSize;
		set
		{
			if (_testSize != value)
			{
				_testSize = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TestSize)));
			}
		}
	}

	public Point TestPoint
	{
		get => _testPoint;
		set
		{
			if (_testPoint != value)
			{
				_testPoint = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TestPoint)));
			}
		}
	}
}
