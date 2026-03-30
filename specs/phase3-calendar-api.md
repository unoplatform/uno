# Phase 3: Calendar API Implementation

## Overview

This phase focuses on completing and correcting the `Windows.Globalization.Calendar` API implementation to match WinRT behavior. The Calendar class is critical for DateTimeFormatter as pattern nodes will use Calendar methods for formatting values according to the configured calendar system, clock, and locale.

## Current State Analysis

The current `Calendar.cs` implementation has:

### What's Implemented
- Basic constructors with language, calendar, clock, and timezone support
- Date/time component properties (Year, Month, Day, Hour, Minute, Second, etc.)
- Add methods (AddYears, AddMonths, AddDays, etc.)
- First/Last/Number properties for components
- Basic string formatting methods
- Calendar system switching
- Clock switching (12/24 hour)
- Timezone handling

### Issues Identified

1. **String Formatting Uses DateTimeOffset Directly**: Methods like `YearAsString()` use `_time.Year.ToString()` instead of going through the calendar system.

2. **MonthAsString Not Calendar-Aware**: Returns culture-based month names but doesn't account for different calendar systems (e.g., Hebrew months).

3. **Hour Property**: The getter uses `_calendar.GetHour()` but also does 12-hour conversion based on clock. This may conflict with how `HourAsString` works.

4. **Numeral System**: The `NumeralSystem` property is stored but not applied to formatting output.

5. **Era Handling**: `EraAsString()` has custom logic for Japanese eras but may not handle all calendar systems correctly.

6. **First/Last Properties**: Some properties like `FirstHourInThisPeriod` return fixed values that don't account for all scenarios.

## WinRT Calendar API Specification

### Constructors

```csharp
// Default: current culture, current calendar, current clock, local timezone
public Calendar();

// Specified languages only
public Calendar(IEnumerable<string> languages);

// Languages, calendar system, and clock
public Calendar(IEnumerable<string> languages, string calendar, string clock);

// Full specification including timezone
public Calendar(IEnumerable<string> languages, string calendar, string clock, string timeZoneId);
```

### Date/Time Component Properties

All properties should work through the calendar system, not directly on DateTime:

| Property | Type | Description |
|----------|------|-------------|
| Era | int | Gets/sets era (calendar-specific) |
| Year | int | Gets/sets year within current era |
| Month | int | Gets/sets month (1-based) |
| Day | int | Gets/sets day of month |
| DayOfWeek | DayOfWeek | Gets day of week (read-only) |
| Hour | int | Gets/sets hour (affected by clock setting) |
| Minute | int | Gets/sets minute |
| Second | int | Gets/sets second |
| Nanosecond | int | Gets/sets nanosecond |
| Period | int | Gets/sets period (1=AM/first, 2=PM/second) |

### String Formatting Methods

All string formatting should:
1. Use the calendar system to get the value
2. Use the resolved language for localization
3. Apply the numeral system for digit translation

#### Year Methods
```csharp
// Returns year without padding
string YearAsString();

// Returns year padded to at least minDigits
string YearAsPaddedString(int minDigits);

// Returns last remainingDigits of year (e.g., 2024 with 2 → "24")
string YearAsTruncatedString(int remainingDigits);
```

#### Month Methods
```csharp
// Returns numeric month without padding
string MonthAsNumericString();

// Returns numeric month padded to minDigits
string MonthAsPaddedNumericString(int minDigits);

// Returns month name suitable for use with other components (may be genitive)
string MonthAsString();

// Returns month name close to idealLength
string MonthAsString(int idealLength);

// Returns month name for standalone display (nominative)
string MonthAsSoloString();

// Returns month name for standalone display, close to idealLength
string MonthAsSoloString(int idealLength);
```

#### Day Methods
```csharp
// Returns day without padding
string DayAsString();

// Returns day padded to minDigits
string DayAsPaddedString(int minDigits);
```

#### Day of Week Methods
```csharp
// Returns full day name suitable for use with other components
string DayOfWeekAsString();

// Returns day name close to idealLength
string DayOfWeekAsString(int idealLength);

// Returns day name for standalone display
string DayOfWeekAsSoloString();

// Returns day name for standalone display, close to idealLength
string DayOfWeekAsSoloString(int idealLength);
```

#### Period Methods
```csharp
// Returns period designator (AM/PM equivalent)
string PeriodAsString();

// Returns period designator close to idealLength
string PeriodAsString(int idealLength);
```

#### Hour Methods
```csharp
// Returns hour without padding (respects clock setting)
string HourAsString();

// Returns hour padded to minDigits (respects clock setting)
string HourAsPaddedString(int minDigits);
```

#### Minute/Second Methods
```csharp
string MinuteAsString();
string MinuteAsPaddedString(int minDigits);
string SecondAsString();
string SecondAsPaddedString(int minDigits);
```

