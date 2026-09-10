using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls;

public enum ButtonMode
{
	None,
	Message,
	Task
}

public sealed partial class VisualStateUserControlWithoutTrigger : UserControl
{
	public static readonly DependencyProperty ModeProperty = DependencyProperty.Register(nameof(Mode), typeof(ButtonMode), typeof(VisualStateUserControlWithoutTrigger), new PropertyMetadata(default(ButtonMode), ModePropertyChanged));

	public ButtonMode Mode
	{
		get => (ButtonMode)GetValue(ModeProperty);
		set => SetValue(ModeProperty, value);
	}

	private static void ModePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is VisualStateUserControlWithoutTrigger VisualStateUserControlWithoutTrigger)
		{
			VisualStateUserControlWithoutTrigger.LastGoToStateResult = VisualStateManager.GoToState(VisualStateUserControlWithoutTrigger, VisualStateUserControlWithoutTrigger.Mode == ButtonMode.Message ? "MessageMode" : "TaskMode", false);

			if (VisualStateUserControlWithoutTrigger.LastGoToStateResult == false)
			{
				throw new InvalidOperationException("Failed to change visual state.");
			}
		}
	}

	public void ResetState() => LastGoToStateResult = null;

	public bool IsMessageTextVisible => MessageText.Visibility == Visibility.Visible;

	public bool IsTaskTextVisible => TaskText.Visibility == Visibility.Visible;

	public bool IsMessageState => ModeStates.CurrentState == MessageMode;

	public bool IsTaskState => ModeStates.CurrentState == TaskMode;

	public bool? LastGoToStateResult { get; private set; }

	public VisualStateUserControlWithoutTrigger()
	{
		InitializeComponent();
	}
}
