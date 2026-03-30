# Phase 1: DateTimeFormatter Pattern Parser Enhancement

## Overview

This phase focuses on ensuring the pattern parser fully supports the WinRT DateTimeFormatter pattern grammar, including proper handling of the formatting context (clock, calendar, timezone) when rendering values.

## Current State Analysis

The current `PatternParser.cs` and `PatternNodes.cs` implementation already handles most pattern elements correctly:

### What's Already Implemented
- Pattern parsing with literal text support (`{openbrace}`, `{closebrace}`)
- Era: `{era.abbreviated(n)}`
- Year: `{year.full(n)}`, `{year.abbreviated(n)}`
- Month: `{month.full}`, `{month.solo.full}`, `{month.abbreviated(n)}`, `{month.solo.abbreviated(n)}`, `{month.integer(n)}`
- Day: `{day.integer(n)}`
- Day of Week: `{dayofweek.full}`, `{dayofweek.solo.full}`, `{dayofweek.abbreviated(n)}`, `{dayofweek.solo.abbreviated(n)}`
- Period: `{period.abbreviated(n)}`
- Hour: `{hour.integer(n)}`
- Minute: `{minute.integer(n)}`
- Second: `{second.integer(n)}`
- Timezone: `{timezone.full}`, `{timezone.abbreviated(n)}`

### What Needs Enhancement

1. **Context-Aware Formatting**: Pattern nodes need access to the formatting context (clock identifier, calendar system, timezone) to render correctly. Currently, they only receive `isTwentyFourHours` as a boolean.

2. **Calendar-Based Year/Era Handling**: Currently uses `DateTimeOffset.Year` directly, but should use the configured calendar system.

3. **Numeral System Support**: The `NumeralSystem` property is not being used during formatting.

## Detailed Specification

### 1. Create DateTimeFormattingContext

A context object that carries all formatting settings:

```csharp
internal sealed class DateTimeFormattingContext
{
    public CultureInfo Culture { get; }
    public string CalendarIdentifier { get; }
    public string ClockIdentifier { get; }
    public string? TimeZoneId { get; }
    public string? NumeralSystem { get; }
    public bool IsTwelveHourClock { get; }
    public Calendar Calendar { get; }
}
```

**Status**: Partially implemented in `DateTimeFormattingContext.cs`

### 2. Update PatternDateTimeNode.Format Signature

Change the `Format` method signature from:
```csharp
void Format(StringBuilder builder, DateTimeOffset dateTime, CultureInfo culture, bool isTwentyFourHours);
```

To:
```csharp
void Format(StringBuilder builder, DateTimeOffset dateTime, DateTimeFormattingContext context);
```

### 3. Calendar-Aware Year Formatting

The `PatternYearNode.Format` method should use the Calendar API for proper year handling:

```csharp
internal override void Format(StringBuilder builder, DateTimeOffset dateTime, DateTimeFormattingContext context)
{
    var calendar = context.Calendar;
    calendar.SetDateTime(dateTime);

    if (Kind == YearKind.Full)
    {
        var year = calendar.Year;
        if (IdealLength.HasValue)
        {
            builder.Append(calendar.YearAsPaddedString(IdealLength.Value));
        }
        else
        {
            builder.Append(calendar.YearAsString());
        }
    }
    else if (Kind == YearKind.Abbreviated)
    {
        if (IdealLength.HasValue)
        {
            builder.Append(calendar.YearAsTruncatedString(IdealLength.Value));
        }
        else
        {
            builder.Append(calendar.YearAsTruncatedString(2));
        }
    }
}
```

### 4. Calendar-Aware Era Formatting

Update `PatternEraNode.Format` to use the provided context:

```csharp
internal override void Format(StringBuilder builder, DateTimeOffset dateTime, DateTimeFormattingContext context)
{
    var calendar = context.Calendar;
    calendar.SetDateTime(dateTime);

    var eraString = IdealLength.HasValue
        ? calendar.EraAsString(IdealLength.Value)
        : calendar.EraAsString();
    builder.Append(eraString);
}
```

### 5. Calendar-Aware Month Formatting

