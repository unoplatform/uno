<Query Kind="Program">
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <NuGetReference>System.Reactive</NuGetReference>
  <Namespace>System.Diagnostics</Namespace>
  <Namespace>System.Drawing</Namespace>
  <Namespace>System.Drawing.Drawing2D</Namespace>
  <Namespace>System.Drawing.Imaging</Namespace>
  <Namespace>System.Globalization</Namespace>
  <Namespace>System.Reactive.Disposables</Namespace>
  <Namespace>System.Reactive.Linq</Namespace>
  <Namespace>System.Runtime.InteropServices</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>System.Windows.Forms</Namespace>
  <Namespace>Newtonsoft.Json</Namespace>
</Query>

#define CLEAR_OLD_LOG  // run `adb logcat -c` before starting to parse log
#define ENABLE_RAW_INPUT_FILTER // filter out noisy system logs
#define REMOVE_DUPLICATED_NEWLINES
//#define DISABLE_SCREENSHOT // on slower device, you may want to uncomment this

void Main()
{
	// This script uses adb to obtain the layout data logged by LayoutProvider.
	// Please ensure that `adb` is installed on this machine and is added to envinronment path.
	// Also, you will need to config the app to allow LayoutProvider write log message. 
	// To do so, add the following code to `ApplicationActivity.OnCreate`:
	//
	//		LogExtensionPoint.AmbientLoggerFactory
	//			.WithFilter(new FilterLoggerSettings
	//			{
	//				{  "Uno.UI.LayoutProvider", LogLevel.Debug },
	//			})
	//			.AddConsole(LogLevel.Debug)
	// 
	// To test everything is properly configured, you can type the following in commandline:
	//		adb logcat -e "Android layout has been updated: " mono-stdout
	// And see if there is any message show up when you switch away from/back to the app, 
	// or force keyboard/nav-bar to show/hide.
	
	// Adjust this value to fit everything on a single row.
	// You can also close the explorer panel or undock the results (F8).
	const double DisplayScaling = 1.0/6.00;

	Util.AutoScrollResults = false;
	var container = new DumpContainer(Util.Metatext("Waiting on adb input")).Dump();
	var disposables = new CompositeDisposable();
	
	Util.KeepRunning().DisposeWith(disposables);
	ObserveLayoutChangedLogs()
		.Subscribe(log =>
		{
			try
			{
				var json = log
					.RegexReplace(@"^\s+Android layout has been updated: ", "")
					.RegexReplace(@" //.+$", "");
				var data = JsonConvert.DeserializeObject<AndroidLayout>(json);
				
				container.Content = Visualize(data, DisplayScaling, "LayoutProvider");
			}
			catch (Exception ex)
			{
				container.Content = Util.VerticalRun(log, ex).Dump(ex.GetType().Name);
			}
		})
		.DisposeWith(disposables);
	
	Util.Cleanup += (s, e) => disposables.Dispose();
}

// Define other methods and classes here
IObservable<string> ObserveLayoutChangedLogs()
{
	var logs = ObserveAndroidMessageDump()
		.Publish().RefCount();

	var formattedLogs = logs
		.AggregateWithOpening(AdbLog.MetadataPattern.IsMatch, g => g.ToArray())
		.Skip(1)
		.Where(x => x.Length != 0)
		.Select(AdbLog.Parse)
		.Where(GetRawInputFilter())
		.Publish().RefCount();

	var tag = "mono-stdout";
	var matcher = new CompositeMatcher<AdbLog>()
		.MatchLog(tag, x => x.Contains("Android layout has been updated: "))
		;

	return formattedLogs
		.Where(matcher.MatchAny)
		.Select(x => x.Message);
}
object Visualize(AndroidLayout layout, double displayScale, string context = null)
{
	var screenSize = (Size)layout.RealMetrics;
	
	return Util.HorizontalRun(true,
#if !DISABLE_SCREENSHOT
		StackView("screencap", Screenshot(), x => x?.Scale(displayScale), x => x?.Apply(y => $"Size: w={y.Width}, h={y.Height}")),
#endif
		VisualizeProperty(x => x.RealMetrics, x => HighlightArea(screenSize, new Rectangle(Point.Empty, x)).Scale(displayScale)),
		string.Empty,
		
		VisualizeProperty(x => x.AdjustNothingFrame, x => HighlightArea(screenSize, x).Scale(displayScale)),
		VisualizeProperty(x => x.AdjustResizeFrame, x => HighlightArea(screenSize, x).Scale(displayScale)),
		string.Empty,
		
		VisualizeProperty(x => x.StatusBarRect, x => HighlightArea(screenSize, x).Scale(displayScale)),
		VisualizeProperty(x => x.NavigationBarRect, x => HighlightArea(screenSize, x).Scale(displayScale)),
		VisualizeProperty(x => x.KeyboardRect, x => HighlightArea(screenSize, x).Scale(displayScale)),
		
		Util.VerticalRun(
			context, "=======",
			PrintProperty(x => x.IsStatusBarVisible),
			// PrintProperty(x => x.Flags),
			PrintProperty(x => x.HasTranslucentStatus),
			PrintProperty(x => x.HasTranslucentNavigation),
			PrintProperty(x => x.HasLayoutNoLimits),
			string.Empty,
			
			PrintProperty(x => x.Flags, x => x?.Apply(y => "\n" + string.Join("\n", 
				y.Split(new[] { ", " }, StringSplitOptions.None)
					.Select(z => $"-{z}")
				)
			)),
			string.Empty
		)
	);
	