#### Nanosecond Methods
```csharp
string NanosecondAsString();
string NanosecondAsPaddedString(int minDigits);
```

#### Era Methods
```csharp
// Returns era designation
string EraAsString();

// Returns era designation close to idealLength
string EraAsString(int idealLength);
```

#### Timezone Methods
```csharp
// Returns full timezone name at current instant
string TimeZoneAsString();

// Returns timezone abbreviation close to idealLength
string TimeZoneAsString(int idealLength);
```

### Numeral System Support

The `NumeralSystem` property specifies which numeral system to use for digit output:

| System ID | Name | Digits |
|-----------|------|--------|
| Latn | Latin | 0123456789 |
| Arab | Arabic-Indic | ٠١٢٣٤٥٦٧٨٩ |
| ArabExt | Extended Arabic-Indic | ۰۱۲۳۴۵۶۷۸۹ |
| Deva | Devanagari | ०१२३४५६७८९ |
| Thai | Thai | ๐๑๒๓๔๕๖๗๘๙ |
| Beng | Bengali | ০১২৩৪৫৬৭৮৯ |
| ... | ... | ... |

Implementation should translate Latin digits to the specified numeral system in all `*AsString` methods.

## Implementation Specification

### 1. Numeral System Translation

Create a helper to translate digits:

```csharp
private string TranslateNumerals(string input)
{
    if (_numeralSystem == "Latn")
        return input;

    var digits = NumeralSystemTranslatorHelper.GetDigitsSource(_numeralSystem);
    if (digits == NumeralSystemTranslatorHelper.EmptyDigits)
        return input;

    var sb = new StringBuilder(input.Length);
    foreach (var c in input)
    {
        if (c >= '0' && c <= '9')
            sb.Append(digits[c - '0']);
        else
            sb.Append(c);
    }
    return sb.ToString();
}
```

### 2. Year Formatting

```csharp
public string YearAsString()
{
    var year = _calendar.GetYear(DateTime);
    return TranslateNumerals(year.ToString(_resolvedCulture));
}

public string YearAsPaddedString(int minDigits)
{
    var year = _calendar.GetYear(DateTime);
    return TranslateNumerals(year.ToString(new string('0', minDigits), _resolvedCulture));
}

public string YearAsTruncatedString(int remainingDigits)
{
    var year = _calendar.GetYear(DateTime);
    var divisor = (int)Math.Pow(10, remainingDigits);
    var truncatedYear = year % divisor;
    return TranslateNumerals(truncatedYear.ToString(new string('0', remainingDigits), _resolvedCulture));
}
```

### 3. Month Formatting

For calendar-aware month names, we need to handle cases where .NET's CultureInfo doesn't have the right names:

```csharp
public string MonthAsString()
{
    var month = _calendar.GetMonth(DateTime);

    // Try to get calendar-specific month name
    if (TryGetCalendarMonthName(month, abbreviated: false, out var name))
        return name;

    // Fall back to culture-based name
    return _resolvedCulture.DateTimeFormat.GetMonthName(month);
}

private bool TryGetCalendarMonthName(int month, bool abbreviated, out string name)
{
    // Handle special calendars that .NET doesn't have proper names for
    var calendarSystem = GetCalendarSystem();

    switch (calendarSystem)
    {
        case CalendarIdentifiers.Hebrew:
            name = GetHebrewMonthName(month, abbreviated);
            return true;
        case CalendarIdentifiers.Hijri:
        case CalendarIdentifiers.UmAlQura:
            name = GetHijriMonthName(month, abbreviated);
            return true;
        // ... other special cases
        default:
            name = null;
            return false;
    }
}
```

### 4. Hour Formatting with Clock Awareness

The `Hour` property and `HourAsString` should be consistent:

```csharp
public int Hour
{
    get
    {
        var hour = _calendar.GetHour(DateTime);

        if (_clock == ClockIdentifiers.TwelveHour)
        {
            // 12-hour clock: 12, 1, 2, ..., 11
            if (hour == 0) return 12;
            if (hour > 12) return hour - 12;
        }

        return hour;
    }
    // setter implementation...
}

public string HourAsString()
{
    var hour = Hour; // Uses the property which already handles clock
    return TranslateNumerals(hour.ToString(_resolvedCulture));
}

public string HourAsPaddedString(int minDigits)
{
    var hour = Hour;
    return TranslateNumerals(hour.ToString(new string('0', minDigits), _resolvedCulture));
}
```

### 5. Period Formatting

```csharp
public string PeriodAsString()
{
    if (_clock == ClockIdentifiers.TwentyFourHour)
    {
        // For 24-hour clock, period is conceptually always 1
        // but we can still return the AM designator for consistency
        return string.Empty; // or culture's AM designator
    }

    var hour = _calendar.GetHour(DateTime);
    return hour < 12
        ? _resolvedCulture.DateTimeFormat.AMDesignator
        : _resolvedCulture.DateTimeFormat.PMDesignator;
}

public string PeriodAsString(int idealLength)
{
    var period = PeriodAsString();
    if (idealLength > 0 && period.Length > idealLength)
    {
        return period.Substring(0, idealLength);
    }
    return period;
}
```

