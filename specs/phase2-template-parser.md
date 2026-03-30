# Phase 2: DateTimeFormatter Template Parser Enhancement

## Overview

This phase focuses on enhancing the template-to-pattern conversion to properly respect clock, calendar, and timezone overrides. The core issue from GitHub #19349 is that when a user specifies a custom clock (e.g., 12-hour clock on a culture that defaults to 24-hour), the generated pattern doesn't reflect this override.

## Problem Statement

Current behavior:
1. User creates `DateTimeFormatter("shorttime", languages, region, calendar, "12HourClock")`
2. The formatter uses `CultureInfo.DateTimeFormat.ShortTimePattern`
3. If the culture defaults to 24-hour format (e.g., Czech), the pattern will be "HH:mm"
4. The clock override is ignored in the pattern generation

Expected behavior:
1. The pattern should be transformed to respect the clock override
2. For 12-hour clock with a 24-hour culture pattern: "HH:mm" → "h:mm tt"
3. For 24-hour clock with a 12-hour culture pattern: "h:mm tt" → "HH:mm"

## Current Implementation Analysis

### PatternBuilder.TransformPatternForClock

The `PatternBuilder.cs` file already contains a `TransformPatternForClock` method that:
- Converts 24-hour patterns to 12-hour (H/HH → h/hh, adds tt)
- Converts 12-hour patterns to 24-hour (h/hh → H/HH, removes tt)

### DateTimeFormatter.BuildPattern

The `BuildPattern` method:
1. Selects base patterns from `CultureInfo.DateTimeFormat` (ShortTimePattern, LongTimePattern, etc.)
2. Calls `PatternBuilder.TransformPatternForClock` at the end
3. Converts the .NET format string to WinRT pattern format

## Specification

### 1. Enhanced Template Processing Flow

```
Template String → TemplateParser → TemplateInfo
                        ↓
                  BuildPattern()
                        ↓
        Culture DateTime Patterns (.NET format)
                        ↓
        TransformPatternForClock() [if clock override]
                        ↓
        ConstructPattern() [.NET → WinRT conversion]
                        ↓
                WinRT Pattern String
```

### 2. Pattern Transformation Rules

#### 2.1 Clock Transformation (12-hour ↔ 24-hour)

When `Clock` property is explicitly set and differs from culture default:

| Culture Pattern | Requested Clock | Transformed Pattern |
|-----------------|-----------------|---------------------|
| `HH:mm` | 12HourClock | `h:mm tt` |
| `HH:mm:ss` | 12HourClock | `h:mm:ss tt` |
| `H:mm` | 12HourClock | `h:mm tt` |
| `h:mm tt` | 24HourClock | `HH:mm` |
| `h:mm:ss tt` | 24HourClock | `HH:mm:ss` |

Implementation in `PatternBuilder.TransformPatternForClock`:
- Already handles the basic cases
- Need to ensure proper handling of edge cases:
  - Leading/trailing spaces around period designator
  - Different period designator formats (t vs tt)
  - Patterns with timezone embedded

#### 2.2 Period Designator Position

The period designator (AM/PM) position varies by culture:
- English: `h:mm tt` (after time)
- Korean: `tt h:mm` (before time)

When adding the period designator for 12-hour clock transformation, we should:
1. Check if the culture naturally places period before or after
2. Insert at the appropriate position

```csharp
private static string GetPeriodPosition(CultureInfo culture)
{
    var pattern = culture.DateTimeFormat.ShortTimePattern;
    var periodIndex = pattern.IndexOfAny(['t']);
    var hourIndex = pattern.IndexOfAny(['h', 'H']);

    return periodIndex < hourIndex ? "before" : "after";
}
```

### 3. Calendar-Specific Pattern Adjustments

Different calendars may have different date component ordering requirements:

#### Japanese Calendar
- Era is often included: `{era.abbreviated}{year.full}年{month.integer}月{day.integer}日`

#### Hebrew Calendar
- Right-to-left rendering
- Hebrew month names

