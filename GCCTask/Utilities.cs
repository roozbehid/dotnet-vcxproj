using System;
using System.IO;
using System.Diagnostics;
using System.Security.Cryptography;

namespace CCTask
{
	internal static class Utilities
	{
		public static bool RunAndGetOutput(string path, string options, out string output)
		{
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

