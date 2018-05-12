using System;
using System.IO;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Linq;

namespace CCTask
{
	internal static class Utilities
	{
		public static bool RunAndGetOutput(string path, string options, out string output, string preLoadApp)
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

            var startInfo = new ProcessStartInfo(path, options);
			startInfo.UseShellExecute = false;
			startInfo.RedirectStandardError = true;
			startInfo.RedirectStandardInput = true;
			startInfo.RedirectStandardOutput = true;
			var process = new Process { StartInfo = startInfo };
			process.Start();
			process.WaitForExit();
			output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
			return process.ExitCode == 0;
		}
	}
}

