using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LeakSort
{
	class LeakInput
	{
		private StreamReader streamReader = null;
		private readonly LeakSaver leakSaver;

		public string BasePath { get; }

        public LeakInput(string path, LeakSaver leakSaver)
		{
			BasePath = path;
			this.leakSaver = leakSaver;
		}


		private void Open() => streamReader = new StreamReader(new FileStream(BasePath,
				FileMode.Open, FileAccess.Read, FileShare.None,
				bufferSize: 4096, useAsync: true));

		public async Task<InputProgress> SortAllAsync(CancellationToken cancellationToken)
		{
			if(streamReader == null)
            {
				Open();
            }

            while (!streamReader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
				string line = streamReader.ReadLine();
				await leakSaver.SaveLine(line);
            }
			return new InputProgress(streamReader.BaseStream);
		}

		public long GetPosition() => streamReader.BaseStream.Position;
		public long GetLength() => streamReader.BaseStream.Length;
	}
}