	(string Name, T Value) GetPropertyNameAndValue<T>(Expression<Func<AndroidLayout, T>> propertySelector)
	{
		var member = propertySelector.Body as MemberExpression;
		var property = member.Member as PropertyInfo;
		var value = propertySelector.Compile().Invoke(layout);
		
		return (property.Name, value);
	}
	string PrintProperty<T>(Expression<Func<AndroidLayout, T>> propertySelector, Func<T, string> formatter = null)
	{
		var property = GetPropertyNameAndValue(propertySelector);
		formatter = formatter ?? (x => x?.ToString());

		return $"{property.Name}: {formatter(property.Value)}";
	}
	object VisualizeProperty<T>(Expression<Func<AndroidLayout, T>> propertySelector, Func<T, Image> visualize, Func<T, string> describe = null)
	{
		var property = GetPropertyNameAndValue(propertySelector);
		describe = describe ?? (x => x.ToString());
		
		return Util.VerticalRun(
			property.Name,
			property.Value?.Apply(visualize),
			property.Value?.Apply(describe)
		);
	}
	
	object StackView<T>(string header, T value, Func<T, Image> visualize, Func<T, string> describe = null)
	{
		describe = describe ?? (x => x.ToString());
		
		return Util.VerticalRun(
			header,
			value?.Apply(visualize),
			value?.Apply(describe)
		);
	}
}
Image Screenshot()
{
	var process = new Process();
	process.StartInfo.FileName = "adb";
	process.StartInfo.Arguments = "exec-out screencap -p";
	process.StartInfo.UseShellExecute = false;
	process.StartInfo.RedirectStandardOutput = true;
	process.StartInfo.CreateNoWindow = true;

	process.Start();
	using (var stream = new MemoryStream())
	{
		try
		{	        
			process.StandardOutput.BaseStream.CopyTo(stream);
			process.WaitForExit();
	
			return System.Drawing.Image.FromStream(stream);
		}
		catch (Exception e)
		{
			return null;
		}
	}
}

// dto
public class AndroidLayout
{
	public Metrics RealMetrics { get; set; }
	public Rect AdjustNothingFrame { get; set; }
	public Rect AdjustResizeFrame { get; set; }
	
	public Rect StatusBarRect { get; set; }
	public Rect KeyboardRect { get; set; }
	public Rect NavigationBarRect { get; set; }
	
	public bool? IsStatusBarVisible { get; set; }
	public string Flags { get; set; }
	public bool? HasTranslucentStatus { get; set; }
	public bool? HasTranslucentNavigation { get; set; }
	public bool? HasLayoutNoLimits { get; set; }
}
public class Metrics
{
	public int Width { get; set; }
	public int Height { get; set; }

	public static implicit operator Size(Metrics x) => new Size(x.Width, x.Height);
	public override string ToString() => $"Size: w={Width}, h={Height}";
}
public class Rect
{
	public int Left { get; set; }
	public int Top { get; set; }
	public int Right { get; set; }
	public int Bottom { get; set; }
	
	public static implicit operator Rectangle(Rect x) => new Rectangle(x.Left, x.Top, x.Right - x.Left, x.Bottom - x.Top);
	public override string ToString() => "LTRB: " + string.Join(", ", Left, Top, Right, Bottom);
}
public class Coord
{
	public int X { get; set; }
	public int Y { get; set; }

	public static implicit operator Point(Coord x) => new Point(x.X, x.Y);
	public override string ToString() => $"Point: x={X}, y={Y}";
}

#region log parser  from:adb logger.linq

