#nullable enable

using Microsoft.UI.Input;

namespace Microsoft.UI.Xaml.Input
{
	public partial class InertiaRotationBehavior
	{
		private readonly GestureRecognizer.Manipulation.InertiaProcessor _processor;

		internal InertiaRotationBehavior(GestureRecognizer.Manipulation.InertiaProcessor processor)
		{
			_processor = processor;
		}

		public double DesiredRotation
		{
			get => _processor.DesiredRotation;
			set => _processor.DesiredRotation = value;
		}

		public double DesiredDeceleration
		{
			get => _processor.DesiredRotationDeceleration;
			set => _processor.DesiredRotationDeceleration = value;
		}
	}
}
