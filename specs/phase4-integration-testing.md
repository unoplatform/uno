# Phase 4: Integration and Testing

## Overview

This phase integrates all the components from Phases 1-3 and creates comprehensive tests to verify the DateTimeFormatter and Calendar implementations match WinRT behavior.

## Integration Points

### 1. DateTimeFormatter → Calendar Integration

The DateTimeFormatter must properly create and use Calendar instances:

```csharp
public DateTimeFormatter(
    string formatTemplate,
    IEnumerable<string> languages,
    string geographicRegion,
    string calendar,
    string clock)
{
    // Create Calendar with specified settings
    _calendar = new Calendar(languages, calendar, clock);

    // Create formatting context
    _context = new DateTimeFormattingContext(
        new CultureInfo(languages.First()),
        calendar,
        clock);

    // ... rest of initialization
}
```

### 2. Pattern Nodes → Calendar Integration

Each pattern node should use the Calendar from the context:

```csharp
// In PatternYearNode.Format
internal override void Format(StringBuilder builder, DateTimeOffset dateTime, DateTimeFormattingContext context)
{
    context.Calendar.SetDateTime(dateTime);
    builder.Append(context.Calendar.YearAsString());
}
```

### 3. TimePicker → DateTimeFormatter Integration

The TimePicker control uses DateTimeFormatter with clock overrides. The workaround code mentioned in the issue should be removed once the fix is complete.

Files to update:
- `src/Uno.UI/UI/Xaml/Controls/TimePicker/TimePicker.partial.mux.cs`
- `src/Uno.UI/UI/Xaml/Controls/TimePicker/TimePickerFlyoutPresenter.partial.mux.cs`

## Test Plan

### Unit Tests: DateTimeFormatter

#### Test File: `DateTimeFormatterTests.cs`

