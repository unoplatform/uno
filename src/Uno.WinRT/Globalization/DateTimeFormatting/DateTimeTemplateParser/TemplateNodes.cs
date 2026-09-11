#nullable enable

using System;
using System.Collections.Immutable;

namespace Windows.Globalization.DateTimeFormatting;

internal sealed class DateTimeTemplateInfo
{
	public YearFormat IncludeYear { get; set; }

	public MonthFormat IncludeMonth { get; set; }

	public DayFormat IncludeDay { get; set; }

	public DayOfWeekFormat IncludeDayOfWeek { get; set; }

	public HourFormat IncludeHour { get; set; }

	public MinuteFormat IncludeMinute { get; set; }

	public SecondFormat IncludeSecond { get; set; }

	public TimeZoneFormat IncludeTimeZone { get; set; }

	public bool IsLongTime { get; set; }

	public bool IsShortTime { get; set; }

	public bool IsLongDate { get; set; }

	public bool IsShortDate { get; set; }
}

internal abstract class TemplateNode
{
	internal abstract void Traverse(DateTimeTemplateInfo state);
}

// <template> ::= <opt-whitespace> <date> <opt-whitespace> |
//                <opt-whitespace> <time> <opt-whitespace> |
//                <opt-whitespace> <specific-date> <whitespace> <time> <opt-whitespace> |
//                <opt-whitespace> <time> <whitespace> <specific-date> <opt-whitespace> |
//                <opt-whitespace> <relative-date> <whitespace> <time> <opt-whitespace> |
//                <opt-whitespace> <time> <whitespace> <relative-date> <opt-whitespace>
//
// <opt-whitespace> ::= [<whitespace>] 
//
// <whitespace> ::= " "+ 
internal sealed class TemplateRootNode : TemplateNode
{
	public TemplateRootNode(TemplateNode first, TemplateNode? second = null)
	{
		First = first;
		Second = second;
	}

	// The options for these properties per the grammar above is:
	// 1. First is TemplateDateNode,         Second is null.
	// 2. First is TemplateTimeNode,         Second is null.
	// 3. First is TemplateSpecificDateNode, Second is TemplateTimeNode.
	// 4. First is TemplateTimeNode,         Second is TemplateSpecificDateNode.
	// 5. First is TemplateRelativeDateNode, Second is TemplateTimeNode.
	// 6. First is TemplateTimeNode,         Second is TemplateRelativeDateNode.
	public TemplateNode First { get; }
	public TemplateNode? Second { get; }

	internal override void Traverse(DateTimeTemplateInfo state)
	{
		First.Traverse(state);
		Second?.Traverse(state);
	}
}

// <date> ::= <year> | <month> | <day> | <month-year> | <relative-date> | <specific-date>
internal abstract class TemplateDateNode : TemplateNode
{
}

// <relative-date> ::= <dayofweek> | <month-day> | <relative-longdate>
internal abstract class TemplateRelativeDateNode : TemplateDateNode
{
}

// <specific-date> ::= <shortdate> | <longdate>
internal abstract class TemplateSpecificDateNode : TemplateDateNode
{

}

// <month-day> ::= <month> <whitespace> <day> |
//                 <day> <whitespace> <month>
internal sealed class TemplateMonthDayNode : TemplateRelativeDateNode
{
	public TemplateMonthDayNode(TemplateDateNode left, TemplateDateNode right)
	{
		Left = left;
		Right = right;
	}

	public TemplateDateNode Left { get; }
	public TemplateDateNode Right { get; }

	internal override void Traverse(DateTimeTemplateInfo state)
	{
		Left.Traverse(state);
		Right.Traverse(state);
	}
}

// <relative-longdate> ::= <month> <whitespace> <day> <whitespace> <dayofweek> |
//                         <month> <whitespace> <dayofweek> <whitespace> <day> |
//                         <day> <whitespace> <month> <whitespace> <dayofweek> |
//                         <day> <whitespace> <dayofweek> <whitespace> <month> |
//                         <dayofweek> <whitespace> <day> <whitespace> <month> |
//                         <dayofweek> <whitespace> <month> <whitespace> <day>
internal sealed class TemplateRelativeLongDateNode : TemplateRelativeDateNode
{
	public TemplateRelativeLongDateNode(TemplateDateNode left, TemplateDateNode middle, TemplateDateNode right)
	{
		Left = left;
		Middle = middle;
		Right = right;
	}

