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
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace CCTask.Compilers
{
	public sealed class GCC : ICompiler
	{
		public GCC(string pathToGcc)
		{
			this.pathToGcc = pathToGcc;
		}

		public bool Compile(string source, string output, string flags)
		{
			// let's get all dependencies
			string gccOutput;
			var mmargs = string.Format("{1} -MM \"{0}\"", source, flags);
			#if DEBUG
			Logger.Instance.LogMessage("MM: {0} ({1})", Path.GetFileName(source), mmargs);
			#endif
			if(!Utilities.RunAndGetOutput(pathToGcc, mmargs, out gccOutput))
			{
				Logger.Instance.LogError(gccOutput);
				return false;
			}
			var dependencies = ParseGccMmOutput(gccOutput).Union(new [] { source });

            if (Path.GetDirectoryName(output) != "")
                Directory.CreateDirectory(Path.GetDirectoryName(output));

			var ccargs = string.Format("\"{0}\" {2} -c -o \"{1}\"", source, output, flags);
			Logger.Instance.LogMessage("CC: {0}", Path.GetFileName(source));
			#if DEBUG
			Logger.Instance.LogMessage("output: {0} flags: {1}", output, ccargs);
			#endif

			var runWrapper = new RunWrapper(pathToGcc, ccargs);
			return runWrapper.Run();
		}

		private static IEnumerable<string> ParseGccMmOutput(string gccOutput)
		{
			var dependency = new StringBuilder();
			for(var i = 0; i < gccOutput.Length; i++)
			{
				var finished = false;
				if(gccOutput[i] == '\\')
				{
					i++;
					if(gccOutput[i] == ' ')
					{
						dependency.Append(' ');
						continue;
					}
					else
					{
						// new line
						finished = true;
					}
				}
				else if(char.IsControl(gccOutput[i]))
				{
					continue;
				}
				else if(gccOutput[i] == ' ')
				{
					finished = true;
				}
				else if(gccOutput[i] == ':')
				{
					dependency = new StringBuilder();
				}
				else
				{
					dependency.Append(gccOutput[i]);
				}
				if(finished)
				{
					if(dependency.Length > 0)
					{
						yield return dependency.ToString();
					}
					dependency = new StringBuilder();
				}
			}
			if(dependency.Length > 0)
			{
				yield return dependency.ToString();
			}
		}

		private readonly string pathToGcc;
	}
}

