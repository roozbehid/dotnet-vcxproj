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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace GCCBuild
{
	internal sealed class RunWrapper
	{
        private readonly ProcessStartInfo startInfo;
        private ShellAppConversion shellApp;

        internal RunWrapper(string path, string options, ShellAppConversion shellApp)
		{
            if (!string.IsNullOrEmpty(shellApp.shellapp))
            {
                var enviromentPath = System.Environment.GetEnvironmentVariable("PATH");
                enviromentPath = enviromentPath + ";" + Environment.GetEnvironmentVariable("SystemRoot") + @"\sysnative";

                //Console.WriteLine(enviromentPath);
                var paths = enviromentPath.Split(';');
                var exePath = paths.Select(x => Path.Combine(x, shellApp.shellapp))
                                   .Where(x => File.Exists(x))
                                   .FirstOrDefault();
                if (!String.IsNullOrEmpty(exePath))
                {
                    options = path + " " + options;
                    path = exePath;
                }
            }

            this.shellApp = shellApp;
            startInfo = new ProcessStartInfo(path, options);
			startInfo.UseShellExecute = false;
			startInfo.RedirectStandardError = true;
			//startInfo.RedirectStandardInput = true;
			startInfo.RedirectStandardOutput = true;
            startInfo.StandardErrorEncoding = System.Text.Encoding.UTF8;
            startInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;
        }

        internal bool RunArchiver(bool showBanner)
        {
            return RunCompiler(showBanner);
        }

        internal bool RunLinker(bool showBanner)
        {
            var process = new Process { StartInfo = startInfo };
            string prevErrorRecieved = "";

            if (showBanner)
                Logger.Instance.LogMessage($"\n{startInfo.FileName} {startInfo.Arguments}");

            Logger.Instance.LogCommandLine($"{startInfo.FileName} {startInfo.Arguments}");
            process.Start();

            string output = process.StandardError.ReadToEnd();

            using (var reader = new StringReader(output))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                    if (string.IsNullOrEmpty(line))
                    {
                        ;
                    }
                    else if ( (line.LastIndexOf(":") == line.Length - 1) /*|| (line.LastIndexOf("'")== line.Length -1)*/)
                    {
                        if (!String.IsNullOrEmpty(prevErrorRecieved))
                            Logger.Instance.LogLinker(prevErrorRecieved, shellApp);

                        prevErrorRecieved = line;

                    }
                    else if (line.IndexOf("/ld: ") > 0)
                    {
                        if (!String.IsNullOrEmpty(prevErrorRecieved))
                            Logger.Instance.LogLinker(prevErrorRecieved, shellApp);
                        Logger.Instance.LogError(line, null);
                        prevErrorRecieved = "";
                    }
                    else
                    {
                        if (!String.IsNullOrWhiteSpace(prevErrorRecieved))
                            prevErrorRecieved = prevErrorRecieved + "\n\r" + line;
                        else
                            prevErrorRecieved = line;
                    }

            }
            process.WaitForExit();
            var successfulExit = (process.ExitCode == 0);

            if (!String.IsNullOrEmpty(prevErrorRecieved))
                Logger.Instance.LogLinker(prevErrorRecieved, shellApp);

            process.Close();
            return successfulExit;
        }

        internal bool RunCompiler(bool showBanner)
        {
            var process = new Process { StartInfo = startInfo };
            string prevErrorRecieved = "";

            if (showBanner)
                Logger.Instance.LogMessage($"\n{startInfo.FileName} {startInfo.Arguments}");

            Logger.Instance.LogCommandLine($"{startInfo.FileName} {startInfo.Arguments}");
            process.Start();

            string cv_error = null;
            Thread et = new Thread(() => { cv_error = process.StandardError.ReadToEnd(); });
            et.Start();

            string cv_out = null;
            Thread ot = new Thread(() => { cv_out = process.StandardOutput.ReadToEnd(); });
            ot.Start();

            process.WaitForExit();
            et.Join();
            string output = cv_error;// process.StandardError.ReadToEnd();

            using (var reader = new StringReader(output))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    MatchCollection err_matches = XBuildLogProvider.err_rgx.Matches(line);
                    MatchCollection warn_matches = XBuildLogProvider.warn_rgx.Matches(line);

                    if (string.IsNullOrEmpty(line))
                    {
                        ;
                    }
                    else if ( (err_matches.Count > 0 || warn_matches.Count > 0 || line.ToLower().Contains("note:")) && (!line.StartsWith(" ")))
                    {
                        if (!String.IsNullOrEmpty(prevErrorRecieved))
                            Logger.Instance.LogDecide(prevErrorRecieved, shellApp);

                        prevErrorRecieved = line;
                    }
                    else
                    {
                        prevErrorRecieved = prevErrorRecieved + "\n\r" + line;
                    }
                }

            }

            var successfulExit = (process.ExitCode == 0);

            if (!String.IsNullOrEmpty(prevErrorRecieved))
                Logger.Instance.LogDecide(prevErrorRecieved, shellApp);

            process.Close();
			return successfulExit;
		}

	}
}