	public TemplateDateNode Left { get; }
	public TemplateDateNode Middle { get; }
	public TemplateDateNode Right { get; }

	internal override void Traverse(DateTimeTemplateInfo state)
	{
		Left.Traverse(state);
		Middle.Traverse(state);
		Right.Traverse(state);
	}
}

// <month-year> ::= <month> <whitespace> <year> |
//                  <year> <whitespace> <month>
internal sealed class TemplateMonthYearNode : TemplateDateNode
{
	public TemplateMonthYearNode(TemplateNode left, TemplateNode right)
	{
		Left = left;
		Right = right;
	}

	public TemplateNode Left { get; }
	public TemplateNode Right { get; }

	internal override void Traverse(DateTimeTemplateInfo state)
	{
		Left.Traverse(state);
		Right.Traverse(state);
	}
}

// <shortdate> ::= "shortdate" |
//                 <month> <whitespace> <day> <whitespace> <year> |
//                 <month> <whitespace> <year> <whitespace> <day> |
//                 <day> <whitespace> <month> <whitespace> <year> |
//                 <day> <whitespace> <year> <whitespace> <month> |
//                 <year> <whitespace> <day> <whitespace> <month> |
//                 <year> <whitespace> <month> <whitespace> <day>
internal sealed class TemplateShortDateNode : TemplateSpecificDateNode
{
	public static TemplateShortDateNode DefaultShortDateInstance { get; } = new(null, null, null);

	public TemplateShortDateNode(TemplateDateNode? left, TemplateDateNode? middle, TemplateDateNode? right)
	{
		Left = left;
		Middle = middle;
		Right = right;
	}

	public TemplateDateNode? Left { get; }
	public TemplateDateNode? Middle { get; }
	public TemplateDateNode? Right { get; }

	internal override void Traverse(DateTimeTemplateInfo state)
	{
		if (ReferenceEquals(this, DefaultShortDateInstance))
		{
			state.IsShortDate = true;
		}
		else
		{
			Left!.Traverse(state);
			Middle!.Traverse(state);
			Right!.Traverse(state);
		}
	}
}

// <longdate> ::= "longdate" |
//                <year> <whitespace> <month> <whitespace> <day> <whitespace> <dayofweek> |
//                <year> <whitespace> <month> <whitespace> <dayofweek> <whitespace> <day> |
//                <year> <whitespace> <day> <whitespace> <month> <whitespace> <dayofweek> |
//                <year> <whitespace> <day> <whitespace> <dayofweek> <whitespace> <month> |
//                <year> <whitespace> <dayofweek> <whitespace> <day> <whitespace> <month> |
//                <year> <whitespace> <dayofweek> <whitespace> <month> <whitespace> <day> |
//                <month> <whitespace> <year> <whitespace> <day> <whitespace> <dayofweek> |
//                <month> <whitespace> <year> <whitespace> <dayofweek> <whitespace> <day> |
//                <day> <whitespace> <year> <whitespace> <month> <whitespace> <dayofweek> |
//                <day> <whitespace> <year> <whitespace> <dayofweek> <whitespace> <month> |
//                <dayofweek> <whitespace> <year> <whitespace> <day> <whitespace> <month> |
//                <dayofweek> <whitespace> <year> <whitespace> <month> <whitespace> <day> |
//                <month> <whitespace> <day> <whitespace> <year> <whitespace> <dayofweek> |
//                <month> <whitespace> <dayofweek> <whitespace> <year> <whitespace> <day> |
//                <day> <whitespace> <month> <whitespace> <year> <whitespace> <dayofweek> |
//                <day> <whitespace> <dayofweek> <whitespace> <year> <whitespace> <month> |
//                <dayofweek> <whitespace> <day> <whitespace> <year> <whitespace> <month> |
//                <dayofweek> <whitespace> <month> <whitespace> <year> <whitespace> <day> |
//                <month> <whitespace> <day> <whitespace> <dayofweek> <whitespace> <year> |
//                <month> <whitespace> <dayofweek> <whitespace> <day> <whitespace> <year> |
//                <day> <whitespace> <month> <whitespace> <dayofweek> <whitespace> <year> |
//                <day> <whitespace> <dayofweek> <whitespace> <month> <whitespace> <year> |
//                <dayofweek> <whitespace> <day> <whitespace> <month> <whitespace> <year> |
//                <dayofweek> <whitespace> <month> <whitespace> <day> <whitespace> <year>
internal sealed class TemplateLongDateNode : TemplateSpecificDateNode
{
	public static TemplateLongDateNode DefaultLongDateInstance { get; } = new(null, null, null, null);

