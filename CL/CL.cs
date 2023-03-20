using Cloo;
using GeorgianNumbers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CL
{
    public static class CL
    {
#if DEBUG
        private static bool debugInfoPrinted = false;
#endif
        private const int _maxSize = 2_000_000_000;

        public static Tuple<int, long> FindBetween(long start, long end, int threads, int memory)
        {
            var max = Tuple.Create<int, long>(0, 0);

            if (ComputePlatform.Platforms.Count == 0)
            {
                Console.WriteLine("No Platforms found!");
                return max;
            }
            var platform = ComputePlatform.Platforms[0];
#if DEBUG
            if (!debugInfoPrinted)
            {
                debugInfoPrinted = true;
                Console.WriteLine($"{ComputePlatform.Platforms.Count} Platform{(ComputePlatform.Platforms.Count != 1 ? "s" : "")} found:");
                foreach (var p in ComputePlatform.Platforms)
                {
                    Console.WriteLine($"\t[{p.Vendor}] {p.Name} ({p.Version}):");
                    foreach (var d in p.Devices)
                        Console.WriteLine($"\t\t{d.Name} ({d.DriverVersion}-{d.OpenCLCVersionString}): {d.GlobalMemorySize} - {d.LocalMemorySize} - {d.MaxMemoryAllocationSize}");
                }
            }
#endif
            try
            {
                long maxSize = (long)((double)ComputePlatform.Platforms[0].Devices[0].GlobalMemorySize / 4);
                maxSize -= maxSize % (GeoNum.Instance.underCount.Length * 4 + GeoNum.Instance.thousandsCount.Length * 4 + 256 * 1_048_576);
                maxSize = Math.Min(_maxSize, maxSize);
                long size = Math.Min(end - start, maxSize);
                int batchCount = (int)Math.Ceiling((double)(end - start - 1) / size);
                threads = Math.Min(threads, batchCount);
                threads = Math.Max(1, Math.Min(threads, (int)(memory * 1_073_741_824 / size / 4)));
#if DEBUG
                Console.WriteLine("Allocating {0} Bytes for every batch of data!", size * 4);
                Console.WriteLine("Total batches: {0}", batchCount);
                Console.WriteLine("Allocating {0} Bytes of System Memory!", (ulong)size * 4 * (ulong)threads);
#endif
                List<int[]> batches = new List<int[]>(threads);
                for (int i = 0; i < threads; ++i)
                    batches.Add(new int[size]);

                var properties = new ComputeContextPropertyList(platform);
                ComputeContext context = new ComputeContext(platform.Devices, properties, null, IntPtr.Zero);

                using ComputeBuffer<int> dev_under = new ComputeBuffer<int>(context, ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.CopyHostPointer, GeoNum.Instance.underCount);
                using ComputeBuffer<int> dev_thousands = new ComputeBuffer<int>(context, ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.CopyHostPointer, GeoNum.Instance.thousandsCount);
                using ComputeBuffer<int> dev_steps = new ComputeBuffer<int>(context, ComputeMemoryFlags.WriteOnly, size);

                using ComputeProgram program = new ComputeProgram(context, Kernel);
                program.Build(null, null, null, IntPtr.Zero);

                ComputeEventList eventList = new ComputeEventList();

                Task<Tuple<int, long>>[] tasks = new Task<Tuple<int, long>>[threads];
                int taskId = 0;

                using ComputeCommandQueue commands = new ComputeCommandQueue(context, context.Devices[0], ComputeCommandQueueFlags.None);
                while (start < end)
                {
                    using ComputeKernel kernel = program.CreateKernel("findKernel");
                    kernel.SetMemoryArgument(0, dev_under);
                    kernel.SetMemoryArgument(1, dev_thousands);
                    kernel.SetMemoryArgument(2, dev_steps);
                    kernel.SetValueArgument(3, start);
                    kernel.SetValueArgument(4, size);

                    commands.Execute(kernel, null, new long[] { size }, null, eventList);

                    int[] steps = batches[taskId];
                    commands.ReadFromBuffer(dev_steps, ref steps, false, eventList);
#if DEBUG
                    int ti = taskId;
#endif
                    long s = start;
                    tasks[taskId] = Task.Run(() =>
                    {
                        var m = Tuple.Create<int, long>(0, 0);
                        for (int i = 0; i < steps.Length; ++i)
                            if (steps[i] > m.Item1)
                                m = Tuple.Create(steps[i], s + i);
#if DEBUG
                        Console.WriteLine("Task {0} Done!", ti);
#endif
                        return m;
                    });

                    if (++taskId == threads)
                    {
                        Task.WhenAll(tasks);
                        foreach (var m in tasks.Select(t => t.Result))
                            if (m.Item1 > max.Item1 || (m.Item1 == max.Item1 && m.Item2 < max.Item2))
                                max = Tuple.Create(m.Item1, m.Item2);
                        taskId = 0;
                    }

                    start += size;
                    size = (int)Math.Min(end - start, maxSize);
                }
                commands.Finish();
                eventList.DisposeAll();
            } catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }

            return max;
        }

        private const string Kernel =
@"
kernel void findKernel(global read_only int* under,
                global read_only int* thousands,
                global write_only int* steps,
                read_only long start,
                read_only int size)
{
    int i = get_global_id(0);
    if (i < size) {
        steps[i] = 1;
        long number = start + i;
        while (number != 4) {
            if (number < 1000)
            {
                number = under[number];
                steps[i]++;
            }
            else
            {
                int separated[10];
                int j = 0;
                while (number >= 1000)
                {
                    separated[j++] = (int)(number % 1000);
                    number /= 1000;
                }
                separated[j] = (int)(number % 1000);
                int nonZero = 0;
                while (separated[nonZero] == 0) ++nonZero;
                number = 0;
                int l = j;
                if (separated[l] != 1)
                    number += under[separated[l]] + 1;
                if (nonZero == j) {
                    number += thousands[--l];
                    continue;
                }
                else
                    number += thousands[--l] - 1;
                while (l > nonZero)
                {
                    if (separated[l] == 0)
                    {
                        --l;
                        continue;
                    }
                    number += 2;
                    if (separated[l] != 1)
                        number += under[separated[l]] + 1;
                    number += thousands[--l] - 1;
                }
                number += 2;
                if (nonZero == 0)
                    number += under[separated[nonZero]];
                else
                {
                    if (separated[nonZero] != 1)
                        number += under[separated[nonZero]] + 1;
                    number += thousands[nonZero - 1];
                }
                steps[i]++;
            }
        }
    }
}";
    }
}
