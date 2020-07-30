using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LeakSort
{
	class LeakInput
	{
		private StreamReader streamReader;
        private readonly LeakSaver leakSaver;

		public string BasePath { get; }

        public LeakInput(string path, LeakSaver leakSaver)
		{
			BasePath = path;
			this.leakSaver = leakSaver;
			streamReader = new StreamReader(new FileStream(BasePath,
				FileMode.Open, FileAccess.Read, FileShare.None,
				bufferSize: 4096, useAsync: true));
		}

        public LeakInput(string path, LeakSaver leakSaver, long skip) : this(path, leakSaver)
        {
			if (skip < 0) { throw new ArgumentException("skip was negative."); }
			if (skip != 0)
			{
				//Set the position before the one supplied so that we can be sure that don't skip some lines (needed because StreamReaders are buffered)
				streamReader.BaseStream.Position = skip - 4096;
				streamReader.ReadLine(); //read a line so the next one is not cut off.
				//may result in some lines getting read twice.
			}
        }

		public async Task<InputProgress> SortAllAsync(CancellationToken cancellationToken)
		{
            while (true)
            {
				if(cancellationToken.IsCancellationRequested || streamReader.EndOfStream)
                {
					break;
                }
				string line = streamReader.ReadLine();
				await leakSaver.SaveLine(line);
            }
			return new InputProgress(streamReader.BaseStream);
		}

		public long GetPosition() => streamReader.BaseStream.Position;
		public long GetLength() => streamReader.BaseStream.Length;
	}
}
