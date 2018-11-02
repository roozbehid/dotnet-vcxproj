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
        const string mntprefix = @"/mnt/";
        public static string ConvertWinPathToWSL(string path)
        {
            try
            {
                StringBuilder FullPath = new StringBuilder(Path.GetFullPath(path));
                FullPath[0] = (FullPath[0].ToString().ToLower())[0];
                return mntprefix + FullPath.ToString().Replace(@":\", @"/").Replace(@"\", @"/");
            }
            catch
            {
                Console.WriteLine("!! ----- error in GCCBuld NTPath -> WSL");
                return path;
            }
        }

        public static string ConvertWSLPathToWin(string path)
        {
            try
            {
                if ((path.Length < 8) || (path.IndexOf(mntprefix) != 0))
                    return path;
                var fileUri = new Uri((path.Substring(mntprefix.Length, path.Length - mntprefix.Length)[0] + ":\\" + path.Substring(mntprefix.Length + 2, path.Length - (mntprefix.Length + 2))).Replace("/", "\\"));
                var referenceUri = new Uri(Directory.GetCurrentDirectory() + "\\");
                return referenceUri.MakeRelativeUri(fileUri).ToString().Replace(@"/", @"\");
            }
            catch
            {
                Console.WriteLine("!! ----- error in GCCBuld WSL -> NTPath");
                return path;
            }
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
            Logger.Instance.LogCommandLine($"{path} {options}");
            process.Start();
			process.WaitForExit();
			output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
			return process.ExitCode == 0;
		}
	}
}

