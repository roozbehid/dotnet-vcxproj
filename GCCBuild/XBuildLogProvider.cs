/**
 * GCCBuild
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

namespace GCCBuild
{
    public interface ILogProvider
    {
        void LogMessage(string message, params object[] parameters);
        void LogWarning(string message, ShellAppConversion shellApp, params object[] parameters);
        void LogError(string message, ShellAppConversion shellApp, params object[] parameters);
        void LogDecide(string message, ShellAppConversion shellApp, params object[] parameters);
        void LogLinker(string message, ShellAppConversion shellApp, params object[] parameters);
        void LogCommandLine(string cmdLine);
    }

    public sealed class Logger
    {
        public static ILogProvider Instance { get; internal set; }
    }

    public sealed class XBuildLogProvider : ILogProvider
	{
        private readonly TaskLoggingHelper log;
        private readonly object sync;
        public static Regex err_rgx;
        public static Regex warn_rgx;

        public XBuildLogProvider(TaskLoggingHelper log)
		{
			this.log = log;
			sync = new object();

            string err_pattern = @"(.*?):((\d+):((\d+):)?)? .*[Ee]rror: ([\s\S]*)";
            err_rgx = new Regex(err_pattern, RegexOptions.IgnoreCase);

            string warn_pattern = @"(.*?):((\d+):((\d+):)?)? .*[Ww]arning: ([\s\S]*)";
            warn_rgx = new Regex(warn_pattern, RegexOptions.IgnoreCase);
        }

		public void LogMessage(string message, params object[] parameters)
		{
			lock(sync)
			{
				log.LogMessage(message, parameters);
			}
		}
        public void LogDecide(string message, ShellAppConversion shellApp, params object[] parameters)
        {
            MatchCollection err_matches = XBuildLogProvider.err_rgx.Matches(message);
            MatchCollection warn_matches = XBuildLogProvider.warn_rgx.Matches(message);

            if (err_matches.Count > 0)
                LogError(message, shellApp, parameters);
            else  if (warn_matches.Count > 0)
                LogWarning(message, shellApp, parameters);
            else if (message.Contains("note:"))
            {
                message = message.Replace("note:", "warning:");
                LogWarning(message, shellApp, parameters);
            }
            else
                LogOther(message, shellApp, parameters);
        }

        public void LogLinker(string message, ShellAppConversion shellApp, params object[] parameters)
        {
            lock (sync)
            {
                string pattern = @"(.*?):(?![\\\/])(.*?)";
                Regex rgx = new Regex(pattern, RegexOptions.IgnoreCase);
                MatchCollection matches = rgx.Matches(message);
                if ((matches.Count >= 1) && (matches[0].Groups.Count > 2))
                {
                    GroupCollection groups = matches[0].Groups;
                    string filename = groups[1].Value;
                    if (shellApp != null && shellApp.convertpath)
                    {
                        message = message.Substring(message.IndexOf(filename) + filename.Length + 1);
                        filename = shellApp.ConvertWSLPathToWin(filename);
                        
                    }
                    log.LogError(null, null, null, filename, 0, 0, 0, 0, message);
                }
                else
                    log.LogError(message, parameters);
            }
        }

        public void LogOther(string message, ShellAppConversion shellApp, params object[] parameters)
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
                    if (shellApp != null && shellApp.convertpath)
                        filename = shellApp.ConvertWSLPathToWin(filename);
                    log.LogWarning(null, null, null, filename, lineNumber, colNumber, 0, 0, groups[4].Value);
                }
                else
                    log.LogWarning(message, parameters);
            }
        }

        public void LogWarning(string message, ShellAppConversion shellApp, params object[] parameters)
		{
			lock(sync)
			{
                MatchCollection matches = warn_rgx.Matches(message);
                if ((matches.Count == 1) && (matches[0].Groups.Count > 4))
                {
                    GroupCollection groups = matches[0].Groups;
                    int lineNumber = 0;
                    int colNumber = 0;
                    string filename = groups[1].Value;
                    int.TryParse(groups[3].Value, out lineNumber);
                    int.TryParse(groups[5].Value, out colNumber);
                    
                    if (shellApp != null && shellApp.convertpath)
                        filename = shellApp.ConvertWSLPathToWin(filename);
                    log.LogWarning(null, null, null, filename, lineNumber, colNumber, 0, 0, groups[6].Value);
                }
                else
                    log.LogWarning(message, parameters);
            }
		}

        public void LogCommandLine(string cmdLine)
        {
            log.LogCommandLine(cmdLine);
        }
        public void LogError(string message, ShellAppConversion shellApp, params object[] parameters)
		{
			lock(sync)
			{
                MatchCollection matches = err_rgx.Matches(message);
                if ((matches.Count == 1) && (matches[0].Groups.Count > 4))
                {
                    GroupCollection groups = matches[0].Groups;
                    int lineNumber = 0;
                    int colNumber = 0;
                    string filename = groups[1].Value;
                    int.TryParse(groups[3].Value, out lineNumber);
                    int.TryParse(groups[5].Value, out colNumber);
                    
                    if (shellApp != null && shellApp.convertpath)
                        filename = shellApp.ConvertWSLPathToWin(filename);
                    log.LogError(null, null, null, filename, lineNumber, colNumber, 0, 0, groups[6].Value);
                }
                else
                    log.LogError(message, parameters);
            }
		}


	}
}

