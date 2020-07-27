using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace LeakSort
{
	class LeakInput
	{
		private StreamReader streamReader = null;
		private readonly LeakSaver leakSaver;

		public string Path { get; }

        public LeakInput(string path, LeakSaver leakSaver)
		{
			Path = path;
			this.leakSaver = leakSaver;
		}


		private void Open() => streamReader = new StreamReader(new FileStream(Path,
				FileMode.Open, FileAccess.Read, FileShare.None,
				bufferSize: 4096, useAsync: true));

		public async Task SortAllAsync()
		{
			if(streamReader == null)
            {
				Open();
            }

            while (!streamReader.EndOfStream)
            {
				string line = await streamReader.ReadLineAsync();
				await leakSaver.SaveLine(line);
            }
		}
	}
}