	public TemplateLongDateNode(TemplateDateNode? first, TemplateDateNode? second, TemplateDateNode? third, TemplateDateNode? forth)
	{
		First = first;
		Second = second;
		Third = third;
		Forth = forth;
	}

	public TemplateDateNode? First { get; }
	public TemplateDateNode? Second { get; }
	public TemplateDateNode? Third { get; }
	public TemplateDateNode? Forth { get; }

	internal override void Traverse(DateTimeTemplateInfo state)
	{
		if (ReferenceEquals(this, DefaultLongDateInstance))
		{
			state.IsLongDate = true;
		}
		else
		{
			First!.Traverse(state);
			Second!.Traverse(state);
			Third!.Traverse(state);
			Forth!.Traverse(state);
		}
	}
}

// <time> ::= <hour> | 
//            <hour> <whitespace> <timezone> |
//            <timezone> <whitespace> <hour> |
//            <shorttime> |
//            <longtime>
internal sealed class TemplateTimeNode : TemplateNode
{
	public TemplateTimeNode(TemplateNode first, TemplateNode? second = null)
	{
		First = first;
		Second = second;
	}

	public TemplateNode First { get; }

	public TemplateNode? Second { get; }

	internal override void Traverse(DateTimeTemplateInfo state)
	{
		First.Traverse(state);
		Second?.Traverse(state);
	}
}

// <shorttime> ::= "shorttime" |
//                 <hour> <whitespace> <minute> |
//                 <minute> <whitespace> <hour> |
//                 <timezone> <whitespace> <hour> <whitespace> <minute> |
//                 <timezone> <whitespace> <minute> <whitespace> <hour> |
//                 <hour> <whitespace> <timezone> <whitespace> <minute> |
//                 <minute> <whitespace> <timezone> <whitespace> <hour> |
//                 <hour> <whitespace> <minute> <whitespace> <timezone> |
//                 <minute> <whitespace> <hour> <whitespace> <timezone>
internal sealed class TemplateShortTimeNode : TemplateNode
{
	public static TemplateShortTimeNode DefaultShortTimeInstance { get; } = new(null, null, null);

	public TemplateShortTimeNode(TemplateNode? first, TemplateNode? second, TemplateNode? third = null)
	{
		First = first;
		Second = second;
		Third = third;
	}

	public TemplateNode? First { get; }
	public TemplateNode? Second { get; }
	public TemplateNode? Third { get; }

	internal override void Traverse(DateTimeTemplateInfo state)
	{
		if (ReferenceEquals(this, DefaultShortTimeInstance))
		{
			state.IsShortTime = true;
		}
		else
		{
			First!.Traverse(state);
			Second!.Traverse(state);
			Third?.Traverse(state);
		}
	}
}

