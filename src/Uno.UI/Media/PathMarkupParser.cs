//-----------------------------------------------------------------------
// The MIT License(MIT)
//
// Original work (https://github.com/AvaloniaUI/Avalonia/blob/969ddc97e03e56c0b76c31295de16ce784d857c7/src/Avalonia.Visuals/Media/PathMarkupParser.cs):
// Copyright (c) 2014 Steven Kirk
// Copyright (c) The Avalonia Project. All rights reserved.
//
// Modified work:
// Copyright (c) 2018 nventive inc. All rights reserved.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Uno.Media;
using Windows.Foundation;
using Windows.UI.Xaml.Media;

namespace Uno.Media
{
    /// <summary>
    /// Parses a path markup string.
    /// </summary>
    public class PathMarkupParser
    {
        private static readonly Dictionary<char, Command> Commands = new Dictionary<char, Command>
        {
            { 'F', Command.FillRule },
            { 'M', Command.Move },
            { 'L', Command.Line },
            { 'H', Command.HorizontalLine },
            { 'V', Command.VerticalLine },
            { 'Q', Command.QuadraticBezierCurve },
            { 'T', Command.SmoothQuadraticBezierCurve },
            { 'C', Command.CubicBezierCurve },
            { 'S', Command.SmoothCubicBezierCurve },
            { 'A', Command.Arc },
            { 'Z', Command.Close },
        };

        private static readonly Dictionary<char, FillRule> FillRules = new Dictionary<char, FillRule>
        {
            {'0', FillRule.EvenOdd },
            {'1', FillRule.Nonzero }
        };

