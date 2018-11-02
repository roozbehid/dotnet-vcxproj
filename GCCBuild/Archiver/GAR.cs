/**
 * CCTask
 * 
 * Copyright 2018 Roozbeh Gh <roozbeh@gmail.com>
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
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace CCTask.Linkers
{
    public sealed class GAR : IArchiver
    {
        public GAR(string pathToAr, string preARApp)
        {
            this.pathToAr = pathToAr;
            this.preARApp = preARApp;
        }

        public bool Archive(IEnumerable<string> objectFiles, string outputFile, string flags)
        {
            if (!string.IsNullOrEmpty(preARApp))
            {
                objectFiles = objectFiles.Select(x => x = Utilities.ConvertWinPathToWSL(x));
                outputFile = Utilities.ConvertWinPathToWSL(outputFile);
            }
            else
                if (!Directory.Exists(Path.GetDirectoryName(outputFile)))
                    Directory.CreateDirectory(Path.GetDirectoryName(outputFile));

            var linkerArguments = string.Format("rcs \"{1}\" {0} {2} ", objectFiles.Select(x => "\"" + x + "\"").Aggregate((x, y) => x + " " + y), outputFile, flags);
            var runWrapper = new RunWrapper(pathToAr, linkerArguments, preARApp);
            Logger.Instance.LogMessage("{0} {1}", pathToAr, Path.GetFileName(outputFile));
            

#if DEBUG
            Logger.Instance.LogMessage(linkerArguments);
#endif
            return runWrapper.RunArchiver();
        }

        private readonly string pathToAr;
        private readonly string preARApp;
    }
}

