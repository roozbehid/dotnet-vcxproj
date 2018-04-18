using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using CCTask.Linkers;

namespace CCTask
{
	public class CLinkerTask : Task
	{
		[Required]
		public ITaskItem[] ObjectFiles { get; set; }

		public ITaskItem[] Flags { get; set; }

		public ITaskItem[] Libraries { get; set; }

		public string LinkerPath { get; set; }

		[Required]
		public string Output { get; set; }

		public override bool Execute()
		{
			Logger.Instance = new XBuildLogProvider(Log); // TODO: maybe initialise statically; this put in constructor causes NRE 

			if(!ObjectFiles.Any())
			{
				return true;
			}

			var lfiles = new List<string>();
			var ofiles = ObjectFiles.Select(x => x.ItemSpec);
			var flags = (Flags != null && Flags.Any()) ? Flags.Select(x => x.ItemSpec).ToList() : new List<string>();

			if(Libraries != null)
			{
				foreach(var library in Libraries.Select(x => x.ItemSpec))
				{
					if(File.Exists(library))
					{
						var directory = Path.GetDirectoryName(library);
						var fileName = Path.GetFileName(library);

						lfiles.Add(library);
						flags.Add(string.Format(" -L{0} -l:{1}", directory, fileName));
					}
					else
					{
						flags.Add(string.Format("-l{0}", library));
					}
				}
			}

			var joinedFlags = string.Join(" ", flags);
			using(var cache = new FileCacheManager(Path.GetDirectoryName(Output)))
			{
				if(!cache.SourceHasChanged(ofiles.Union(lfiles), joinedFlags) && File.Exists(Output))
				{
					return true;
				}
			}

			// linking
			var linker = new GLD(string.IsNullOrEmpty(LinkerPath) ? DefaultLinker : LinkerPath);
			return linker.Link(ofiles, Output, joinedFlags);
		}

		private const string DefaultLinker = "cc";
	}
}

