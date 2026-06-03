using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;

namespace Uno.UI.Tasks.BatchMerge
{
	class Utils
	{
		//The XmlWriter can't handle &#xE0E5 unless we escape/unescape the ampersand
		public static string UnEscapeAmpersand(string s)
		{
			return s.Replace("&amp;", "&");
		}

		public static string EscapeAmpersand(string s)
		{
			return s.Replace("&", "&amp;");
		}

		public static string DocumentToString(Action<XmlWriter> action)
		{
			StringWriter sw = new StringWriter();
			XmlWriterSettings settings = new XmlWriterSettings { Indent = true, OmitXmlDeclaration = true, Encoding = Encoding.UTF8 };
			XmlWriter writer = XmlWriter.Create(sw, settings);
			action(writer);
			writer.Flush();
			return Utils.UnEscapeAmpersand(sw.ToString());
		}

		public static string RewriteFileIfNecessary(string path, string contents)
		{
			var fullPath = Path.GetFullPath(path);

			try
			{
				if (File.Exists(fullPath) && String.Equals(File.ReadAllText(fullPath), contents))
				{
					// Content is already up to date. Leave the file untouched so parallel variant
					// builds that merge into the same shared output don't needlessly churn it.
					return fullPath;
				}
			}
			catch
			{
				// Unreadable/locked existing file: fall through and rewrite it.
			}

			AtomicWrite(fullPath, contents);

			return fullPath;
		}

		// Several Uno.UI.FluentTheme.* project variants merge into the same shared output file
		// and may run in parallel. Write to a per-writer temp file and atomically swap it into
		// place so a concurrent reader (the XAML source generator consuming the merged Page)
		// never observes a partially written file, and concurrent writers can't corrupt each
		// other's output.
		private static void AtomicWrite(string fullPath, string contents)
		{
			var directory = Path.GetDirectoryName(fullPath);
			var tempPath = Path.Combine(directory, "." + Path.GetFileName(fullPath) + "." + Guid.NewGuid().ToString("N") + ".tmp");

			File.WriteAllText(tempPath, contents);

			try
			{
				for (int attempt = 0; ; attempt++)
				{
					try
					{
						if (File.Exists(fullPath))
						{
							File.Replace(tempPath, fullPath, destinationBackupFileName: null, ignoreMetadataErrors: true);
						}
						else
						{
							File.Move(tempPath, fullPath);
						}

						return;
					}
					catch (IOException) when (attempt < 5)
					{
						// Transient contention: another writer swapped the file (so File.Move now
						// collides) or a reader briefly holds it. Retry the atomic swap; on the next
						// attempt File.Exists is true and we take the File.Replace path.
						Thread.Sleep(100);
					}
				}
			}
			finally
			{
				if (File.Exists(tempPath))
				{
					try
					{
						File.Delete(tempPath);
					}
					catch
					{
					}
				}
			}
		}
	}
}
