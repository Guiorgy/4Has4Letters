using CommandLine;
using GeorgianNumbers;
using System;
using System.Collections.Generic;
using System.IO;
#if DEBUG
using System.Linq;
#endif
using System.Text;
using System.Threading.Tasks;
using static _4Has4Letters.Cuda;
using static CL.CL;

namespace _4Has4Letters // Even in Georgian!
{
    class Program
    {
        public class Options
        {
            [Option('t', "threads", Required = false, HelpText = "Number of CPU threads")]
            public int threads { get; set; } =  Math.Max(1, Environment.ProcessorCount - 2);

            [Option('g', "gpu", Required = false, HelpText = "Use GPU instead of the CPU")]
            public bool gpu { get; set; } = false;

            [Option('c', "cuda", Required = false, HelpText = "Use CUDA for GPU. Otherwise, OpenCL will be used")]
            public bool cuda { get; set; } = false;

            [Option('b', "blocks", Required = false, HelpText = "The maximum number of CUDA blocks. 100,000 by default")]
            public uint blocks { get; set; } = 100_000;

            [Option('m', "memory", Required = false, HelpText = "The maximum System Memory (GB) should OpenCL use (8 by default)")]
            public int memory { get; set; } = 8;

            [Option('s', "start", Required = false, HelpText = "The start of the range of numbers to test. 0 by default")]
            public long start { get; set; } = 0;

            [Option('e', "end", Required = false, HelpText = "The end of the range of numbers to test. 1,000,000,000 by default")]
            public long end { get; set; } = 1_000_000_000;

            [Option('o', "output", Required = false, HelpText = "The output file. If whitespace, Console will be used instead")]
            public string output { get; set; } = "./out.txt";

            [Option('n', "no-comma", Required = false, HelpText = "Don't use comma separators in Georgian number representation")]
            public bool noComma { get; set; } = false;
            public string separator { get => !noComma ? ", " : " "; }
        }

        static async Task Main(string[] args)
        {
            List<long> sequence = null;

            Options options = null;
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(o => options = o)
                .WithNotParsed(errors =>
            {
#if DEBUG
                Console.WriteLine("{0} errors encountered!", errors.Count());
                foreach (var error in errors)
                    Console.WriteLine("\t{0}", error);
#endif
                return;
            });
            if (options == null)
            {
#if DEBUG
                Console.WriteLine("Options are null!");
#endif
                return;
            }

#if DEBUG
            Console.WriteLine("Options:");
            Console.WriteLine("\tthreads: {0}", options.threads);
            Console.WriteLine("\tgpu: {0}", options.gpu);
            Console.WriteLine("\tcuda: {0}", options.cuda);
            Console.WriteLine("\tblocks: {0}", options.blocks);
            Console.WriteLine("\tstart: {0}", options.start);
            Console.WriteLine("\tend: {0}", options.end);
            Console.WriteLine("\toutput: {0}", options.output);
            Console.WriteLine("\tno-comma: {0}", options.noComma);
#endif

            if (options.end <= options.start)
                options.end = options.start + 1;

            var watch = System.Diagnostics.Stopwatch.StartNew();
            if (options.gpu)
            {
                int separatorCount = options.separator.Length;
                if (options.cuda)
                {
                    Cuda.prepare(GeoNum.Instance.underCount, GeoNum.Instance.thousandsCount, options.blocks);
                    sec max = Cuda.findBetween((ulong)options.start, (ulong)options.end);
                    Cuda.reset();
                    sequence = new List<long>();
                    long next = (long)max.start;
                    while (next != 4)
                    {
                        sequence.Add(next);
                        next = GeoNum.Instance.CountLong(next, separatorCount);
                    }
                    sequence.Add(4);
                }
                else
                {
                    var max = FindBetween(options.start, options.end, options.threads, options.memory);
                    sequence = new List<long>();
                    long next = max.Item2;
                    while (next != 4)
                    {
                        sequence.Add(next);
                        next = GeoNum.Instance.CountLong(next, separatorCount);
                    }
                    sequence.Add(4);
                }
            }
            else
            {
                Task<List<long>>[] tasks = new Task<List<long>>[options.threads];
                for (int c = 0; c < options.threads; ++c)
                {
                    int core = c;
                    tasks[c] = Task.Run(() => FastCheck(options, core));
                }
                List<long>[] sequences = await Task.WhenAll(tasks);
                foreach (List<long> s in sequences)
                    if (sequence == null || sequence.Count < s.Count || (sequence.Count == s.Count && s[0] < sequence[0]))
                        sequence = s;
            }

            watch.Stop();
            if (!string.IsNullOrWhiteSpace(options.output))
            {
                using StreamWriter writer = new StreamWriter(options.output, false, Encoding.Unicode);
                writer.WriteLine($"First longest ({sequence.Count}) sequence over {string.Format("{0:n0}", options.start)} and under {string.Format("{0:n0}", options.end)}:");
                foreach (long n in sequence)
                {
                    string geo = GeoNum.Instance.LongToGeorgian(n, options.separator);
                    writer.WriteLine($"\t{string.Format("{0:n0}", n)}: {geo} ({geo.Length})");
                }
                writer.Write($"Execution Time: {watch.ElapsedMilliseconds} ms");
            }
            else
            {
                Console.WriteLine($"First longest ({sequence.Count}) sequence over {string.Format("{0:n0}", options.start)} and under {string.Format("{0:n0}", options.end)}:");
                foreach (long n in sequence)
                {
                    string geo = GeoNum.Instance.LongToGeorgian(n, options.separator);
                    Console.WriteLine($"\t{string.Format("{0:n0}", n)}: {geo} ({geo.Length})");
                }
            }
            Console.WriteLine($"Execution Time: {watch.ElapsedMilliseconds} ms");
        }

