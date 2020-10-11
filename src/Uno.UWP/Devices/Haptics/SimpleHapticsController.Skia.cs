using System;
using System.Collections.Generic;
using Uno.Foundation.Extensibility;

namespace Windows.Devices.Haptics
{
	public partial class SimpleHapticsController
    {
		private ISimpleHapticsControllerExtension _simpleHapticsControllerExtensions;

		partial void InitPlatform()
		{
			if (!ApiExtensibility.CreateInstance(typeof(VibrationDevice), out _simpleHapticsControllerExtensions))
			{
				throw new InvalidOperationException($"Unable to find IApplicationExtension extension");
			}
		}

		public IReadOnlyList<SimpleHapticsControllerFeedback> SupportedFeedback =>
			_simpleHapticsControllerExtensions.SupportedFeedback;

		public void SendHapticFeedback(SimpleHapticsControllerFeedback feedback) =>
			_simpleHapticsControllerExtensions.SendHapticFeedback(feedback);
	}

	internal interface ISimpleHapticsControllerExtension
	{
		IReadOnlyList<SimpleHapticsControllerFeedback> SupportedFeedback { get; }

		void SendHapticFeedback(SimpleHapticsControllerFeedback feedback);
	}
}
