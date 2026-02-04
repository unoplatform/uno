#nullable enable

using Microsoft.UI.Input;

namespace Microsoft.UI.Xaml.Input
{
	public partial class InertiaExpansionBehavior
	{
		private readonly GestureRecognizer.Manipulation.InertiaProcessor _processor;

		internal InertiaExpansionBehavior(GestureRecognizer.Manipulation.InertiaProcessor processor)
		{
			_processor = processor;
		}

		public double DesiredExpansion
		{
			get => _processor.DesiredExpansion;
			set => _processor.DesiredExpansion = value;
		}

		public double DesiredDeceleration
		{
			get => _processor.DesiredExpansionDeceleration;
			set => _processor.DesiredExpansionDeceleration = value;
		}
	}
}