### 6. Timezone Formatting

```csharp
public string TimeZoneAsString()
{
    // Return full timezone name at the current instant
    var isDst = _timeZone.IsDaylightSavingTime((DateTimeOffset)_time);
    return isDst ? _timeZone.DaylightName : _timeZone.StandardName;
}

public string TimeZoneAsString(int idealLength)
{
    // For abbreviated, return UTC offset format
    var offset = _timeZone.GetUtcOffset((DateTimeOffset)_time);
    var sign = offset >= TimeSpan.Zero ? "+" : "";
    var hours = (int)offset.TotalHours;
    var minutes = Math.Abs(offset.Minutes);

    if (idealLength <= 3)
    {
        // Just offset: +9 or +09
        return $"{sign}{hours}";
    }
    else if (minutes > 0)
    {
        return $"{sign}{hours}:{minutes:00}";
    }
    return $"{sign}{hours}";
}
```

### 7. Era Handling

```csharp
public string EraAsString()
{
    return GetEraName(_calendar, Era, _resolvedCulture, idealLength: 0);
}

public string EraAsString(int idealLength)
{
    return GetEraName(_calendar, Era, _resolvedCulture, idealLength);
}

private static string GetEraName(System.Globalization.Calendar calendar, int era, CultureInfo culture, int idealLength)
{
    // First try .NET's era names
    var eraName = culture.DateTimeFormat.GetEraName(era);
    if (!string.IsNullOrEmpty(eraName))
    {
        if (idealLength > 0 && idealLength < eraName.Length)
        {
            var abbreviated = culture.DateTimeFormat.GetAbbreviatedEraName(era);
            if (!string.IsNullOrEmpty(abbreviated))
                return abbreviated;
        }
        return eraName;
    }

    // Calendar-specific fallbacks
    return calendar switch
    {
        JapaneseCalendar => GetJapaneseEraName(era, idealLength),
        HebrewCalendar => idealLength == 1 ? "א" : "א.מ.",
        HijriCalendar => idealLength == 1 ? "ه" : "هـ",
        // ... other calendars
        _ => idealLength == 1 ? "A" : "A.D."
    };
}
```

## Implementation Tasks

1. [ ] Add numeral system translation helper method
2. [ ] Update `YearAsString`, `YearAsPaddedString`, `YearAsTruncatedString` to use calendar
3. [ ] Update month formatting methods to be calendar-aware
4. [ ] Add Hebrew month name support
5. [ ] Add Hijri month name support
6. [ ] Update `HourAsString`, `HourAsPaddedString` to use `Hour` property
7. [ ] Update `PeriodAsString` to respect clock setting
8. [ ] Update timezone formatting methods
9. [ ] Improve era handling for various calendars
10. [ ] Apply numeral translation to all formatting methods
11. [ ] Add `MonthAsFullString()` internal method (alias for `MonthAsString`)
12. [ ] Add `DayOfWeekAsFullString()` internal method

## Testing Criteria

### Test 1: Numeral System
```csharp
var calendar = new Calendar(["ar-SA"]);
calendar.NumeralSystem = "Arab";
calendar.Year = 2024;
Assert.Equal("٢٠٢٤", calendar.YearAsString());
```

### Test 2: Japanese Calendar Era
```csharp
var calendar = new Calendar(["ja-JP"], CalendarIdentifiers.Japanese, ClockIdentifiers.TwentyFourHour);
calendar.SetDateTime(new DateTimeOffset(2024, 1, 28, 0, 0, 0, TimeSpan.FromHours(9)));
Assert.Equal("令和", calendar.EraAsString());
Assert.Equal("6", calendar.YearAsString()); // Year 6 of Reiwa
```

### Test 3: 12-Hour Clock Hour Formatting
```csharp
var calendar = new Calendar(["en-US"], CalendarIdentifiers.Gregorian, ClockIdentifiers.TwelveHour);
calendar.SetDateTime(new DateTimeOffset(2024, 1, 28, 0, 30, 0, TimeSpan.Zero)); // Midnight
Assert.Equal(12, calendar.Hour);
Assert.Equal("12", calendar.HourAsString());
Assert.Equal("AM", calendar.PeriodAsString());
```

### Test 4: Hebrew Month Names
```csharp
var calendar = new Calendar(["he-IL"], CalendarIdentifiers.Hebrew, ClockIdentifiers.TwentyFourHour);
// Set to a date in Hebrew calendar
// Verify Hebrew month name is returned
```

## Files to Modify

- `src/Uno.UWP/Globalization/Calendar.cs`

## Dependencies

- None (this is foundational for other phases)