```csharp
namespace Uno.UWP.Tests.Globalization.DateTimeFormatting
{
    [TestClass]
    public class DateTimeFormatterTests
    {
        private static readonly DateTimeOffset TestDate =
            new DateTimeOffset(2024, 9, 15, 14, 30, 45, TimeSpan.Zero);

        #region Clock Override Tests

        [TestMethod]
        public void Format_TwelveHourClock_OnTwentyFourHourCulture_IncludesPeriod()
        {
            // Czech culture defaults to 24-hour clock
            var formatter = new DateTimeFormatter(
                "shorttime",
                ["cs-CZ"],
                "CZ",
                CalendarIdentifiers.Gregorian,
                ClockIdentifiers.TwelveHour);

            var result = formatter.Format(TestDate);

            // Should include period (AM/PM equivalent in Czech)
            Assert.IsTrue(result.Contains("odp.") || result.Contains("PM") ||
                          result.Contains(":") && result.Length > 5,
                          $"Expected period designator, got: {result}");
        }

        [TestMethod]
        public void Format_TwentyFourHourClock_OnTwelveHourCulture_NoPeriod()
        {
            // US English defaults to 12-hour clock
            var formatter = new DateTimeFormatter(
                "shorttime",
                ["en-US"],
                "US",
                CalendarIdentifiers.Gregorian,
                ClockIdentifiers.TwentyFourHour);

            var result = formatter.Format(TestDate);

            // Should NOT include AM/PM
            Assert.IsFalse(result.Contains("AM") || result.Contains("PM"),
                          $"Expected no period designator, got: {result}");
            // Should show 14 (or 2 PM converted to 14)
            Assert.IsTrue(result.Contains("14"),
                          $"Expected 24-hour format, got: {result}");
        }

        [TestMethod]
        public void Format_MidnightIn12HourClock_Shows12()
        {
            var midnight = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var formatter = new DateTimeFormatter(
                "{hour.integer}",
                ["en-US"],
                "US",
                CalendarIdentifiers.Gregorian,
                ClockIdentifiers.TwelveHour);

            var result = formatter.Format(midnight);

            Assert.AreEqual("12", result);
        }

        [TestMethod]
        public void Format_NoonIn12HourClock_Shows12()
        {
            var noon = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
            var formatter = new DateTimeFormatter(
                "{hour.integer}",
                ["en-US"],
                "US",
                CalendarIdentifiers.Gregorian,
                ClockIdentifiers.TwelveHour);

            var result = formatter.Format(noon);

            Assert.AreEqual("12", result);
        }

        #endregion

        #region Calendar System Tests

        [TestMethod]
        public void Format_JapaneseCalendar_IncludesEra()
        {
            var formatter = new DateTimeFormatter(
                "{era.abbreviated}{year.full}年",
                ["ja-JP"],
                "JP",
                CalendarIdentifiers.Japanese,
                ClockIdentifiers.TwentyFourHour);

            // 2024 is Reiwa 6
            var result = formatter.Format(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.FromHours(9)));

            // Should contain era designation and year
            Assert.IsTrue(result.Contains("令和") || result.Contains("R"),
                          $"Expected Japanese era, got: {result}");
        }

        [TestMethod]
        public void Format_HebrewCalendar_HebrewMonthNames()
        {
            var formatter = new DateTimeFormatter(
                "{month.full}",
                ["he-IL"],
                "IL",
                CalendarIdentifiers.Hebrew,
                ClockIdentifiers.TwentyFourHour);

            var result = formatter.Format(TestDate);

            // Should be a Hebrew month name, not Gregorian
            Assert.IsFalse(result.Equals("September", StringComparison.OrdinalIgnoreCase),
                          $"Expected Hebrew month, got: {result}");
        }

        #endregion

        #region Pattern Tests

        [TestMethod]
        public void Pattern_YearFullWithPadding_PadsCorrectly()
        {
            var formatter = new DateTimeFormatter("{year.full(2)}");
            var date = new DateTimeOffset(99, 1, 1, 0, 0, 0, TimeSpan.Zero);

            var result = formatter.Format(date);

            Assert.AreEqual("99", result);
        }

        [TestMethod]
        public void Pattern_MonthIntegerWithPadding_PadsCorrectly()
        {
            var formatter = new DateTimeFormatter("{month.integer(2)}");
            var date = new DateTimeOffset(2024, 5, 1, 0, 0, 0, TimeSpan.Zero);

            var result = formatter.Format(date);

            Assert.AreEqual("05", result);
        }

        [TestMethod]
        public void Pattern_LiteralBraces_Preserved()
        {
            var formatter = new DateTimeFormatter("{openbrace}hour{closebrace}: {hour.integer}");

            var result = formatter.Format(TestDate);

            Assert.IsTrue(result.StartsWith("{hour}:"),
                          $"Expected literal braces, got: {result}");
        }

        #endregion

        #region Template Tests

        [TestMethod]
        public void Template_ShortDate_FormatsCorrectly()
        {
            var formatter = new DateTimeFormatter("shortdate");

            var result = formatter.Format(TestDate);

            // Should contain month, day, and year components
            Assert.IsTrue(result.Contains("9") || result.Contains("15") || result.Contains("2024"),
                          $"Expected date components, got: {result}");
        }

        [TestMethod]
        public void Template_LongTime_FormatsCorrectly()
        {
            var formatter = new DateTimeFormatter("longtime");

            var result = formatter.Format(TestDate);

            // Should contain hours, minutes, seconds
            Assert.IsTrue(result.Contains(":"),
                          $"Expected time separators, got: {result}");
        }

        #endregion

        #region Timezone Tests

        [TestMethod]
        public void Format_WithTimezoneId_ConvertsCorrectly()
        {
            var formatter = new DateTimeFormatter(
                "{hour.integer(2)}:{minute.integer(2)}",
                ["en-US"],
                "US",
                CalendarIdentifiers.Gregorian,
                ClockIdentifiers.TwentyFourHour);

            // UTC time
            var utcTime = new DateTimeOffset(2024, 1, 15, 12, 0, 0, TimeSpan.Zero);

            // Format in Pacific time (UTC-8)
            var result = formatter.Format(utcTime, "America/Los_Angeles");

            // Should show 04:00 (12:00 UTC - 8 hours)
            Assert.AreEqual("04:00", result);
        }

        #endregion
    }
}
```

### Unit Tests: Calendar

#### Test File: `CalendarTests.cs`

