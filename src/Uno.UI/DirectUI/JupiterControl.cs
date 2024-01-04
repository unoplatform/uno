namespace DirectUI;

internal class JupiterControl
{
	internal static JupiterControl Create()
	{
		var control = new JupiterControl();
		control.Init();
		return control;
	}

	private JupiterControl()
	{
	}
}