```csharp
internal override void Format(StringBuilder builder, DateTimeOffset dateTime, DateTimeFormattingContext context)
{
    var calendar = context.Calendar;
    calendar.SetDateTime(dateTime);

    switch (Kind)
    {
        case MonthKind.Full:
            builder.Append(calendar.MonthAsString());
            break;
        case MonthKind.SoloFull:
            builder.Append(calendar.MonthAsSoloString());
            break;
        case MonthKind.Abbreviated:
            builder.Append(IdealLength.HasValue
                ? calendar.MonthAsString(IdealLength.Value)
                : calendar.MonthAsString());
            break;
        case MonthKind.SoloAbbreviated:
            builder.Append(IdealLength.HasValue
                ? calendar.MonthAsSoloString(IdealLength.Value)
                : calendar.MonthAsSoloString());
            break;
        case MonthKind.Integer:
            builder.Append(IdealLength.HasValue
                ? calendar.MonthAsPaddedNumericString(IdealLength.Value)
                : calendar.MonthAsNumericString());
            break;
    }
}
```

### 6. Clock-Aware Hour Formatting

The hour formatting must respect the configured clock (12 or 24 hour):

```csharp
internal override void Format(StringBuilder builder, DateTimeOffset dateTime, DateTimeFormattingContext context)
{
    var calendar = context.Calendar;
    calendar.SetDateTime(dateTime);

    builder.Append(IdealLength.HasValue
        ? calendar.HourAsPaddedString(IdealLength.Value)
        : calendar.HourAsString());
}
```

### 7. Numeral System Support

Add numeral system translation to formatted output. The `Calendar` class should handle this internally based on its `NumeralSystem` property.

### 8. Timezone-Aware Formatting

Ensure timezone formatting uses the context's timezone information:

```csharp
internal override void Format(StringBuilder builder, DateTimeOffset dateTime, DateTimeFormattingContext context)
{
    var calendar = context.Calendar;
    calendar.SetDateTime(dateTime);

    if (Kind == TimeZoneKind.Full)
    {
        builder.Append(calendar.TimeZoneAsString());
    }
    else
    {
        builder.Append(IdealLength.HasValue
            ? calendar.TimeZoneAsString(IdealLength.Value)
            : calendar.TimeZoneAsString());
    }
}
```

## Implementation Tasks

1. [ ] Finalize `DateTimeFormattingContext` class with all required properties
2. [ ] Update `PatternDateTimeNode` abstract class with new Format signature
3. [ ] Update `PatternRootNode` to create and pass context
4. [ ] Update `PatternEraNode` to use Calendar API
5. [ ] Update `PatternYearNode` to use Calendar API
6. [ ] Update `PatternMonthNode` to use Calendar API
7. [ ] Update `PatternDayNode` to use Calendar API
8. [ ] Update `PatternDayOfWeekNode` to use Calendar API
9. [ ] Update `PatternHourNode` to use Calendar API (respects clock setting)
10. [ ] Update `PatternMinuteNode` to use Calendar API
11. [ ] Update `PatternSecondNode` to use Calendar API
12. [ ] Update `PatternPeriodNode` to use Calendar API (only outputs for 12-hour clock)
13. [ ] Update `PatternTimeZoneNode` to use Calendar API
14. [ ] Update `DateTimeFormatter.Format()` to create proper context

## Testing Criteria

1. **Clock Override Test**: Create formatter with 12-hour clock on culture that uses 24-hour clock (e.g., Czech). Format should show AM/PM.
2. **Clock Override Test 2**: Create formatter with 24-hour clock on culture that uses 12-hour clock (e.g., en-US). Format should not show AM/PM.
3. **Calendar Test**: Create formatter with different calendar systems (Hebrew, Hijri, Japanese) and verify correct year/era output.
4. **Timezone Test**: Format with explicit timezone and verify correct offset display.
5. **Numeral System Test**: Set numeral system to Arabic-Indic and verify digit output.

## Files to Modify

- `src/Uno.UWP/Globalization/DateTimeFormatting/DateTimeFormattingContext.cs` (enhance)
- `src/Uno.UWP/Globalization/DateTimeFormatting/DateTimePatternParser/PatternNodes.cs` (major changes)
- `src/Uno.UWP/Globalization/DateTimeFormatting/DateTimeFormatter.cs` (update Format methods)

## Dependencies

- Phase 3 (Calendar API) should be done first or in parallel, as pattern nodes will depend on Calendar methods.