        //private static List<long> NaiveCheck(Options options, int thread)
        //{
        //    List<long> sequence = new List<long>();
        //    for (long i = options.start + thread; i <= options.end; i += options.threads)
        //    {
        //        // Console.WriteLine($"thread {options.thread} starting {i}");
        //        // Console.WriteLine($"Starting {i}");
        //        List<long> visited = new List<long>();
        //        long next = i;
        //        while (!visited.Contains(next))
        //        {
        //            visited.Add(next);
        //            string geo = GeoNum.Instance.LongToGeorgian(next, options.separator);
        //            next = geo.Length; // Console.WriteLine($"\t{next}: {geo} ({next = geo.Length})");
        //        }
        //        if (visited.Count > sequence.Count)
        //        {
        //            sequence.Clear();
        //            sequence.AddRange(visited);
        //        }
        //    }
        //    return sequence;
        //}

        private static List<long> FastCheck(Options options, int thread)
        {
            int separatorCount = options.separator.Length;
            List<long> sequence = new List<long>();
            for (long i = options.start + thread; i <= options.end; i += options.threads)
            {
                // Console.WriteLine($"thread {options.thread} starting {i}");
                // Console.WriteLine($"Starting {i}");
                List<long> visited = new List<long>();
                long next = i;
                // Assuming all sequences end with 4
                while (next != 4)
                // while (!visited.Contains(next))
                {
                    visited.Add(next);
                    next = GeoNum.Instance.CountLong(next, separatorCount);
                    // Console.WriteLine($"\t{next}: {geo} ({next = GeoNum.Instance.CountLong(next, separatorCount);})");
                }
                if (visited.Count + 1 > sequence.Count)
                {
                    sequence.Clear();
                    sequence.AddRange(visited);
                    // Assuming all sequences end with 4
                    sequence.Add(4);
                }
                // Console.WriteLine($"{i}: {sequence.Count}");
            }
            return sequence;
        }
    }
}
