using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Blacker.MangaScraper.Helpers
{
    static class ProcessHelper
    {
        /// <summary>
        /// Quotes all arguments that contain whitespace, or begin with a quote and returns a single
        /// argument string for use with Process.Start().
        /// </summary>
        /// <param name="args">A list of strings for arguments, may not contain null, '\0', '\r', or '\n'</param>
        /// <returns>The combined list of escaped/quoted strings</returns>
        /// Implementation based on: http://csharptest.net/529/how-to-correctly-escape-command-line-arguments-in-c/
        public static string EscapeArguments(params string[] args)
        {
            StringBuilder arguments = new StringBuilder();
            Regex invalidChar = new Regex("[\x00\x0a\x0d]");        // these can not be escaped
            Regex needsQuotes = new Regex(@"\s|""");                // contains whitespace or two quote characters
            Regex escapeQuote = new Regex(@"(\\*)(""|$)");          // one or more '\' followed with a quote or end of string

            foreach(var arg in args)
            {
                if (arg == null) 
                    throw new ArgumentNullException("arg");

                if (invalidChar.IsMatch(arg))
                    throw new ArgumentOutOfRangeException("arg");

                if (arg == String.Empty)
                {
                    arguments.Append("\"\"");
                }
                else if (!needsQuotes.IsMatch(arg))
                {
                    arguments.Append(arg);
                }
                else
                {
                    arguments.Append('"');
                    arguments.Append(escapeQuote.Replace(arg,
                        m =>
                            m.Groups[1].Value + m.Groups[1].Value + (m.Groups[2].Value == "\"" ? "\\\"" : "")));
                    arguments.Append('"');
                }

                arguments.Append(' ');
            }

            return arguments.ToString();
        }
    }
}
