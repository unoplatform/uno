#nullable enable

using System;
using System.Collections.Generic;
using Uno.Foundation.Extensibility;

namespace Windows.Devices.Haptics
{
	public partial class SimpleHapticsController
	{
		private ISimpleHapticsControllerExtension _simpleHapticsControllerExtension = null!;

		partial void InitPlatform()
		{
			_simpleHapticsControllerExtension = ApiExtensibility.CreateInstance<ISimpleHapticsControllerExtension>(typeof(SimpleHapticsController));
		}

		public IReadOnlyList<SimpleHapticsControllerFeedback> SupportedFeedback =>
			_simpleHapticsControllerExtension.SupportedFeedback;

		public void SendHapticFeedback(SimpleHapticsControllerFeedback feedback)
		{
			if (feedback is null)
			{
				throw new ArgumentNullException(nameof(feedback));
			}

			_simpleHapticsControllerExtension.SendHapticFeedback(feedback);
		}
	}

	internal interface ISimpleHapticsControllerExtension
	{
		IReadOnlyList<SimpleHapticsControllerFeedback> SupportedFeedback { get; }

		void SendHapticFeedback(SimpleHapticsControllerFeedback feedback);
	}
}