#### Hijri Calendar
- Different month names and year counting

The current implementation relies on .NET's `CultureInfo` to provide appropriate patterns, but when a different calendar is specified, we may need to adjust.

### 4. BuildPattern Enhancements

Update the `BuildPattern` method to handle calendar-specific adjustments:

```csharp
private string BuildPattern()
{
    // 1. Get base patterns from culture
    string datePattern = GetDatePattern();
    string timePattern = GetTimePattern();

    // 2. Apply clock transformation to time pattern
    if (!string.IsNullOrEmpty(timePattern))
    {
        timePattern = PatternBuilder.TransformPatternForClock(
            timePattern, Clock, _firstCulture);
    }

    // 3. Apply calendar-specific adjustments to date pattern
    if (Calendar != CalendarIdentifiers.Gregorian)
    {
        datePattern = AdjustPatternForCalendar(datePattern);
    }

    // 4. Combine and convert to WinRT format
    return ConstructPattern(CombinePatterns(datePattern, timePattern));
}
```

### 5. Era Inclusion Logic

Some calendars (Japanese, Hebrew) commonly include era in dates. The pattern should reflect this:

```csharp
private bool ShouldIncludeEra()
{
    // Japanese calendar typically includes era
    if (Calendar == CalendarIdentifiers.Japanese)
        return true;

    // For other calendars, only if explicitly requested in template
    return IncludeEra != EraFormat.None;
}
```

## Implementation Tasks

1. [ ] Verify `TransformPatternForClock` handles all edge cases:
   - Multiple hour specifiers (shouldn't happen but defensive)
   - Quoted strings (literal text) are skipped
   - Various separator styles (colon, dot, space)

2. [ ] Add culture-aware period position detection

3. [ ] Add calendar-specific pattern adjustments:
   - Era inclusion for Japanese calendar
   - Month name format for lunar calendars

4. [ ] Add tests for clock override scenarios:
   - Czech culture + 12-hour clock
   - English culture + 24-hour clock
   - Arabic culture + various clocks (RTL handling)

## Testing Criteria

### Test Case 1: Czech Culture with 12-Hour Clock
```csharp
var formatter = new DateTimeFormatter(
    "shorttime",
    ["cs-CZ"],
    "CZ",
    CalendarIdentifiers.Gregorian,
    ClockIdentifiers.TwelveHour);

// Culture default: "H:mm" (24-hour, no period)
// Expected pattern: "{hour.integer(1)}:{minute.integer(2)} {period.abbreviated}"
// Expected output for 14:30: "2:30 odp."
```

### Test Case 2: English Culture with 24-Hour Clock
```csharp
var formatter = new DateTimeFormatter(
    "shorttime",
    ["en-US"],
    "US",
    CalendarIdentifiers.Gregorian,
    ClockIdentifiers.TwentyFourHour);

// Culture default: "h:mm tt" (12-hour with period)
// Expected pattern: "{hour.integer(2)}:{minute.integer(2)}"
// Expected output for 2:30 PM: "14:30"
```

### Test Case 3: Japanese Calendar with Era
```csharp
var formatter = new DateTimeFormatter(
    "longdate",
    ["ja-JP"],
    "JP",
    CalendarIdentifiers.Japanese,
    ClockIdentifiers.TwentyFourHour);

// Should include era in output: "令和6年1月28日"
```

### Test Case 4: Korean Culture Period Position
```csharp
var formatter = new DateTimeFormatter(
    "shorttime",
    ["ko-KR"],
    "KR",
    CalendarIdentifiers.Gregorian,
    ClockIdentifiers.TwelveHour);

// Period should be before time: "오후 2:30"
```

## Files to Modify

- `src/Uno.UWP/Globalization/DateTimeFormatting/PatternBuilder.cs` (enhance clock transformation)
- `src/Uno.UWP/Globalization/DateTimeFormatting/DateTimeFormatter.cs` (BuildPattern method)

## Dependencies

- This phase can be worked on independently
- Testing will require Phase 1 and Phase 3 to be complete for full validation
