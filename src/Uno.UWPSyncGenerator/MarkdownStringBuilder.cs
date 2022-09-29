#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.Extensions;

namespace Uno.UWPSyncGenerator
{
	/// <summary>
	/// Builder for a (GitHub-flavoured) Markdown-formatted document.
	/// </summary>
	public class MarkdownStringBuilder : IndentedStringBuilder
	{
		public int CurrentSectionDepth { get; private set; }
		private int? _tableColumns;

		/// <summary>
		/// Append a new paragraph.
		/// </summary>
		/// <param name="paragraph">Paragraph content</param>
		public void AppendParagraph(string paragraph)
		{
			AppendLine(paragraph);
			AppendLine(); //Starts new line
			AppendLine(); //Blank line
		}

		/// <summary>
		/// Ends the current paragraph.
		/// </summary>
		public void AppendParagraph()
		{
			AppendLine(); //Starts new line
			AppendLine(); //Blank line
		}

		/// <summary>
		/// Append a Markdown-formatted horizontal line.
		/// </summary>
		public void AppendHorizontalRule()
		{
			AppendParagraph("---");
		}

		/// <summary>
		/// Start a new Markdown section.
		/// </summary>
		/// <param name="header">Section title</param>
		/// <returns>Disposable to be used in a 'using' block</returns>
		public IDisposable Section(string header)
		{
			CurrentSectionDepth += 1;
			AppendLine("");
			for (int i = 0; i < CurrentSectionDepth; i++)
			{
				Append("#");
			}
			Append($" {header}");
			AppendLine();
			AppendLine();
			return new DisposableAction(() =>
			{
				AppendLine();
				CurrentSectionDepth -= 1;
			});
		}

		/// <summary>
		/// Start a new Markdown-formatted table.
		/// </summary>
		/// <param name="columnHeaders">Column headers</param>
		/// <returns>Disposable to be used in a 'using' block</returns>
		public IDisposable Table(params string[] columnHeaders)
		{
			if (_tableColumns.HasValue)
			{
				throw new InvalidOperationException("Tables can't be nested.");
			}

			_tableColumns = columnHeaders.Length;
			AppendRow(columnHeaders);
			var underlines = columnHeaders.Select(_ => "---").ToArray();
			AppendRow(underlines);
			return new DisposableAction(() =>
			{
				_tableColumns = null;
				AppendLine();
			});
		}

		/// <summary>
		/// Append a row to a Markdown table.
		/// </summary>
		/// <param name="columnEntries">Column entries.</param>
		/// <remarks>This must be called within a 'using (<see cref="Table(string[])"/>) { ... }' block.</remarks>
		public void AppendRow(params string[] columnEntries)
		{
			if (!_tableColumns.HasValue)
			{
				throw new InvalidOperationException($"{nameof(AppendRow)} must be called within a using ({nameof(Table)}) block.");
			}

			if (columnEntries.Length > _tableColumns.Value)
			{
				throw new InvalidOperationException($"Row has {columnEntries.Length} columns but current table only has {_tableColumns.Value} columns.");
			}

			AppendLine("");
			Append("|");
			foreach (var entry in columnEntries)
			{
				Append($" {entry} |");
			}
			AppendLine();
		}

		/// <summary>
		/// Append each cell to the current Markdown table, wrapping to a new row when necessary.
		/// </summary>
		/// <param name="cells">Cell contents</param>
		public void AppendCells(IList<string> cells)
		{
			if (!_tableColumns.HasValue)
			{
				throw new InvalidOperationException($"{nameof(AppendCells)} must be called within a using ({nameof(Table)}) block.");
			}

			for (int i = 0; i < cells.Count; i++)
			{
				if (i % _tableColumns.Value == 0)
				{
					// Wrap to new row
					if (i != 0)
					{
						AppendLine();
					}
					AppendLine("");
					Append("|");
				}

				Append($" {cells[i]} |");
			}
			AppendLine();
		}

		/// <summary>
		/// Append a comment string which won't be visible in the displayed Markdown document.
		/// </summary>
		/// <param name="comment">Comment content</param>
		public void AppendComment(string comment)
		{
			AppendLine($"<!-- {comment} -->");
			AppendLine();
		}

		/// <summary>
		/// Returns a Markdown-formatted hyperlink.
		/// </summary>
		/// <param name="linkText">Link text to display</param>
		/// <param name="linkUrl">Url to link to</param>
		/// <returns>String in Markdown's hyperlink format</returns>
		public static string Hyperlink(string linkText, string linkUrl) => $"[{linkText}]({linkUrl})";
	}
}
