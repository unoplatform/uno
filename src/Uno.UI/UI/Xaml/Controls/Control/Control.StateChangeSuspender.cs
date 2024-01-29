#nullable enable

using System;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class Control
	{
		private protected class StateChangeSuspender : IDisposable
		{
			private readonly Control _control;

			public StateChangeSuspender(Control control)
			{
				_control = control ?? throw new ArgumentNullException(nameof(control));

				_control._suspendStateChanges = true;
			}

			public void Dispose()
			{
				_control._suspendStateChanges = false;
				_control.UpdateVisualState();
			}
		}
	}
}
