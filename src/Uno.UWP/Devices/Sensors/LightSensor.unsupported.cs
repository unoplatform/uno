#if !__WASM__ && !__ANDROID__
namespace Windows.Devices.Sensors
{
    public partial class LightSensor
    {
		private LightSensor()
		{
		}

        public static LightSensor? GetDefault() => null;
    }
}
#endif
