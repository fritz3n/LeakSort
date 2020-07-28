using System;
using System.Threading;

namespace LeakSort
{
    class Program
    {
        static LeakInput li;
        static CancellationTokenSource tsc;
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            tsc = new CancellationTokenSource();
            Console.CancelKeyPress += Console_CancelKeyPress;
            InputProgress prog;
            using (LeakSaver ls = new LeakSaver(3, @"D:\leaks2"))
            {
                li = new LeakInput(@"D:\dropbox", ls);
                prog = await li.SortAllAsync(tsc.Token);

            }

            if (prog.IsDone)
            {
                LeakReader lr = new LeakReader(3, @"D:\leaks");
                System.Collections.Generic.IEnumerable<string> res = await lr.Lookup("ric");
            }
            else
            {
                Console.WriteLine("Progress: {0}%\t Done: {1}", prog.Percentage *100, prog.Completed);
                Console.Read();
            }
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            tsc.Cancel();
            Console.WriteLine("Stopping");
        }
    }
}
