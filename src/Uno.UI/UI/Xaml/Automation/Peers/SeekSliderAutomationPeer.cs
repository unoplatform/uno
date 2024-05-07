using System;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

internal partial class SeekSliderAutomationPeer : SliderAutomationPeer, IValueProvider
{
	public SeekSliderAutomationPeer(Slider owner) : base(owner)
	{
	}

	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.Value)
		{
			return this;
		}
		else
		{
			return base.GetPatternCore(patternInterface);
		}
	}

	protected override string GetClassNameCore() => nameof(SeekSliderAutomationPeer);

	string IValueProvider.Value
	{
		get
		{
			var owner = (Slider)Owner;
			TimeSpan currentPosition;
			TimeSpan duration = owner.GetCurrentDuration();

			if (duration == TimeSpan.MaxValue || duration == TimeSpan.Zero)
			{
				currentPosition = owner.GetCurrentPosition();
			}
			else
			{
				// Construct the current position timestamp from the slider position in normal cases.
				// This is necessary because in the scenario of seeking, the position reported by
				// the player will not have completed the seek and would otherwise report an old position.
				var positionSliderMinimum = owner.Minimum;
				var positionSliderMaximum = owner.Maximum;
				var positionSliderValue = owner.Value;

				currentPosition = (positionSliderValue - positionSliderMinimum) / (positionSliderMaximum - positionSliderMinimum) * duration;
			}

			return ConvertTimeSpanToHString(currentPosition)
		}
	}

	void IValueProvider.SetValue(string value) => throw new NotSupportedException("Value cannot be set via automation.");

	private string ConvertTimeSpanToHString(TimeSpan position)
	{
        private string ConvertTimeSpanToHString(TimeSpan position)
        {
            const uint HNSPerSecond = 10000000;

            string szPositionTime;
            string strPositionTime;
            string strUnformattedPositionTime;
            string strHours;
            string strMinutes;
            string strSeconds;

            int positionInSeconds = (int)(position.TotalSeconds);
            int totalSecondsInRange = (positionInSeconds > 0) ? (positionInSeconds % 86400) : 0;
            int numHours = totalSecondsInRange / 3600;
            int remainingSeconds = totalSecondsInRange % 3600;
            int numMinutes = remainingSeconds / 60;
            int numSeconds = remainingSeconds % 60;

            Debug.Assert(numHours < 24 && numMinutes < 60 && numSeconds < 60);

            // Assemble seconds portion of string
            CreatePositionTimeComponent(
                numSeconds == 1 ? UIA_MEDIA_SEEKSLIDER_SECOND : UIA_MEDIA_SEEKSLIDER_SECONDS,
                numSeconds,
                out strSeconds);

            // Assemble minutes portion of string
            CreatePositionTimeComponent(
                numMinutes == 1 ? UIA_MEDIA_SEEKSLIDER_MINUTE : UIA_MEDIA_SEEKSLIDER_MINUTES,
                numMinutes,
                out strMinutes);

            // Assemble final current position string. Include hours if relevant.
            if (numHours > 0)
            {
                CreatePositionTimeComponent(
                    numHours == 1 ? UIA_MEDIA_SEEKSLIDER_HOUR : UIA_MEDIA_SEEKSLIDER_HOURS,
                    numHours,
                    out strHours);

                string strUnformattedPositionTime = DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString(UIA_MEDIA_SEEKSLIDER_POSITION_3, out strUnformattedPositionTime);
                int cch = FormatMsg(szPositionTime, strUnformattedPositionTime, strHours, strMinutes, strSeconds);
                if (cch > 1)
                {
                    return strPositionTime;
                }
                else
                {
                    throw new Exception("Failed to format position time.");
                }
            }
            else
            {
                string strUnformattedPositionTime = DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString(UIA_MEDIA_SEEKSLIDER_POSITION_2, out strUnformattedPositionTime);
                int cch = FormatMsg(szPositionTime, strUnformattedPositionTime, strMinutes, strSeconds);
                if (cch > 1)
                {
                    return strPositionTime;
                }
                else
                {
                    throw new Exception("Failed to format position time.");
                }
            }
        }

        private void CreatePositionTimeComponent(string component, int value, out string result)
        {
			static const unsigned int MaxNumberLength = 3;  // max of 2 digits per time component (hours, minutes, seconds) and a trailing '\0'

			wrl_wrappers::HString strUnformattedTimeComponent;
			WCHAR szNumber[MaxNumberLength];
			WCHAR szTimeComponentBuffer[128];
			XUINT32 cch;

			HRESULT hr = S_OK;
			IFC(DXamlCore::GetCurrentNoCreate()->GetLocalizedResourceString(localizationId, strUnformattedTimeComponent.ReleaseAndGetAddressOf()));
			IFC(_itow_s(timeComponentValue, szNumber, 10) ? E_FAIL : S_OK);
			cch = FormatMsg(szTimeComponentBuffer, strUnformattedTimeComponent.GetRawBuffer(NULL), szNumber);
			IFC(1 < cch ? S_OK : E_FAIL);
			IFC(strTimeComponent.Set(szTimeComponentBuffer));
		}
	}

	bool IValueProvider.IsReadOnly => base.IsReadOnly;
}
