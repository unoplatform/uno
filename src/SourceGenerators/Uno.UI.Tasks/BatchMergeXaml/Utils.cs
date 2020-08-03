using System;
using System.IO;
using System.Text;
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
            bool rewrite = true;
            var fullPath = Path.GetFullPath(path);
            try
            {
                string existingContents = File.ReadAllText(fullPath);
                if (String.Equals(existingContents, contents))
                {
                    rewrite = false;
                }
            }
            catch
            {
            }

            if (rewrite)
            {
                File.WriteAllText(fullPath, contents);
            }

            return fullPath;
        }
    }
}
