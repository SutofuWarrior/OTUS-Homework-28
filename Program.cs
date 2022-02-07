using System.Diagnostics;

namespace SummatorApp
{
    public class Program
    {
        public static void Main()
        {
            TestAll(1_000_000);
            TestAll(10_000_000);
            TestAll(100_000_000);

            Console.ReadLine();
        }

        private static void TestAll(int count)
        {
            Console.WriteLine();
            Console.WriteLine($"Тест для {count} элементов");

            var mas = GetIntArray(count);

            var watcher = new Stopwatch();
            long sum;

            watcher.Start();
            sum = SumSimple(mas);
            watcher.Stop();
            Console.WriteLine($"Обычное = {sum} за {watcher.ElapsedMilliseconds} мс.");

            watcher.Reset();

            watcher.Start();
            sum = SumByThreads(mas);
            watcher.Stop();
            Console.WriteLine($"Параллельное Threads = {sum} за {watcher.ElapsedMilliseconds} мс.");

            watcher.Reset();

            watcher.Start();
            sum = SumByPLINQ(mas);
            watcher.Stop();
            Console.WriteLine($"PLINQ = {sum} за {watcher.ElapsedMilliseconds} мс.");

            Console.WriteLine(new string('*', Console.WindowWidth));
        }

        private static int[] GetIntArray(int length)
        {
            var mas = new int[length];

            for (int i = 0; i < length; i++)
                mas[i] = i;

            return mas;
        }

        private static long SumSimple(int[] mas)
        {
            long sum = 0;

            for (int i = 0; i < mas.Length; i++)
                sum += mas[i];

            return sum;
        }

        private static long SumByThreads(int[] mas)
        {
            int threadCount = Environment.ProcessorCount / 2;
            int elementsPerThread = mas.Length / 4;
            var lstThreadStates = new List<ThreadState>();

            for (int i = 0; i < threadCount; i++)
                lstThreadStates.Add(new ThreadState(mas, i * elementsPerThread, i * elementsPerThread + elementsPerThread));

            lstThreadStates[^1].EndIndex = mas.Length;

            for (int i = 0; i < threadCount; i++)
                new Thread((p) => SumThread(p)).Start(lstThreadStates[i]);

            var waits = lstThreadStates.Select(t => t.WaitHandle).ToArray();

            WaitHandle.WaitAll(waits);

            long sum = 0;

            for (int i = 0; i < lstThreadStates.Count; i++)
                sum += lstThreadStates[i].Result;

            return sum;
        }

        private static void SumThread(object param)
        {
            ThreadState p = (ThreadState)param;

            long sum = 0;

            for (int i = p.StartIndex; i < p.EndIndex; i++)
                sum += p.Elements[i];

            p.Result = sum;
            p.WaitHandle.Set();
        }

        private static long SumByPLINQ(int[] mas)
        {
            return mas.Select(x => (long)x).AsParallel().Sum();
        }
    }

    public class ThreadState
    {
        public int[] Elements;
        public int StartIndex;
        public int EndIndex;

        public long Result = 0;

        public EventWaitHandle WaitHandle = new ManualResetEvent(false);

        public ThreadState(int[] elements, int startIndex, int endIndex)
        {
            Elements = elements;
            StartIndex = startIndex;
            EndIndex = endIndex;
        }
    }
}