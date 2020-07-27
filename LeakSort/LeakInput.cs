using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace LeakSort
{
	class LeakInput
	{
		private FileStream fileStream = null;
		private readonly LeakSaver leakSaver;

		public string Path { get; }

		public LeakInput(string path, LeakSaver leakSaver)
		{
			Path = path;
			this.leakSaver = leakSaver;
		}


		public void Open() => fileStream = new FileStream(Path,
				FileMode.Open, FileAccess.Read, FileShare.None,
				bufferSize: 4096, useAsync: true);

		public async Task SortAllAsync()
		{

		}
	}
}
