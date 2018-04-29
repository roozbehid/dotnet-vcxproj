using System;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;

namespace CCTask
{
	public class EnvironmentTask : Task
	{
		[Output]
		public int PointerSize { get; private set; }

		public override bool Execute()
		{
			PointerSize = Environment.Is64BitOperatingSystem ? 64 : 32;
			return true;
		}

	}
}

