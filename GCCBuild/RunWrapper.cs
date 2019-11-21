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
	internal sealed class RunWrapper : IDisposable
	{
        private readonly ProcessStartInfo startInfo;
        private ShellAppConversion shellApp;
        private string realCommandLine;

        public void Dispose()
        {
            try
            {
                if (!String.IsNullOrEmpty(shellApp.prerunapp) && !String.IsNullOrEmpty(startInfo.FileName) && startInfo.FileName.Contains("GCCBuildPreRun_"))
                    File.Delete(startInfo.FileName);

                if (!String.IsNullOrEmpty(startInfo.Arguments) && startInfo.Arguments.Length > 1 && !startInfo.Arguments.StartsWith("@") && Path.GetExtension(startInfo.Arguments.Substring(1)) == ".rsp" && File.Exists(startInfo.Arguments.Substring(1)))
                    File.Delete(startInfo.Arguments.Substring(1));
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError("~RunWrapper caused an exception:" + ex, shellApp);
            }
        }

        internal RunWrapper(string path, string options, ShellAppConversion shellApp, bool useresponse)
        {
            realCommandLine = $"{path} {options}";

            if (!Utilities.isLinux() && useresponse && (path.Length + options.Length) > 8100) //technically it is 8191
            {
                var responsefilename = Path.Combine(shellApp.tmpfolder, "response_" + Guid.NewGuid().ToString() + ".rsp");
                File.WriteAllText(responsefilename, $"{options}");
                options = $"@{responsefilename}";
            }

            if (!string.IsNullOrEmpty(shellApp.shellapp)) //try to find full path of it from path enviroment!
            {
                var enviromentPath = System.Environment.GetEnvironmentVariable("PATH");
                if (!Utilities.isLinux())
                    enviromentPath = enviromentPath + ";" + Environment.GetEnvironmentVariable("SystemRoot") + @"\sysnative";

                //Console.WriteLine(enviromentPath);
                var paths = enviromentPath.Split(Utilities.isLinux() ? ':' : ';');
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
            

            ///
            /// if there is a prerun app. bundle that prerun and compiler\linker\archiver into one batch\bash file and then run that!
            ///
            if (!String.IsNullOrEmpty(shellApp.prerunapp))
            {
                string newfilename;
                if (!Utilities.isLinux())
                {
                    newfilename = Path.Combine(shellApp.tmpfolder, "GCCBuildPreRun_" + Guid.NewGuid().ToString()+ ".bat");
                    File.WriteAllText(newfilename, $"@{shellApp.prerunapp}\r\n@{path} {options}");
                }
                else
                {
                    newfilename = Path.Combine(shellApp.tmpfolder, "GCCBuildPreRun_" + Guid.NewGuid().ToString()+ ".sh");
                    File.WriteAllText(newfilename, $"#!/bin/bash\n{shellApp.prerunapp}\n{path} {options}");
                }

                path = newfilename;
                options = "";
            }

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
            {
                if (!String.IsNullOrEmpty(shellApp.prerunapp))
                    Logger.Instance.LogMessage($"PreRun Command : {startInfo.FileName}");
                Logger.Instance.LogMessage($"\n{realCommandLine}");
            }

            Logger.Instance.LogCommandLine($"{realCommandLine}");

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
                    else if ( (line.LastIndexOf(":") == line.Length - 1) || XBuildLogProvider.linker_rgx1.Match(line).Success || 
                        XBuildLogProvider.linker_rgx2.Match(line).Success || XBuildLogProvider.linker_rgx3.Match(line).Success)

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
            

            if (showBanner)
            {
                if (!String.IsNullOrEmpty(shellApp.prerunapp))
                    Logger.Instance.LogMessage($"\nPreRun Command : {startInfo.FileName}");
                Logger.Instance.LogMessage($"\n{realCommandLine}");
            }

            Logger.Instance.LogCommandLine($"{realCommandLine}");
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
            string prevErrorRecieved = "";
            string previnfileincluded = "";

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

                        if (!String.IsNullOrEmpty(previnfileincluded))
                        {
                            prevErrorRecieved = line + "\n\r" + previnfileincluded + "\n\r";
                            previnfileincluded = "";
                        }
                        else
                            prevErrorRecieved = line + "\n\r";
                    }
                    else
                    {
                        if (line.StartsWith("In file included"))
                            previnfileincluded = previnfileincluded + line + "\n\r";
                        else
                            prevErrorRecieved = prevErrorRecieved + line + "\n\r";
                    }
                }

            }

            var successfulExit = (process.ExitCode == 0);

            if (!String.IsNullOrEmpty(prevErrorRecieved))
                Logger.Instance.LogDecide(prevErrorRecieved, shellApp);

            process.Close();
			return successfulExit;
		}

        public bool RunCompilerAndGetOutput(out string output, bool showBanner)
        {
            try
            {
                var process = new Process { StartInfo = startInfo };

                if (showBanner)
                    Logger.Instance.LogMessage($"\n{realCommandLine}");

                Logger.Instance.LogCommandLine($"{realCommandLine}");
                process.Start();

                string cv_error = null;
                Thread et = new Thread(() => { cv_error = process.StandardError.ReadToEnd(); });
                et.Start();

                string cv_out = null;
                Thread ot = new Thread(() => { cv_out = process.StandardOutput.ReadToEnd(); });
                ot.Start();

                process.WaitForExit();
                et.Join();
                ot.Join();
                output = /*cv_error +*/ cv_out;
                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error running program. Is your PATH and ENV variables correct? Command to run was {realCommandLine}.\n" + ex.ToString(), null);
                output = "FATAL";
                return false;
            }
        }

    }
}