        private readonly StreamGeometryContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="PathMarkupParser"/> class.
        /// </summary>
        /// <param name="context">The context for the geometry.</param>
        public PathMarkupParser(StreamGeometryContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Defines the command currently being processed.
        /// </summary>
        private enum Command
        {
            None,
            FillRule,
            Move,
            Line,
            HorizontalLine,
            VerticalLine,
            CubicBezierCurve,
            QuadraticBezierCurve,
            SmoothCubicBezierCurve,
            SmoothQuadraticBezierCurve,
            Arc,
            Close,
        }

        /// <summary>
        /// Parses the specified markup string.
        /// </summary>
        /// <param name="s">The markup string.</param>
        public void Parse(string s, ref FillRule fillRule)
        {
            bool openFigure = false;

            using (StringReader reader = new StringReader(s))
            {
                Command command = Command.None;
                Point point = new Point();
                bool relative = false;        
                Point? previousControlPoint = null;

                while (ReadCommand(reader, ref command, ref relative))
                {
                    switch (command)
                    {
                        case Command.FillRule:
							fillRule = ReadFillRule(reader);
                            //_context.SetFillRule(ReadFillRule(reader));
                            previousControlPoint = null;
                            break;

                        case Command.Move:
                            if (openFigure)
                            {
                                _context.SetClosedState(false);
                            }

                            point = ReadPoint(reader, point, relative);
                            _context.BeginFigure(point, true, false);
                            openFigure = true;
                            previousControlPoint = null;
                            break;

                        case Command.Line:
                            point = ReadPoint(reader, point, relative);
                            _context.LineTo(point, true, false);
                            previousControlPoint = null;
                            break;

                        case Command.HorizontalLine:
                            if (!relative)
                            {
                                point = new Point(ReadDouble(reader), point.Y);
                            }
                            else
                            {
                                point = new Point(point.X + ReadDouble(reader), point.Y);
                            }

                            _context.LineTo(point, true, false);
                            previousControlPoint = null;
                            break;

                        case Command.VerticalLine:
                            if (!relative)
                            {
								point = new Point(point.X, ReadDouble(reader));
							}
							else
                            {
                                point = new Point(point.X, point.Y + ReadDouble(reader));
                            }

                            _context.LineTo(point, true, false);
                            previousControlPoint = null;
                            break;

                        case Command.QuadraticBezierCurve:
                            {
                                Point handle = ReadPoint(reader, point, relative);
                                previousControlPoint = handle;
                                ReadSeparator(reader);
                                point = ReadPoint(reader, point, relative);
                                _context.QuadraticBezierTo(handle, point, true, false);
                                break;
                            }

                        case Command.SmoothQuadraticBezierCurve:
                            {
                                Point end = ReadPoint(reader, point, relative);
                                
                                if(previousControlPoint != null)
                                    previousControlPoint = MirrorControlPoint((Point)previousControlPoint, point);
                                
                                _context.QuadraticBezierTo(previousControlPoint ?? point, end, true, false);
                                point = end;
                                break;
                            }

                        case Command.CubicBezierCurve:
                            {
                                Point point1 = ReadPoint(reader, point, relative);
                                ReadSeparator(reader);
                                Point point2 = ReadPoint(reader, point, relative);
                                previousControlPoint = point2;
                                ReadSeparator(reader);
                                point = ReadPoint(reader, point, relative);
                                _context.BezierTo(point1, point2, point, true, false);
                                break;
                            }
                            
                        case Command.SmoothCubicBezierCurve:
                            {
                                Point point2 = ReadPoint(reader, point, relative);
                                ReadSeparator(reader);
                                Point end = ReadPoint(reader, point, relative);
                                
                                if(previousControlPoint != null)
                                    previousControlPoint = MirrorControlPoint((Point)previousControlPoint, point);
                                
                                _context.BezierTo(previousControlPoint ?? point, point2, end, true, false);
                                previousControlPoint = point2;
                                point = end;
                                break;
                            }

                        case Command.Arc:
                            {
                                Size size = ReadSize(reader);
                                ReadSeparator(reader);
                                double rotationAngle = ReadDouble(reader);
                                ReadSeparator(reader);
                                bool isLargeArc = ReadBool(reader);
                                ReadSeparator(reader);
                                SweepDirection sweepDirection = ReadBool(reader) ? SweepDirection.Clockwise : SweepDirection.Counterclockwise;
                                ReadSeparator(reader);
                                point = ReadPoint(reader, point, relative);

                                _context.ArcTo(point, size, rotationAngle, isLargeArc, sweepDirection, true, false);
                                previousControlPoint = null;
                                break;
                            }

                        case Command.Close:
                            _context.SetClosedState(true);
                            openFigure = false;
                            previousControlPoint = null;
                            break;

                        default:
                            throw new NotSupportedException("Unsupported command");
                    }
                }

                if (openFigure)
                {
                    _context.SetClosedState(false);
                }
            }
        }

        private Point MirrorControlPoint(Point controlPoint, Point center)
        {
            Point dir = (controlPoint - center);
            return center - dir;
        }

        private static bool ReadCommand(
            StringReader reader,
            ref Command command,
            ref bool relative)
        {
			// https://www.w3.org/TR/SVG/paths.html#PathDataMovetoCommands
			// If a moveto is followed by multiple pairs of coordinates, the subsequent pairs are treated as implicit lineto commands
			if (command == Command.Move)
			{
				command = Command.Line;
			}

			ReadWhitespace(reader);

            int i = reader.Peek();

            if (i == -1)
            {
                return false;
            }
            else
            {
                char c = (char)i;
                Command next = Command.None;

                if (!Commands.TryGetValue(char.ToUpperInvariant(c), out next))
                {
                    if ((char.IsDigit(c) || c == '.' || c == '+' || c == '-') &&
                        (command != Command.None))
                    {
                        return true;
                    }
					else if (c == ',')
					{
						reader.Read();
						return true;
					}
                    else
                    {
                        throw new InvalidDataException("Unexpected path command '" + c + "'.");
                    }
                }

                command = next;
                relative = char.IsLower(c);
                reader.Read();
                return true;
            }
        }

        private static FillRule ReadFillRule(StringReader reader)
        {
			ReadWhitespace(reader);

            int i = reader.Read();
            if (i == -1)
            {
                throw new InvalidDataException("Invalid fill rule");
            }
            char c = (char)i;
            FillRule rule;

            if (!FillRules.TryGetValue(c, out rule))
            {
                throw new InvalidDataException("Invalid fill rule");
            }

            return rule;
        }

        private static double ReadDouble(StringReader reader)
        {
            ReadWhitespace(reader);

            // TODO: Handle Infinity, NaN and scientific notation.
            StringBuilder b = new StringBuilder();
            bool readSign = false;
            bool readPoint = false;
            bool readExponent = false;
            int i;

            while ((i = reader.Peek()) != -1)
            {
                char c = char.ToUpperInvariant((char)i);

                if (((c == '+' || c == '-') && !readSign) ||
                    (c == '.' && !readPoint) ||
                    (c == 'E' && !readExponent) ||
                    char.IsDigit(c))
                {
                    if (b.Length != 0 && !readExponent && c == '-')
                        break;
                    
                    b.Append(c);
                    reader.Read();

                    if (!readSign)
                    {
                        readSign = c == '+' || c == '-';
                    }

                    if (!readPoint)
                    {
                        readPoint = c == '.';
                    }

                    if (c == 'E')
                    {
                        readSign = false;
                        readExponent = true;
                    }
                }
                else
                {
                    break;
                }
            }

            return double.Parse(b.ToString(), CultureInfo.InvariantCulture);
        }

        private static Point ReadPoint(StringReader reader, Point current, bool relative)
        {
            if (!relative)
            {
                current = new Point();
            }

            ReadWhitespace(reader);
            double x = current.X + ReadDouble(reader);
            ReadSeparator(reader);
            double y = current.Y + ReadDouble(reader);
            return new Point(x, y);
        }

        private static Size ReadSize(StringReader reader)
        {
            ReadWhitespace(reader);
            double x = ReadDouble(reader);
            ReadSeparator(reader);
            double y = ReadDouble(reader);
            return new Size(x, y);
        }

        private static bool ReadBool(StringReader reader)
        {
            return ReadDouble(reader) != 0;
        }

        private static Point ReadRelativePoint(StringReader reader, Point lastPoint)
        {
            ReadWhitespace(reader);
            double x = ReadDouble(reader);
            ReadSeparator(reader);
            double y = ReadDouble(reader);
            return new Point(lastPoint.X + x, lastPoint.Y + y);
        }

        private static void ReadSeparator(StringReader reader)
        {
            int i;
            bool readComma = false;

            while ((i = reader.Peek()) != -1)
            {
                char c = (char)i;

                if (char.IsWhiteSpace(c))
                {
                    reader.Read();
                }
                else if (c == ',')
                {
                    if (readComma)
                    {
                        throw new InvalidDataException("Unexpected ','.");
                    }

                    readComma = true;
                    reader.Read();
                }
                else
                {
                    break;
                }
            }
        }

        private static void ReadWhitespace(StringReader reader)
        {
            int i;

            while ((i = reader.Peek()) != -1)
            {
                char c = (char)i;

                if (char.IsWhiteSpace(c))
                {
                    reader.Read();
                }
                else
                {
                    break;
                }
            }
        }
    }
}
 