using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls;

public sealed partial class VisualStateUserControlWithTrigger : UserControl
{
	public static readonly DependencyProperty ModeProperty = DependencyProperty.Register(nameof(Mode), typeof(ButtonMode), typeof(VisualStateUserControlWithTrigger), new PropertyMetadata(default(ButtonMode), ModePropertyChanged));

	public ButtonMode Mode
	{
		get => (ButtonMode)GetValue(ModeProperty);
		set => SetValue(ModeProperty, value);
	}

	private static void ModePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is VisualStateUserControlWithTrigger VisualStateUserControlWithTrigger)
		{
			VisualStateUserControlWithTrigger.LastGoToStateResult = VisualStateManager.GoToState(VisualStateUserControlWithTrigger, VisualStateUserControlWithTrigger.Mode == ButtonMode.Message ? "MessageMode" : "TaskMode", false);

			if (VisualStateUserControlWithTrigger.LastGoToStateResult == false)
			{
				throw new InvalidOperationException("Failed to change visual state.");
			}
		}
	}

	public void ResetState() => LastGoToStateResult = null;

	public bool IsMessageTextVisible => ModeStates.CurrentState == MessageMode;

	public bool IsTaskTextVisible => ModeStates.CurrentState == TaskMode;

	public bool? LastGoToStateResult { get; private set; }

	public bool IsTriggerState => ModeStates.CurrentState == TriggerMode;

	public bool IsMessageState => ModeStates.CurrentState == MessageMode;

	public bool IsTaskState => ModeStates.CurrentState == TaskMode;

	public void SetTriggerSize(double size) => WindowSizeTrigger.MinWindowWidth = size;

	public VisualStateUserControlWithTrigger()
	{
		InitializeComponent();
	}
}