// <longtime> ::= "longtime" |
//                <hour> <whitespace> <minute> <whitespace> <second> |
//                <hour> <whitespace> <second> <whitespace> <minute> |
//                <minute> <whitespace> <hour> <whitespace> <second> |
//                <minute> <whitespace> <second> <whitespace> <hour> |
//                <second> <whitespace> <minute> <whitespace> <hour> |
//                <second> <whitespace> <hour> <whitespace> <minute> |
//                <timezone> <whitespace> <hour> <whitespace> <minute> <whitespace> <second> |
//                <timezone> <whitespace> <hour> <whitespace> <second> <whitespace> <minute> |
//                <timezone> <whitespace> <minute> <whitespace> <hour> <whitespace> <second> |
//                <timezone> <whitespace> <minute> <whitespace> <second> <whitespace> <hour> |
//                <timezone> <whitespace> <second> <whitespace> <minute> <whitespace> <hour> |
//                <timezone> <whitespace> <second> <whitespace> <hour> <whitespace> <minute> |
//                <hour> <whitespace> <timezone> <whitespace> <minute> <whitespace> <second> |
//                <hour> <whitespace> <timezone> <whitespace> <second> <whitespace> <minute> |
//                <minute> <whitespace> <timezone> <whitespace> <hour> <whitespace> <second> |
//                <minute> <whitespace> <timezone> <whitespace> <second> <whitespace> <hour> |
//                <second> <whitespace> <timezone> <whitespace> <minute> <whitespace> <hour> |
//                <second> <whitespace> <timezone> <whitespace> <hour> <whitespace> <minute> |
//                <hour> <whitespace> <minute> <whitespace> <timezone> <whitespace> <second> |
//                <hour> <whitespace> <second> <whitespace> <timezone> <whitespace> <minute> |
//                <minute> <whitespace> <hour> <whitespace> <timezone> <whitespace> <second> |
//                <minute> <whitespace> <second> <whitespace> <timezone> <whitespace> <hour> |
//                <second> <whitespace> <minute> <whitespace> <timezone> <whitespace> <hour> |
//                <second> <whitespace> <hour> <whitespace> <timezone> <whitespace> <minute> |
//                <hour> <whitespace> <minute> <whitespace> <second> <whitespace> <timezone> |
//                <hour> <whitespace> <second> <whitespace> <minute> <whitespace> <timezone> |
//                <minute> <whitespace> <hour> <whitespace> <second> <whitespace> <timezone> |
//                <minute> <whitespace> <second> <whitespace> <hour> <whitespace> <timezone> |
//                <second> <whitespace> <minute> <whitespace> <hour> <whitespace> <timezone> |
//                <second> <whitespace> <hour> <whitespace> <minute> <whitespace> <timezone>
internal sealed class TemplateLongTimeNode : TemplateNode
{
	public static TemplateLongTimeNode DefaultLongTimeInstance { get; } = new(null, null, null, null);

	public TemplateLongTimeNode(TemplateNode? first, TemplateNode? second, TemplateNode? third, TemplateNode? forth = null)
	{
		First = first;
		Second = second;
		Third = third;
		Forth = forth;
	}

	public TemplateNode? First { get; }
	public TemplateNode? Second { get; }
	public TemplateNode? Third { get; }
	public TemplateNode? Forth { get; }

	internal override void Traverse(DateTimeTemplateInfo state)
	{
		if (ReferenceEquals(this, DefaultLongTimeInstance))
		{
			state.IsLongTime = true;
		}
		else
		{
			First!.Traverse(state);
			Second!.Traverse(state);
			Third!.Traverse(state);
			Forth?.Traverse(state);
		}
	}
}


// <year> ::= "year" | "year.full" | "year.abbreviated"
internal sealed class TemplateYearNode : TemplateDateNode
{
	public static TemplateYearNode NormalInstance { get; } = new(YearKind.Normal);
	public static TemplateYearNode FullInstance { get; } = new(YearKind.Full);
	public static TemplateYearNode AbbreviatedInstance { get; } = new(YearKind.Abbreviated);

	internal enum YearKind
	{
		Normal,
		Full,
		Abbreviated,
	}

	private TemplateYearNode(YearKind kind)
	{
		Kind = kind;
	}

	public YearKind Kind { get; }

	internal override void Traverse(DateTimeTemplateInfo state)
	{
		state.IncludeYear = Kind switch
		{
			YearKind.Normal => YearFormat.Default,
			YearKind.Full => YearFormat.Full,
			YearKind.Abbreviated => YearFormat.Abbreviated,
			_ => throw new InvalidOperationException(),
		};
	}
}

// <month> ::= "month" | "month.full" | "month.abbreviated" | "month.numeric"
internal sealed class TemplateMonthNode : TemplateDateNode
{
	public static TemplateMonthNode NormalInstance { get; } = new(MonthKind.Normal);
	public static TemplateMonthNode FullInstance { get; } = new(MonthKind.Full);
	public static TemplateMonthNode AbbreviatedInstance { get; } = new(MonthKind.Abbreviated);
	public static TemplateMonthNode NumericInstance { get; } = new(MonthKind.Numeric);

	internal enum MonthKind
	{
		Normal,
		Full,
		Abbreviated,
		Numeric,
	}

