using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls;

public enum ButtonMode
{
	Message,
	Task
}

public sealed partial class VisualStateUserControl : UserControl
{
	public static readonly DependencyProperty ModeProperty = DependencyProperty.Register(nameof(Mode), typeof(ButtonMode), typeof(VisualStateUserControl), new PropertyMetadata(default(ButtonMode), ModePropertyChanged));

	public ButtonMode Mode
	{
		get => (ButtonMode)GetValue(ModeProperty);
		set => SetValue(ModeProperty, value);
	}

	private static void ModePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is VisualStateUserControl VisualStateUserControl)
		{
			VisualStateUserControl.LastGoToStateResult = VisualStateManager.GoToState(VisualStateUserControl, VisualStateUserControl.Mode == ButtonMode.Message ? "MessageMode" : "TaskMode", false);

			if (VisualStateUserControl.LastGoToStateResult == false)
			{
				throw new InvalidOperationException("Failed to change visual state.");
			}
		}
	}

	public void ResetState() => LastGoToStateResult = null;

	public bool IsMessageTextVisible => MessageText.Visibility == Visibility.Visible;

	public bool IsTaskTextVisible => TaskText.Visibility == Visibility.Visible;

	public bool? LastGoToStateResult { get; private set; }

	public VisualStateUserControl()
	{
		InitializeComponent();
	}
}
