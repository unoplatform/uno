#nullable enable

using System;
using System.ComponentModel;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace Uno.UI.RuntimeTests.Tests;

public class BindingShouldBeAppliedOnPropertyChangedEventConverter : IValueConverter
{
	public int ConvertCount { get; private set; }

	public object? Convert(object? value, Type targetType, object parameter, string language)
	{
		ConvertCount++;
		return value is BindingShouldBeAppliedOnPropertyChangedEventValueHolder holder ? holder.Value : null;
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language)
		=> throw new NotSupportedException();
}

public sealed partial class BindingShouldBeAppliedOnPropertyChangedEvent : Page
{
	public BindingShouldBeAppliedOnPropertyChangedEvent()
	{
		this.InitializeComponent();
		this.DataContext = new BindingShouldBeAppliedOnPropertyChangedEventVM();
	}
}

public class BindingShouldBeAppliedOnPropertyChangedEventVM : INotifyPropertyChanged
{
	public BindingShouldBeAppliedOnPropertyChangedEventValueHolder Holder { get; set; } = new();

	public event PropertyChangedEventHandler? PropertyChanged;

	public void Increment()
	{
		Holder.Value++;
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Holder)));
	}
}

public class BindingShouldBeAppliedOnPropertyChangedEventValueHolder
{
	public int Value { get; set; }
}
