/**
 * CCTask
 * 
 * Copyright 2012 Konrad Kruczyński <konrad.kruczynski@gmail.com>
 * 
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:

 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.

 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */ 
using System;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace CCTask
{
	public sealed class XBuildLogProvider : ILogProvider
	{
		public XBuildLogProvider(TaskLoggingHelper log)
		{
			this.log = log;
			sync = new object();
		}

		public void LogMessage(string message, params object[] parameters)
		{
			lock(sync)
			{
				log.LogMessage(message, parameters);
			}
		}
        public void LogDecide(string message, bool WSLPathToNT, params object[] parameters)
        {
            if (message.Contains("error:"))
                LogError(message, WSLPathToNT, parameters);
            else  if (message.Contains("warning:"))
                LogWarning(message, WSLPathToNT, parameters);
            else if (message.Contains("note:"))
            {
                message = message.Replace("note:", "warning:");
                LogWarning(message, WSLPathToNT, parameters);
            }
            else
                LogOther(message, WSLPathToNT, parameters);
        }

        public void LogOther(string message, bool WSLPathToNT, params object[] parameters)
        {
            lock (sync)
            {
                string pattern = @"(.*):(.*):(.*): (.*)";
                Regex rgx = new Regex(pattern, RegexOptions.IgnoreCase);
                MatchCollection matches = rgx.Matches(message);
                if ((matches.Count == 1) && (matches[0].Groups.Count > 4))
                {
                    GroupCollection groups = matches[0].Groups;
                    int lineNumber = 0;
                    int colNumber = 0;
                    string filename = groups[2].Value;
                    if (WSLPathToNT)
                        filename = Utilities.ConvertWSLPathToWin(filename);
                    log.LogWarning(null, null, null, filename, lineNumber, colNumber, 0, 0, groups[4].Value);
                }
                else
                    log.LogWarning(message, parameters);
            }
        }

        public void LogWarning(string message, bool WSLPathToNT, params object[] parameters)
		{
			lock(sync)
			{
                string pattern = @"(.*):(\d+):(\d+): .*warning: (.*)";
                Regex rgx = new Regex(pattern, RegexOptions.IgnoreCase);
                MatchCollection matches = rgx.Matches(message);
                if ((matches.Count == 1) && (matches[0].Groups.Count > 4))
                {
                    GroupCollection groups = matches[0].Groups;
                    int lineNumber = 0;
                    int colNumber = 0;
                    int.TryParse(groups[2].Value, out lineNumber);
                    int.TryParse(groups[3].Value, out colNumber);
                    string filename = groups[1].Value;
                    if (WSLPathToNT)
                        filename = Utilities.ConvertWSLPathToWin(filename);
                    log.LogWarning(null, null, null, filename, lineNumber, colNumber, 0, 0, groups[4].Value);
                }
                else
                    log.LogWarning(message, parameters);
            }
		}

        public void LogCommandLine(string cmdLine)
        {
            log.LogCommandLine(cmdLine);
        }
        public void LogError(string message, bool WSLPathToNT, params object[] parameters)
		{
			lock(sync)
			{
                string pattern = @"(.*):(\d+):(\d+): .*error: (.*)";
                Regex rgx = new Regex(pattern, RegexOptions.IgnoreCase);
                MatchCollection matches = rgx.Matches(message);
                if ((matches.Count == 1) && (matches[0].Groups.Count > 4))
                {
                    GroupCollection groups = matches[0].Groups;
                    int lineNumber = 0;
                    int colNumber = 0;
                    int.TryParse(groups[2].Value, out lineNumber);
                    int.TryParse(groups[3].Value, out colNumber);
                    string filename = groups[1].Value;
                    if (WSLPathToNT)
                        filename = Utilities.ConvertWSLPathToWin(filename);
                    log.LogError(null, null, null, filename, lineNumber, colNumber, 0, 0, groups[4].Value);
                }
                else
                    log.LogError(message, parameters);
            }
		}

		private readonly TaskLoggingHelper log;
		private readonly object sync;
	}
}