IObservable<string> ObserveAndroidMessageDump()
{
	return Observable.Create<string>(observer =>
	{
#if CLEAR_OLD_LOG
		var process1 = new Process();
		process1.StartInfo.FileName = "adb";
		process1.StartInfo.Arguments = "logcat -c";
		process1.StartInfo.UseShellExecute = false;
		process1.StartInfo.RedirectStandardOutput = true;
		process1.StartInfo.CreateNoWindow = true;
		
		process1.Start();
		process1.WaitForExit();
#endif
		
		var process = new Process();
		process.StartInfo.FileName = "adb";
		process.StartInfo.Arguments = "logcat -v long";
		process.StartInfo.UseShellExecute = false;
		process.StartInfo.CreateNoWindow = true;
		process.StartInfo.RedirectStandardOutput = true;
		process.StartInfo.RedirectStandardError = true;

		process.OutputDataReceived += (s, e) =>
		{
			if (e.Data == null)
			{
				observer.OnCompleted();
				return;
			}

			observer.OnNext(e.Data);
		};
		process.ErrorDataReceived += (s, e) =>
		{
			if (e.Data != null)
			{
				e.Data.Dump();
			}
		};
		process.Start();
		process.BeginErrorReadLine();
		process.BeginOutputReadLine();

		return () =>
		{
			if (!process.HasExited)
				process.Kill();
			
			process.Dispose();
		};
	});
}
Func<AdbLog, bool> GetRawInputFilter()
{
	return new CompositeMatcher<AdbLog>()
#if ENABLE_RAW_INPUT_FILTER
		// sys debug
		.MatchTag(x => IsAnyOf(x, "art", "Finsky", "sh", "TCMD"))
		.MatchTag(x => StringContains(x, "Engine", "Service", "Manager", "Mgr", "crypt", "motion"))
		// huawai honor 10
		.MatchTag(x => StringContains(x, "NetworkSpeed", "QoS"))
		.MatchTag(x => IsAnyOf(x, "TrafficMonitor", "WeatherHelper", "QosMonitor", "NetworkUtils", "HWComposer", "hwcomposer", "hw_netstat", "HwWifiStatStore"))
		// LG
		.MatchTag(x => IsAnyOf(x, "sensors_hal_motion_accel"))

#endif
		.MatchNone;
		
		bool IsAnyOf(string x, params string[] values) => x != null && values.Any(x.Equals);
		bool StringStartsWith(string x, params string[] values) => x != null && values.Any(x.StartsWith);
		bool StringContains(string x, params string[] values) => x != null && values.Any(x.StartsWith);
}

// from lowest (V) to highest (S)
public enum LogPriority { Verbose = 1, Debug = 2, Info = 3, Warning = 4, Error = 5, Fatal = 6, Silent = 999 }
public class AdbLog
{
	// "-v long" format
	public static readonly Regex MetadataPattern = new Regex(@"^\[ (?<timestamp>.{18}) +(?<pid>\d+): *(?<tid>\d+) (?<level>\w)/(?<tag>.+?) +\]$", RegexOptions.ExplicitCapture);
	private static readonly Dictionary<string, LogPriority> PriorityParser = typeof(LogPriority).GetEnumValues()
		.Cast<LogPriority>()
		.ToDictionary(x => x.ToString().Substring(0, 1), x => x);

	public int Index { get; set; }
	public string Timestamp { get; set; }
	public string PID { get; set; }
	public string TID { get; set; }
	public LogPriority Priority { get; set; }
	public string Tag { get; set; }
	public string Message { get; set; }

	public static AdbLog Parse(string[] raw, int index)
	{
		try
		{
			var match = MetadataPattern.Match(raw[0]);
			var log = new AdbLog
			{
				Index = index,
				Timestamp = match.Groups["timestamp"].Value,
				PID = match.Groups["pid"].Value,
				TID = match.Groups["tid"].Value,
				Priority = PriorityParser[match.Groups["level"].Value],
				Tag = match.Groups["tag"].Value,
				Message = string.Join("\n", raw.Skip(1))
					#if REMOVE_DUPLICATED_NEWLINES
					.RegexReplace("\n+", "\n")
					#endif
					.TrimEnd().TrimStart('\n'),
			};
			
			return log;
		}
		catch (Exception ex)
		{
			ex.Data["Raw"] = raw[0];
			throw;
		}
	}
}
public class UnoLog
{
	public static Regex LogPattern = new Regex(@"^(?<type>.+?): (?<level>\w+?): ");

	public string TypeName { get; set; }
	public string ClassName => TypeName.Split('.').Last();
	public string Level { get; set; }
	public string Message { get; set; }

	public static UnoLog Parse(string message)
	{
		try
		{
			var match = LogPattern.Match(message);
			if (!match.Success)
				return null;

			return new UnoLog
			{
				TypeName = match.Groups["type"].Value,
				Level = match.Groups["level"].Value,
				Message = message.Substring(match.Length),
			};
		}
		catch (Exception ex)
		{
			ex.Data["message"] = message;
			throw;
		}
	}
}

public class CompositeMatcher<T>
{
	private List<Func<T, bool>> predicates = new List<Func<T, bool>>();

