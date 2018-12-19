using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using System.IO;

namespace GCCBuild.Cleaner
{
    public class CppCleanTask : Task
    {
        [Required]
        public ITaskItem[] FoldersToClean { get; set; }
        [Required]
        public string FilesExcludedFromClean { get; set; }
        [Required]
        public bool DoDelete { get; set; }
        [Required]
        public string FilePatternsToDeleteOnClean { get; set; }

        [Output]
        public ITaskItem[] DeletedFiles { get; set; }


        public override bool Execute()
        {
            var deletedFiles = new List<string>();

            if (!DoDelete)
                return true;
            if (String.IsNullOrWhiteSpace(FilePatternsToDeleteOnClean))
                return true;

            if (String.IsNullOrEmpty(FilesExcludedFromClean))
                FilesExcludedFromClean = "";

            foreach (var folder in FoldersToClean)
            {
                foreach (string file in Directory.GetFiles(folder.ItemSpec, "*.*", SearchOption.AllDirectories).Where(s => FilePatternsToDeleteOnClean.Contains(Path.GetExtension(s).ToLower())))
                {
                    if (FilesExcludedFromClean.IndexOf(file) > 0)
                        continue;
                    try
                    {
                        var fullname = Path.Combine(folder.ItemSpec, file);
                        File.Delete(fullname);
                        deletedFiles.Add(fullname);
                    }
                    catch ( Exception ex)
                    {
                        Logger.Instance.LogMessage($"Error while deleting file {Path.Combine(folder.ItemSpec, file)} {ex}");
                    }
                }
            }

            DeletedFiles = deletedFiles.Any() ? deletedFiles.Select(x => new TaskItem(x)).ToArray() : new TaskItem[0];
            return true;
        }
    }
}
