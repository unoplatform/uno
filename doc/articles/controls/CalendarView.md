---
uid: Uno.Controls.CalendarView
---

# CalendarView

The `CalendarView` control in Uno Platform provides a way to display and select dates using a calendar interface. It supports various features including date selection, density indicators, and customizable appearance.

## Density Colors

The `CalendarViewDayItem.SetDensityColors` method allows you to display up to 10 colored density bars on individual calendar day items. This feature is useful for visualizing event density or activity levels for specific days.

### Usage

To use density colors, handle the `CalendarViewDayItemChanging` event and call `SetDensityColors` on the day item:

```csharp
private void OnCalendarViewDayItemChanging(CalendarView sender, CalendarViewDayItemChangingEventArgs args)
{
    // Create a list of colors (up to 10)
    var colors = new List<Color>
    {
        Colors.Red,
        Colors.Orange,
        Colors.Yellow
    };
    
    // Apply the density colors to the day item
    args.Item.SetDensityColors(colors);
}
```

### Parameters

- **colors**: An `IEnumerable<Color>` containing the colors to display as density bars. Maximum of 10 colors are supported. Pass `null` or an empty collection to clear the density bars.

### Example: Event Density Visualization

```csharp
public sealed partial class MyPage : Page
{
    private Dictionary<DateTimeOffset, List<Color>> _eventDensityData;
    
    public MyPage()
    {
        this.InitializeComponent();
        
        // Initialize event density data
        _eventDensityData = new Dictionary<DateTimeOffset, List<Color>>();
        
        // Sample data: Add density bars for specific dates
        var today = DateTimeOffset.Now.Date;
        _eventDensityData[today] = new List<Color> 
        { 
            Colors.Green,  // Confirmed event
            Colors.Green,  // Confirmed event
            Colors.Blue    // Tentative event
        };
        
        _eventDensityData[today.AddDays(1)] = new List<Color>
        {
            Colors.Green,
            Colors.Green,
            Colors.Green,
            Colors.Blue,
            Colors.Blue
        };
    }
    
    private void CalendarView_CalendarViewDayItemChanging(
        CalendarView sender, 
        CalendarViewDayItemChangingEventArgs args)
    {
        // Check if we have density data for this date
        var date = args.Item.Date.Date;
        if (_eventDensityData.TryGetValue(date, out var colors))
        {
            args.Item.SetDensityColors(colors);
        }
        else
        {
            // Clear density bars for days without events
            args.Item.SetDensityColors(null);
        }
    }
}
```

### XAML Setup

```xml
<CalendarView 
    x:Name="MyCalendar"
    CalendarViewDayItemChanging="CalendarView_CalendarViewDayItemChanging" />
```

### Notes

- The visual rendering of density bars depends on the CalendarView template and styling
- Density colors should be set each time the `CalendarViewDayItemChanging` event occurs, as day items are recycled
- Colors represent density indicators for each day item
- If more than 10 colors are provided, only the first 10 will be used
- Transparent and semi-transparent colors are supported

### Platform Support

The `SetDensityColors` method is supported on all Uno Platform targets:
- WebAssembly
- Skia (Desktop)
- iOS
- Android
- macOS
- Windows

## See Also

- [CalendarView Class (Microsoft Docs)](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.calendarview)
- [CalendarViewDayItem.SetDensityColors Method (Microsoft Docs)](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.calendarviewdayitem.setdensitycolors)
