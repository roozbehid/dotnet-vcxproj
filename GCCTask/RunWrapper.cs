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
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace CCTask
{
	internal sealed class RunWrapper
	{
		internal RunWrapper(string path, string options,string preLoadApp)
		{
            if (!string.IsNullOrEmpty(preLoadApp))
            {
                var enviromentPath = System.Environment.GetEnvironmentVariable("PATH");
                enviromentPath = enviromentPath + ";" + Environment.GetEnvironmentVariable("SystemRoot") + @"\sysnative";

                Console.WriteLine(enviromentPath);
                var paths = enviromentPath.Split(';');
                var exePath = paths.Select(x => Path.Combine(x, preLoadApp))
                                   .Where(x => File.Exists(x))
                                   .FirstOrDefault();
                if (!String.IsNullOrEmpty(exePath))
                {
                    options = path + " " + options;
                    path = exePath;
                }
            }

            startInfo = new ProcessStartInfo(path, options);
			startInfo.UseShellExecute = false;
			startInfo.RedirectStandardError = true;
			startInfo.RedirectStandardInput = true;
			startInfo.RedirectStandardOutput = true;
		}

		internal bool Run()
		{
			var process = new Process { StartInfo = startInfo };
			process.OutputDataReceived += (sender, e) =>
			{
				if(!string.IsNullOrEmpty(e.Data))
				{
					Logger.Instance.LogMessage(e.Data);
				}
			};

            string prevErrorRecieved = "";
			process.ErrorDataReceived += (sender, e) =>
			{
				if(string.IsNullOrEmpty(e.Data))
				{
					return;
				}

                if ( e.Data.Contains("error:") || e.Data.Contains("warning:") || e.Data.Contains("note:") )
                {
                    if (!String.IsNullOrEmpty(prevErrorRecieved))
                        Logger.Instance.LogDecide(prevErrorRecieved);

                    prevErrorRecieved = e.Data;
                }
                else
                {
                    prevErrorRecieved = prevErrorRecieved + "\r" + e.Data;
				}
			};

			process.Start();
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();

			process.WaitForExit();
			var successfulExit = (process.ExitCode == 0);

            if (!String.IsNullOrEmpty(prevErrorRecieved))
                Logger.Instance.LogDecide(prevErrorRecieved);

            process.Close();
			return successfulExit;
		}

		private readonly ProcessStartInfo startInfo;
	}
}