	private TemplateMonthNode(MonthKind kind)
	{
		Kind = kind;
	}

	public MonthKind Kind { get; }

	internal override void Traverse(DateTimeTemplateInfo state)
	{
		state.IncludeMonth = Kind switch
		{
			MonthKind.Normal => MonthFormat.Default,
			MonthKind.Full => MonthFormat.Full,
			MonthKind.Abbreviated => MonthFormat.Abbreviated,
			MonthKind.Numeric => MonthFormat.Numeric,
			_ => throw new InvalidOperationException(),
		};
	}
}

// <day> ::= "day"
internal sealed class TemplateDayNode : TemplateDateNode
{
	private TemplateDayNode()
	{
	}

	public static TemplateDayNode Instance { get; } = new();

	internal override void Traverse(DateTimeTemplateInfo state)
	{
		state.IncludeDay = DayFormat.Default;
	}
}

// <dayofweek> ::= "dayofweek" | "dayofweek.full" | "dayofweek.abbreviated"
internal sealed class TemplateDayOfWeekNode : TemplateRelativeDateNode
{
	public static TemplateDayOfWeekNode NormalInstance { get; } = new(DayOfWeekKind.Normal);
	public static TemplateDayOfWeekNode FullInstance { get; } = new(DayOfWeekKind.Full);
	public static TemplateDayOfWeekNode AbbreviatedInstance { get; } = new(DayOfWeekKind.Abbreviated);

	internal enum DayOfWeekKind
	{
		Normal,
		Full,
		Abbreviated,
	}

	private TemplateDayOfWeekNode(DayOfWeekKind kind)
	{
		Kind = kind;
	}

	public DayOfWeekKind Kind { get; }

	internal override void Traverse(DateTimeTemplateInfo state)
	{
		state.IncludeDayOfWeek = Kind switch
		{
			DayOfWeekKind.Normal => DayOfWeekFormat.Default,
			DayOfWeekKind.Full => DayOfWeekFormat.Full,
			DayOfWeekKind.Abbreviated => DayOfWeekFormat.Abbreviated,
			_ => throw new InvalidOperationException(),
		};
	}
}

// <hour> ::= "hour"
internal sealed class TemplateHourNode : TemplateNode
{
	private TemplateHourNode()
	{
	}

	public static TemplateHourNode Instance { get; } = new();

	internal override void Traverse(DateTimeTemplateInfo state)
	{
		state.IncludeHour = HourFormat.Default;
	}
}

// <minute> ::= "minute"
internal sealed class TemplateMinuteNode : TemplateNode
{
	private TemplateMinuteNode()
	{
	}

	public static TemplateMinuteNode Instance { get; } = new();

	internal override void Traverse(DateTimeTemplateInfo state)
	{
		state.IncludeMinute = MinuteFormat.Default;
	}
}

// <second> ::= "second"
internal sealed class TemplateSecondNode : TemplateNode
{
	private TemplateSecondNode()
	{
	}

	public static TemplateSecondNode Instance { get; } = new();

	internal override void Traverse(DateTimeTemplateInfo state)
	{
		state.IncludeSecond = SecondFormat.Default;
	}
}

// <timezone> ::= "timezone" | "timezone.full" | "timezone.abbreviated"
internal sealed class TemplateTimeZoneNode : TemplateNode
{
	public static TemplateTimeZoneNode NormalInstance { get; } = new(TimeZoneKind.Normal);
	public static TemplateTimeZoneNode FullInstance { get; } = new(TimeZoneKind.Full);
	public static TemplateTimeZoneNode AbbreviatedInstance { get; } = new(TimeZoneKind.Abbreviated);

	internal enum TimeZoneKind
	{
		Normal,
		Full,
		Abbreviated,
	}

	private TemplateTimeZoneNode(TimeZoneKind kind)
	{
		Kind = kind;
	}

	public TimeZoneKind Kind { get; }

	internal override void Traverse(DateTimeTemplateInfo state)
	{
		state.IncludeTimeZone = Kind switch
		{
			TimeZoneKind.Normal => TimeZoneFormat.Default,
			TimeZoneKind.Full => TimeZoneFormat.Full,
			TimeZoneKind.Abbreviated => TimeZoneFormat.Abbreviated,
			_ => throw new InvalidOperationException(),
		};
	}
}