```csharp
namespace Uno.UWP.Tests.Globalization
{
    [TestClass]
    public class CalendarTests
    {
        #region Numeral System Tests

        [TestMethod]
        public void NumeralSystem_Arabic_TranslatesDigits()
        {
            var calendar = new Calendar(["ar-SA"]);
            calendar.NumeralSystem = "Arab";
            calendar.SetDateTime(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));

            var result = calendar.YearAsString();

            // Arabic-Indic digits for 2024: ٢٠٢٤
            Assert.AreEqual("٢٠٢٤", result);
        }

        [TestMethod]
        public void NumeralSystem_Latin_NoTranslation()
        {
            var calendar = new Calendar(["en-US"]);
            calendar.NumeralSystem = "Latn";
            calendar.SetDateTime(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));

            var result = calendar.YearAsString();

            Assert.AreEqual("2024", result);
        }

        #endregion

        #region Hour Tests

        [TestMethod]
        public void Hour_TwelveHourClock_MidnightIs12()
        {
            var calendar = new Calendar(["en-US"], CalendarIdentifiers.Gregorian, ClockIdentifiers.TwelveHour);
            calendar.SetDateTime(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));

            Assert.AreEqual(12, calendar.Hour);
        }

        [TestMethod]
        public void Hour_TwelveHourClock_1AMIs1()
        {
            var calendar = new Calendar(["en-US"], CalendarIdentifiers.Gregorian, ClockIdentifiers.TwelveHour);
            calendar.SetDateTime(new DateTimeOffset(2024, 1, 1, 1, 0, 0, TimeSpan.Zero));

            Assert.AreEqual(1, calendar.Hour);
        }

        [TestMethod]
        public void Hour_TwelveHourClock_NoonIs12()
        {
            var calendar = new Calendar(["en-US"], CalendarIdentifiers.Gregorian, ClockIdentifiers.TwelveHour);
            calendar.SetDateTime(new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero));

            Assert.AreEqual(12, calendar.Hour);
        }

        [TestMethod]
        public void Hour_TwelveHourClock_1PMIs1()
        {
            var calendar = new Calendar(["en-US"], CalendarIdentifiers.Gregorian, ClockIdentifiers.TwelveHour);
            calendar.SetDateTime(new DateTimeOffset(2024, 1, 1, 13, 0, 0, TimeSpan.Zero));

            Assert.AreEqual(1, calendar.Hour);
        }

        [TestMethod]
        public void Hour_TwentyFourHourClock_1PMIs13()
        {
            var calendar = new Calendar(["en-US"], CalendarIdentifiers.Gregorian, ClockIdentifiers.TwentyFourHour);
            calendar.SetDateTime(new DateTimeOffset(2024, 1, 1, 13, 0, 0, TimeSpan.Zero));

            Assert.AreEqual(13, calendar.Hour);
        }

        #endregion

        #region Period Tests

        [TestMethod]
        public void Period_TwelveHourClock_AMIs1()
        {
            var calendar = new Calendar(["en-US"], CalendarIdentifiers.Gregorian, ClockIdentifiers.TwelveHour);
            calendar.SetDateTime(new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero));

            Assert.AreEqual(1, calendar.Period);
        }

        [TestMethod]
        public void Period_TwelveHourClock_PMIs2()
        {
            var calendar = new Calendar(["en-US"], CalendarIdentifiers.Gregorian, ClockIdentifiers.TwelveHour);
            calendar.SetDateTime(new DateTimeOffset(2024, 1, 1, 14, 0, 0, TimeSpan.Zero));

            Assert.AreEqual(2, calendar.Period);
        }

        [TestMethod]
        public void Period_TwentyFourHourClock_AlwaysIs1()
        {
            var calendar = new Calendar(["en-US"], CalendarIdentifiers.Gregorian, ClockIdentifiers.TwentyFourHour);
            calendar.SetDateTime(new DateTimeOffset(2024, 1, 1, 14, 0, 0, TimeSpan.Zero));

            Assert.AreEqual(1, calendar.Period);
        }

        #endregion

        #region Japanese Calendar Era Tests

        [TestMethod]
        public void JapaneseCalendar_Era_Reiwa()
        {
            var calendar = new Calendar(["ja-JP"], CalendarIdentifiers.Japanese, ClockIdentifiers.TwentyFourHour);
            calendar.SetDateTime(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.FromHours(9)));

            // Era 5 is Reiwa (started May 1, 2019)
            Assert.AreEqual(5, calendar.Era);
        }

        [TestMethod]
        public void JapaneseCalendar_Year_InReiwa()
        {
            var calendar = new Calendar(["ja-JP"], CalendarIdentifiers.Japanese, ClockIdentifiers.TwentyFourHour);
            calendar.SetDateTime(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.FromHours(9)));

            // 2024 is Reiwa 6
            Assert.AreEqual(6, calendar.Year);
        }

        [TestMethod]
        public void JapaneseCalendar_EraAsString_Full()
        {
            var calendar = new Calendar(["ja-JP"], CalendarIdentifiers.Japanese, ClockIdentifiers.TwentyFourHour);
            calendar.SetDateTime(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.FromHours(9)));

            var era = calendar.EraAsString();

            Assert.IsTrue(era == "令和" || era == "Reiwa",
                          $"Expected Reiwa era, got: {era}");
        }

        [TestMethod]
        public void JapaneseCalendar_EraAsString_Abbreviated()
        {
            var calendar = new Calendar(["ja-JP"], CalendarIdentifiers.Japanese, ClockIdentifiers.TwentyFourHour);
            calendar.SetDateTime(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.FromHours(9)));

            var era = calendar.EraAsString(1);

            Assert.IsTrue(era == "R" || era == "令",
                          $"Expected abbreviated era, got: {era}");
        }

        #endregion
    }
}
```

### Integration Tests: TimePicker

#### Test File: `TimePickerIntegrationTests.cs`

