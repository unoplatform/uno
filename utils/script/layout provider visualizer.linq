<Query Kind="Program">
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <NuGetReference>System.Reactive</NuGetReference>
  <Namespace>Newtonsoft.Json</Namespace>
  <Namespace>System.Drawing</Namespace>
  <Namespace>System.Drawing.Drawing2D</Namespace>
  <Namespace>System.Drawing.Imaging</Namespace>
  <Namespace>System.Reactive.Disposables</Namespace>
  <Namespace>System.Reactive.Linq</Namespace>
</Query>

#define REMOVE_DUPLICATED_NEWLINES // remove duplicated newlines from log entries

public static class Config
{
	// Please ensure that `adb`, android debug bridge, is installed on this machine
	// If `adb` is not added to your environment path, you can change this value to its full path
	public static readonly string AdbPath = "adb.exe";

	// Adjust this value to change the final display scaling.
	// You can also close the left explorer panel or undock the results (F8).
	public static double DisplayScaling = 1.0 / 6.0;

	// Set this value to true to use the device screencap as the base canvas, otherwise a blank image is used instead
	public static bool UseScreenshotAsCanvas = true;
}
void Main()
{
	var container = new DumpContainer(Util.Metatext("Waiting on adb input")).Dump();
	var disposables = new CompositeDisposable();

	var layout = new AndroidLayout();

	Util.KeepRunning().DisposeWith(disposables);
	ObserveRawAdbLog(argument: "logcat -e \"=== Measure(Insets|Layout): {\" mono-stdout:D *:S")
		.SkipWhile(x => !x.Contains("mono-stdout"))
		.Subscribe(log =>
		{
			try
			{
				var json = Regex.Replace(log, "^.+?(?={)", string.Empty);
				JsonConvert.PopulateObject(json, layout);

				container.Content = Visualize(layout);
				container.Refresh();
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
IObservable<string> ObserveRawAdbLog(string argument = "logcat -v long", bool clearOldLog = true)
{
	return Observable.Create<string>(observer =>
	{
		if (clearOldLog) ClearOldLog();

		var process = new Process();

		process.StartInfo.FileName = Config.AdbPath;
		process.StartInfo.Arguments = argument;
		process.StartInfo.UseShellExecute = false;
		process.StartInfo.CreateNoWindow = true;
		process.StartInfo.RedirectStandardOutput = true;
		process.StartInfo.RedirectStandardError = true;


		process.OutputDataReceived += OutputDataReceived;
		process.ErrorDataReceived += OutputDataReceived;

		process.Start();
		process.BeginErrorReadLine();
		process.BeginOutputReadLine();

		return () =>
		{
			if (!process.HasExited)
				process.Kill();

			process.Dispose();
		};

		void ClearOldLog()
		{
			Process
				.Start(new ProcessStartInfo(Config.AdbPath, "logcat -c") { UseShellExecute = false, CreateNoWindow = true, })
				.WaitForExit();
		}
		void OutputDataReceived(object sender, DataReceivedEventArgs e)
		{
			if (e.Data == null)
			{
				observer.OnCompleted();
				return;
			}

			observer.OnNext(e.Data);
		};
		void ErrorDataReceived(object sender, DataReceivedEventArgs e)
		{
			if (e.Data != null)
			{
				e.Data.Dump();
			}
		};
	});
}
object Visualize(AndroidLayout layout)
{
	if (layout.RealMetrics == null) return Util.VerticalRun(
		Util.Metatext("missing metrics"), 
		"not enough information to visualize, you can force this to update by changing orientation, show/hide keyboard, restarting app", 
		layout
	);

	var canvas = Config.UseScreenshotAsCanvas 
		? Screenshot() 
		: new Bitmap(layout.RealMetrics.Width, layout.RealMetrics.Height).Clear(Color.WhiteSmoke);

	Func<Point, Image> crosshair = x => canvas.Crosshair(x, 50, Color.Red, 20);
	Func<Rect, Image> overlay = x => canvas.Overlay(x, Color.Black, 0.5);
	Func<Rect, Image> overlayOrange = x => canvas.Overlay(x, Color.Orange, 0.7);
	Func<Rect, Image> overlayPink = x => canvas.Overlay(x, Color.Pink, 0.7);

	return Util.HorizontalRun(true,
		VisualizeProperty(x => x.RealMetrics, x => canvas),
		string.Empty,

		VisualizeProperty(x => x.AdjustNothingFrame, overlayOrange),
		VisualizeProperty(x => x.AdjustResizeFrame, overlayOrange),
		VisualizeProperty(x => x.PhysicalInsets, x => canvas.Overlay(x, Color.Purple, 0.5)),
		string.Empty,

		VisualizeProperty(x => x.StatusBarRect, overlayPink),
		VisualizeProperty(x => x.NavigationBarRect, overlayPink),
		VisualizeProperty(x => x.KeyboardRect, overlayPink),
		
		Util.VerticalRun(
			Util.Metatext("Other properties"),
			string.Empty,
			
			PrintProperty(x => x.Orientation),
			PrintProperty(x => x.Flags, flags => "\n" + string.Concat(flags
				.Split(new[] { ", " }, StringSplitOptions.None)
				.Select(x => $"- {x}\n")
			)),
			PrintProperty(x => x.SystemUiVisibility),
			PrintProperty(x => x.FlagsRaw),
			PrintProperty(x => x.SystemUiVisibilityRaw)
		)
	);


	(string Name, T Value) GetPropertyNameAndValue<T>(Expression<Func<AndroidLayout, T>> propertySelector)
	{

		var member = propertySelector.Body is UnaryExpression convert
			? convert.Operand as MemberExpression
			: propertySelector.Body as MemberExpression;
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

		return StackView(property.Name, property.Value, visualize, describe);
	}

	object StackView<T>(string header, T value, Func<T, Image> visualize, Func<T, string> describe = null)
	{
		describe = describe ?? (x => x.ToString());

		return Util.VerticalRun(
			Util.Metatext(header),
			value?.Apply(visualize).Scale(Config.DisplayScaling),
			value?.Apply(describe)
		);
	}
}
System.Drawing.Image Screenshot()
{
	var process = new Process();
	process.StartInfo.FileName = Config.AdbPath;
	process.StartInfo.Arguments = "exec-out screencap -p";
	process.StartInfo.UseShellExecute = false;
	process.StartInfo.RedirectStandardOutput = true;
	process.StartInfo.CreateNoWindow = true;

	process.Start();
	using (var stream = new MemoryStream())
	{
		process.StandardOutput.BaseStream.CopyTo(stream);
		process.WaitForExit();

		return System.Drawing.Image.FromStream(stream);
	}
}

// dot
public class AndroidLayout
{
	// measured values
	public Metrics RealMetrics { get; set; }
	public Rect AdjustNothingFrame { get; set; }
	public Rect AdjustResizeFrame { get; set; }
	public Insets PhysicalInsets { get; set; }
	// computed values
	public Rect StatusBarRect { get; set; }
	public Rect KeyboardRect { get; set; }
	public Rect NavigationBarRect { get; set; }
	// for debugging
	public string Orientation { get; set; }
	public string Flags { get; set; }
	public string SystemUiVisibility { get; set; }
	public int? FlagsRaw { get; set; }
	public int? SystemUiVisibilityRaw { get; set; }
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
public class Insets
{
	public int Left { get; set; }
	public int Top { get; set; }
	public int Right { get; set; }
	public int Bottom { get; set; }
	
	// don't add conversion to rect here, thickness/inset is measured from the edges of canvas
	public override string ToString() => "LTRB: " + string.Join(", ", Left, Top, Right, Bottom);
}
public class Coord
{
	public int X { get; set; }
	public int Y { get; set; }

	public static implicit operator Point(Coord x) => new Point(x.X, x.Y);
	public override string ToString() => $"Point: x={X}, y={Y}";
}

// extensions
public static class ImageExtensions
{
	public static Bitmap Clear(this Image source, Color color)
	{
		var image = new Bitmap(source);
		using (var g = Graphics.FromImage(image))
		{
			g.Clear(color);
		}

		return image;
	}

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

	public static Image Overlay(this Image source, Rectangle rectangle, Color color, double opacity = 1.0) => source.Overlay(rectangle, new SolidBrush(Color.FromArgb((int)(255 * opacity), color.R, color.G, color.B)));
	public static Image Overlay(this Image source, Rectangle rectangle, Brush brush)
	{
		var image = new Bitmap(source);
		using (var g = Graphics.FromImage(image))
		{
			g.FillRectangle(brush, rectangle);
		}

		return image;
	}

	public static Image Overlay(this Image source, Insets insets, Color color, double opacity = 1.0) => source.Overlay(insets, new SolidBrush(Color.FromArgb((int)(255 * opacity), color.R, color.G, color.B)));
	public static Image Overlay(this Image source, Insets insets, Brush brush)
	{
		var image = new Bitmap(source);
		using (var g = Graphics.FromImage(image))
		{
			g.FillRectangle(brush, Rectangle.FromLTRB(0, 0, insets.Left, source.Height));
			g.FillRectangle(brush, Rectangle.FromLTRB(0, 0, source.Width, insets.Top));
			g.FillRectangle(brush, Rectangle.FromLTRB(source.Width - insets.Right, 0, source.Width, source.Height));
			g.FillRectangle(brush, Rectangle.FromLTRB(0, source.Height - insets.Bottom, source.Width, source.Height));
		}

		return image;
	}

	public static Image Crosshair(this Image source, Point location, int length, Color color, int thickness) => source.Crosshair(location, length, new Pen(color, thickness));
	public static Image Crosshair(this Image source, Point location, int length, Pen pen)
	{
		var image = new Bitmap(source);
		using (var g = Graphics.FromImage(image))
		{
			g.DrawLine(pen, location.X - length, location.Y, location.X + length, location.Y);
			g.DrawLine(pen, location.X, location.Y - length, location.X, location.Y + length);
		}

		return image;
	}
}
public static class FluentSyntaxExtensions
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