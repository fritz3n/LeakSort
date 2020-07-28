using System.IO;

namespace LeakSort
{
    public class InputProgress
    {
        public readonly long Completed;
        public readonly long Total;
        public float Percentage { get => Completed / Total; }
        public bool IsDone { get => Completed == Total; }
        public InputProgress(Stream stream) : this(stream.Position, stream.Length) { }
        public InputProgress(long completed, long total)
        {
            Completed = completed;
            Total = total;
        }
    }
}