```csharp
namespace Uno.UI.RuntimeTests.Tests.TimePicker
{
    [TestClass]
    public class TimePickerClockOverrideTests
    {
        [TestMethod]
        public async Task TimePicker_TwelveHourClock_CzechCulture_ShowsPeriod()
        {
            // This test verifies the fix for GitHub issue #19349

            var timePicker = new TimePicker
            {
                ClockIdentifier = ClockIdentifiers.TwelveHour,
                Time = new TimeSpan(14, 30, 0) // 2:30 PM
            };

            await UITestHelper.Load(timePicker);

            // Verify the period selector is visible
            var periodPicker = timePicker.FindChild<Selector>("PeriodPicker");
            Assert.IsNotNull(periodPicker, "Period picker should exist for 12-hour clock");
            Assert.AreEqual(Visibility.Visible, periodPicker.Visibility);

            // Verify hour shows 2, not 14
            var hourPicker = timePicker.FindChild<Selector>("HourPicker");
            Assert.IsNotNull(hourPicker);
            // The selected value should represent 2 (for 2:30 PM)
        }

        [TestMethod]
        public async Task TimePicker_TwentyFourHourClock_USCulture_NoPeriod()
        {
            var timePicker = new TimePicker
            {
                ClockIdentifier = ClockIdentifiers.TwentyFourHour,
                Time = new TimeSpan(14, 30, 0)
            };

            await UITestHelper.Load(timePicker);

            // Period should be hidden for 24-hour clock
            var periodHost = timePicker.FindChild<Border>("ThirdPickerHost");
            // The period picker host should be collapsed when period is not used
        }
    }
}
```

## Implementation Checklist

### Phase 1 Integration
- [ ] Update all pattern nodes to use `DateTimeFormattingContext`
- [ ] Update `PatternRootNode.Format` to create context
- [ ] Update `DateTimeFormatter.Format` to pass context

### Phase 2 Integration
- [ ] Verify `PatternBuilder.TransformPatternForClock` works correctly
- [ ] Ensure `BuildPattern` applies transformations before converting to WinRT pattern

### Phase 3 Integration
- [ ] Ensure Calendar string methods apply numeral translation
- [ ] Ensure Calendar uses configured calendar system for all operations
- [ ] Ensure Calendar respects clock setting for hour/period

### TimePicker Fix
- [ ] Remove workaround code from `TimePicker.partial.mux.cs`
- [ ] Remove workaround code from `TimePickerFlyoutPresenter.partial.mux.cs`
- [ ] Verify TimePicker works correctly with clock override

### Test Coverage
- [ ] Add DateTimeFormatter unit tests
- [ ] Add Calendar unit tests
- [ ] Add TimePicker integration tests
- [ ] Test various calendar systems (Gregorian, Japanese, Hebrew, Hijri)
- [ ] Test various clock configurations
- [ ] Test various cultures (en-US, cs-CZ, ar-SA, ja-JP, he-IL, ko-KR)

## Validation Against WinRT

To ensure our implementation matches WinRT, create comparison tests that can run on both platforms:

```csharp
#if WINDOWS_UWP
[TestMethod]
public void CompareWithWinRT_ShortTime_CzechWith12Hour()
{
    var formatter = new DateTimeFormatter(
        "shorttime",
        ["cs-CZ"],
        "CZ",
        CalendarIdentifiers.Gregorian,
        ClockIdentifiers.TwelveHour);

    var testDate = new DateTimeOffset(2024, 1, 15, 14, 30, 0, TimeSpan.FromHours(1));
    var result = formatter.Format(testDate);

    // Log result for comparison with Uno implementation
    Debug.WriteLine($"WinRT result: {result}");

    // This test is primarily for capturing expected behavior
    // The actual assertion would compare against known WinRT output
}
#endif
```

## Files to Create/Modify

### New Files
- `src/Uno.UWP.Tests/Globalization/DateTimeFormatting/DateTimeFormatterTests.cs`
- `src/Uno.UWP.Tests/Globalization/CalendarTests.cs`
- `src/Uno.UI.RuntimeTests/Tests/TimePicker/TimePickerClockOverrideTests.cs`

### Files to Modify
- `src/Uno.UI/UI/Xaml/Controls/TimePicker/TimePicker.partial.mux.cs` (remove workarounds)
- `src/Uno.UI/UI/Xaml/Controls/TimePicker/TimePickerFlyoutPresenter.partial.mux.cs` (remove workarounds)

## Success Criteria

1. **Issue #19349 Resolved**: TimePicker with 12-hour clock on Czech culture displays correctly
2. **All Unit Tests Pass**: DateTimeFormatter and Calendar tests pass
3. **No Regressions**: Existing functionality continues to work
4. **Cross-Platform Consistency**: Behavior matches WinRT on Windows

## Documentation

After implementation, update documentation:
- [ ] Add remarks to DateTimeFormatter about clock override behavior
- [ ] Add remarks to Calendar about numeral system support
- [ ] Update any existing samples that demonstrate date/time formatting
