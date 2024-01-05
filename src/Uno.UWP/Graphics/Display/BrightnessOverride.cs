using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Windows.Graphics.Display
{
	public sealed partial class BrightnessOverride
	{
#pragma warning disable CS0649
		private static double _defaultBrightnessLevel;
		private static double _targetBrightnessLevel;
#pragma warning restore CS0649

		private static BrightnessOverride? _instance;

		/// <summary>
		/// Returns a brightness override object.
		/// </summary>
		public static BrightnessOverride GetForCurrentView()
		{
			if (_instance == null)
			{
				_instance = new BrightnessOverride();
			}

			return _instance;
		}

		/// <summary>
		/// Gets the current screen brightness level.
		/// </summary>
		public double BrightnessLevel => IsOverrideActive ? _targetBrightnessLevel : _defaultBrightnessLevel;

		/// <summary>
		/// BOOLEAN value that indicates whether the brightness override is active. 
		/// If TRUE, the current brightness level matches the override brightness level. 
		/// This property value will always be FALSE if StartOverride() isn't called.
		/// </summary>
		public bool IsOverrideActive { get; private set; }
	}
}
