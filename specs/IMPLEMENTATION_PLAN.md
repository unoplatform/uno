# DateTimeFormatter Implementation Plan

## Issue Reference
- GitHub Issue: [#19349 - Improve DateTimeFormatter pattern building](https://github.com/unoplatform/uno/issues/19349)

## Problem Summary

The current `DateTimeFormatter` implementation relies solely on `CultureInfo` to provide formatting patterns. This doesn't work correctly when users override aspects of the formatter (like setting a custom `Clock`). For example:

- A `TimePicker` with 12-hour clock on Czech culture (which defaults to 24-hour) shows incorrect format
- The override settings are not reflected in the generated pattern

## Solution Overview

Implement full support for the WinRT DateTimeFormatter grammar and ensure the Calendar API is properly integrated for formatting. This involves:

1. **Enhanced Pattern Nodes**: Pattern nodes should use the Calendar API for formatting, respecting clock/calendar/timezone settings
2. **Pattern Transformation**: Templates should be transformed to match explicit clock/calendar overrides
3. **Complete Calendar API**: The Calendar class must properly implement all formatting methods with numeral system support
4. **Integration**: Ensure TimePicker and other consumers work correctly with the fixes

## Phase Structure

### Phase 1: Pattern Parser Enhancement
**Goal**: Update pattern nodes to use formatting context with Calendar API

**Key Changes**:
- Create `DateTimeFormattingContext` to carry formatting settings
- Update `PatternDateTimeNode` and subclasses to use context
- All formatting should go through Calendar methods

**Spec**: [phase1-pattern-parser.md](phase1-pattern-parser.md)

### Phase 2: Template Parser Enhancement
**Goal**: Ensure templates generate correct patterns for clock/calendar overrides

**Key Changes**:
- Enhance `PatternBuilder.TransformPatternForClock` for edge cases
- Add culture-aware period position detection
- Handle calendar-specific pattern adjustments

**Spec**: [phase2-template-parser.md](phase2-template-parser.md)

### Phase 3: Calendar API Implementation
**Goal**: Complete and correct the Calendar class implementation

**Key Changes**:
- Add numeral system translation to all formatting methods
- Make year/month/era formatting calendar-aware
- Fix hour/period formatting to respect clock setting
- Add support for non-Gregorian calendar month/era names

**Spec**: [phase3-calendar-api.md](phase3-calendar-api.md)

### Phase 4: Integration and Testing
**Goal**: Integrate all components and verify correctness

**Key Changes**:
- Remove workarounds from TimePicker
- Create comprehensive unit tests
- Create integration tests
- Validate against WinRT behavior

**Spec**: [phase4-integration-testing.md](phase4-integration-testing.md)

## Implementation Order

The recommended order for implementation is:

```
Phase 3 (Calendar API) → Phase 1 (Pattern Nodes) → Phase 2 (Templates) → Phase 4 (Integration)
```

**Rationale**:
1. Phase 3 is foundational - pattern nodes will call Calendar methods
2. Phase 1 depends on Phase 3 - pattern nodes need Calendar to be correct
3. Phase 2 can be done in parallel with Phase 1
4. Phase 4 validates everything works together

## Key Files

### Core Implementation
- `src/Uno.UWP/Globalization/Calendar.cs`
- `src/Uno.UWP/Globalization/DateTimeFormatting/DateTimeFormatter.cs`
- `src/Uno.UWP/Globalization/DateTimeFormatting/DateTimeFormattingContext.cs`
- `src/Uno.UWP/Globalization/DateTimeFormatting/PatternBuilder.cs`
- `src/Uno.UWP/Globalization/DateTimeFormatting/DateTimePatternParser/PatternNodes.cs`

### Consumer Files (to update after fix)
- `src/Uno.UI/UI/Xaml/Controls/TimePicker/TimePicker.partial.mux.cs`
- `src/Uno.UI/UI/Xaml/Controls/TimePicker/TimePickerFlyoutPresenter.partial.mux.cs`

## Test Scenarios

### Critical Test Cases
1. **Czech + 12-hour clock**: Must show AM/PM equivalent
2. **US English + 24-hour clock**: Must NOT show AM/PM
3. **Japanese calendar**: Must show correct era and year
4. **Arabic numeral system**: Must translate digits
5. **Midnight in 12-hour**: Must show 12, not 0
6. **Noon in 12-hour**: Must show 12, not 0

### Cultures to Test
- `en-US` (12-hour default)
- `cs-CZ` (24-hour default)
- `ja-JP` (Japanese calendar)
- `ar-SA` (Arabic numerals, Hijri calendar)
- `he-IL` (Hebrew calendar)
- `ko-KR` (period before time)

## Success Criteria

1. ✅ Issue #19349 is resolved
2. ✅ TimePicker works correctly with clock overrides
3. ✅ All formatting respects explicit clock/calendar/timezone settings
4. ✅ Non-Gregorian calendars display correct month/era names
5. ✅ Numeral system translation works
6. ✅ No regressions in existing functionality
7. ✅ Behavior matches WinRT on Windows

## Risk Mitigation

### Backward Compatibility
- Existing patterns should continue to work
- Default behavior (no overrides) should be unchanged

### Performance
- Calendar instance reuse where possible
- Avoid creating unnecessary objects during formatting

### Testing
- Comprehensive unit tests for edge cases
- Integration tests with actual controls
- Cross-platform validation (compare with Windows)

## Timeline Estimate

Each phase can be completed incrementally. The work is designed to allow for partial progress and testing at each phase.

---

*This plan was created as part of the Ralph Loop implementation process. Each phase has a detailed specification document with implementation tasks and testing criteria.*
