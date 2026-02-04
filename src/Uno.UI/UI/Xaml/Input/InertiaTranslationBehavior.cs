#nullable enable

using Microsoft.UI.Input;

namespace Microsoft.UI.Xaml.Input
{
	public partial class InertiaTranslationBehavior
	{
		private readonly GestureRecognizer.Manipulation.InertiaProcessor _processor;

		internal InertiaTranslationBehavior(GestureRecognizer.Manipulation.InertiaProcessor processor)
		{
			_processor = processor;
		}

		public double DesiredDisplacement
		{
			get => _processor.DesiredDisplacement;
			set => _processor.DesiredDisplacement = value;
		}

		public double DesiredDeceleration
		{
			get => _processor.DesiredDisplacementDeceleration;
			set => _processor.DesiredDisplacementDeceleration = value;
		}
	}
}
