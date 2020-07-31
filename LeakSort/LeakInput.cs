using System;
using System.IO;
using System.Linq;
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

        public LeakInput(string path, LeakSaver leakSaver, LeakReader lr) : this(path, leakSaver)
        {
            //hack to get around no use of async await
            Task t = Task.Run(async () =>
            {
                int jumpSize = 1000 * 1000 * 100;// 100 mb
                int maxNumberOfDecs = 10;
                bool isFirstLine = true;
                long lastJmpPos = 0;
                while (true)
                {
                    Console.WriteLine("checking at byte {0}", lastJmpPos);
                    string line = await streamReader.ReadLineAsync();
                    if (await lr.HasResult(line)) //line is present jump forward
                    {
                        lastJmpPos += jumpSize - 1000;
                        streamReader.BaseStream.Position = lastJmpPos; //jump less because we will read a partial line
                        streamReader.DiscardBufferedData();
                        await streamReader.ReadLineAsync();
                    }
                    else
                    {
                        //if the first line is not found no point in looking
                        if (isFirstLine) { streamReader.BaseStream.Position = 0; streamReader.DiscardBufferedData(); return; }

                        //not found jump back decrease jump size
                        lastJmpPos -= jumpSize + 1000;
                        lastJmpPos = lastJmpPos <= 0 ? 0 : lastJmpPos;
                        streamReader.BaseStream.Position = lastJmpPos; //jump back further than needed because we will read a partial line
                        jumpSize /= 2;
                        streamReader.DiscardBufferedData();
                        await streamReader.ReadLineAsync();
                        maxNumberOfDecs--;
                        if (maxNumberOfDecs == 0)
                        {
                            break;
                        }
                    }
                    isFirstLine = false;
                }

                while (true)
                {
                    string line = await streamReader.ReadLineAsync();
                    if (!(await lr.HasResult(line)))
                    {
                        await leakSaver.SaveLine(line);
                        break;
                    }
                }
                Console.WriteLine("Skipped aprox. {0}", streamReader.BaseStream.Position);
            });
            t.Wait();
        }

        public async Task<InputProgress> SortAllAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && !streamReader.EndOfStream)
            {
                string line = streamReader.ReadLine();
                await leakSaver.SaveLine(line, cancellationToken);
            }
            return new InputProgress(streamReader.BaseStream);
        }

        public long GetPosition() => streamReader.BaseStream.Position;
        public long GetLength() => streamReader.BaseStream.Length;
    }
}
