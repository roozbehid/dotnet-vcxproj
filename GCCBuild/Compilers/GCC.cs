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
    public interface ICompiler
    {
        bool Compile(string source, string output, string flags, string flags_dep);
    }
    
    public sealed class GCC : ICompiler
	{
		public GCC(string pathToGcc, ShellAppConversion shellApp, string projectFile)
		{
			this.pathToGcc = pathToGcc;
            this.shellApp = shellApp;
            this.projectFile = projectFile;
            if (shellApp.convertpath)
                this.projectFile = shellApp.ConvertWinPathToWSL(projectFile);

        }

        public bool Compile(string source, string output, string flags, string flags_dep)
        {
            // let's get all dependencies
            string gccOutput;

            if (Path.GetDirectoryName(output) != "")
                Directory.CreateDirectory(Path.GetDirectoryName(output));

            // This part is to get all dependencies and so know what files to recompile!
            bool needRecompile = true;
            if (!String.IsNullOrEmpty(flags_dep))
                try
                {
                    if (!Utilities.RunAndGetOutput(pathToGcc, flags_dep, out gccOutput, shellApp))
                    {
                        if (gccOutput == "FATAL")
                            return false;
                        Logger.Instance.LogDecide(gccOutput, shellApp);
                        ///return false;
                    }
                    var dependencies = ParseGccMmOutput(gccOutput).Union(new[] { source, projectFile });

                    if (File.Exists(output))
                    {
                        needRecompile = false;
                        FileInfo objInfo = new FileInfo(output);
                        foreach (var dep in dependencies)
                        {
                            string depfile = dep;
                            if (String.IsNullOrWhiteSpace(depfile))
                                continue;
 
                            if ((depfile.IndexOfAny(Path.GetInvalidPathChars()) >= 0) || (Path.GetFileName(depfile).IndexOfAny(Path.GetInvalidFileNameChars()) >= 0))
                                continue;
                            if (shellApp.convertpath)
                                depfile = shellApp.ConvertWSLPathToWin(dep);//here use original!

                            FileInfo fi = new FileInfo(depfile);
                            if (fi.Exists == false || fi.Attributes == FileAttributes.Directory || fi.Attributes == FileAttributes.Device)
                                continue;
                            if (fi.LastWriteTime > objInfo.LastWriteTime)
                            {
                                needRecompile = true;
                                break;
                            }

                        }
                    }
                }
                catch
                {
                    needRecompile = true;
                    Logger.Instance.LogError("Internal error while trying to get dependencies from gcc", null);
                }

            bool runCompileResult = false;
            if (needRecompile)
            {
                var runWrapper = new RunWrapper(pathToGcc, flags, shellApp);
                runCompileResult = runWrapper.RunCompiler();
            }
            else
                runCompileResult = true;

            return runCompileResult;

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
        private ShellAppConversion shellApp;
        private readonly string projectFile;

    }
}

