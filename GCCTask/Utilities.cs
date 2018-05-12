using System;
using System.IO;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Linq;
using System.Text;

namespace CCTask
{
	internal static class Utilities
	{
        public static string ConvertWinPathToWSL(string path)
        {
            StringBuilder FullPath = new StringBuilder(Path.GetFullPath(path));
            //FullPath[0] = (FullPath[0].ToString().ToLower())[0];
            return @"/mnt/"+ FullPath.ToString().Replace(@":\",@"/").Replace(@"\",@"/");
        }
        public static bool IsPathDirectory(string path)
        {
            if (path == null) throw new ArgumentNullException("path");
            path = path.Trim();

            if (Directory.Exists(path))
                return true;

            if (File.Exists(path))
                return false;

            // neither file nor directory exists. guess intention

            // if has trailing slash then it's a directory
            if (new[] { "\\", "/" }.Any(x => path.EndsWith(x)))
                return true; // ends with slash

            // if has extension then its a file; directory otherwise
            return string.IsNullOrWhiteSpace(Path.GetExtension(path));
        }
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