	public Func<T, bool> MatchNone => x => predicates.All(rule => !rule(x));
	public Func<T, bool> MatchAny => x => predicates.Any(rule => rule(x));

	public CompositeMatcher<T> Add(Func<T, bool> predicate)
	{
		predicates.Add(predicate);
		return this;
	}
}
public static class CompositeMatcherExtensions
{
	public static CompositeMatcher<AdbLog> MatchTag(this CompositeMatcher<AdbLog> matcher, Func<string, bool> tagMatcher) => matcher.Add(x => tagMatcher(x.Tag));
	
	public static CompositeMatcher<AdbLog> MatchLog(this CompositeMatcher<AdbLog> matcher, string tag) => matcher.Add(x => x.Tag == tag);
	public static CompositeMatcher<AdbLog> MatchLog(this CompositeMatcher<AdbLog> matcher, string tag, Func<string, bool> messageMatcher) => matcher.Add(x => x.Tag == tag && messageMatcher(x.Message));
	public static CompositeMatcher<AdbLog> MatchLog(this CompositeMatcher<AdbLog> matcher, Func<string, bool> messageMatcher) => matcher.Add(x => messageMatcher(x.Message));
	
	public static CompositeMatcher<AdbLog> FromClass(this CompositeMatcher<AdbLog> matcher, string tag, string className) => matcher.MatchLog(tag, x => x.Contains($".{className}:"));
	public static CompositeMatcher<AdbLog> FromClass(this CompositeMatcher<AdbLog> matcher, string tag, string className, Func<string, bool> parentMessageMatcher) => matcher.MatchLog(tag, x => x.Contains($".{className}:") && parentMessageMatcher(x));
}

public static class ObservableExtensions
{
	public static IObservable<TResult> AggregateWithOpening<TSource, TResult>(this IObservable<TSource> source, Func<TSource, bool> openingSelector, Func<IEnumerable<TSource>, TResult> resultSelector)
	{
		return Observable.Create<TResult>(o =>
		{
			var bucket = new List<TSource>();

			return source.Subscribe(x =>
			{
				if (openingSelector(x))
				{
					o.OnNext(resultSelector(bucket));
					bucket.Clear();
				}

				bucket.Add(x);
			},
			o.OnError,
			() =>
			{
				o.OnNext(resultSelector(bucket));
				o.OnCompleted();
			});
		});
	}
}
public static class StringExtensions
{
	public static string Truncate(this string value, int length, string ellipsis = null)
	{
		if (value == null || value.Length <= length)
			return value;

		if (ellipsis == null || length <= ellipsis.Length)
			return value.Substring(0, length);

		return value.Substring(0, length - ellipsis.Length) + ellipsis;
	}
	public static string RegexReplace(this string value, string pattern, string replacement)
	{
		return value == null ? value : Regex.Replace(value, pattern, replacement);
	}
}
#endregion Adb+Uno Parser
#region rect visualizer  from: rect analyzer.linq
Rectangle FromLTRB(int left, int top, int right, int bottom) => new Rectangle(left, top, right - left, bottom - top);
Image HighlightArea(Size screen, Rectangle rectangle)
{
	var image = new Bitmap(screen.Width, screen.Height);
	using (var g = Graphics.FromImage(image))
	{
		g.Clear(Color.Pink);
		g.FillRectangle(Brushes.Orange, rectangle);
	}

	return image;
}

public static class ImageExtensions
{
	public static Bitmap Scale(this Image image, double factor) => image.Scale((int)(image.Width * factor), (int)(image.Height * factor));
	public static Bitmap Scale(this Image image, int width, int height)
	{
		var destRect = new Rectangle(0, 0, width, height);
		var destImage = new Bitmap(width, height);

		destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

		using (var graphics = Graphics.FromImage(destImage))
		{
			graphics.CompositingMode = CompositingMode.SourceCopy;
			graphics.CompositingQuality = CompositingQuality.HighQuality;
			graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
			graphics.SmoothingMode = SmoothingMode.HighQuality;
			graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

			using (var wrapMode = new ImageAttributes())
			{
				wrapMode.SetWrapMode(WrapMode.TileFlipXY);
				graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
			}
		}

		return destImage;
	}
}
#endregion

public static class SyntaxExtensions
{
	public static T Apply<T>(this T target, Action<T> action) { action(target); return target; }
	public static TResult Apply<T, TResult>(this T target, Func<T, TResult> selector) => selector(target);
}
public static class DisposableExtensions
{
	public static TDisposable DisposeWith<TDisposable>(this TDisposable disposable, CompositeDisposable disposables) where TDisposable : IDisposable
	{
		disposables.Add(disposable);
		
		return disposable;
	}
}