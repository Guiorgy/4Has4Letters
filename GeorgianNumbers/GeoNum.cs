using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;

namespace GeorgianNumbers
{
    public class GeoNum
    {
        private static GeoNum instance;
        private static readonly object thislock = new object();

        public static GeoNum Instance
        {
            get
            {
                if (instance == null)
                    lock (thislock)
                        return instance ??= new GeoNum();
                return instance;
            }
        }

        private string ReadResources(string resourceName)
        {
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                resourceName = assembly.GetManifestResourceNames().Single(str => str.EndsWith(resourceName));
                using Stream stream = assembly.GetManifestResourceStream(resourceName);
                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
            catch
            {
                return null;
            }
        }

        private string[] ReadResourceLines(string resourceName)
        {
            try
            {
                return ReadResources(resourceName).Split(
                    new[] { Environment.NewLine },
                    StringSplitOptions.None
                );
            }
            catch
            {
                return new string[0];
            }
        }

        private readonly string[] under = new string[1000];
        private readonly Tuple<string, string>[] thousands = new Tuple<string, string>[]
        {
            Tuple.Create("ათასი", "ათას"),
            Tuple.Create("მილიონი", "მილიონ"),
            Tuple.Create("მილიარდი", "მილიარდ"),
            Tuple.Create("ტრილიონი", "ტრილიონ"),
            Tuple.Create("კვადრილიონი", "კვადრილიონ"),
            Tuple.Create("კვინტილიონი", "კვინტილიონ"),
            Tuple.Create("სექსტილიონი", "სექსტილიონ"),
            Tuple.Create("სეპტილიონი", "სეპტილიონ"),
            Tuple.Create("ოქტილიონი", "ოქტილიონ"),
            Tuple.Create("ნონილიონი", "ნონილიონ"),
            Tuple.Create("დეცილიონი", "დეცილიონ"),
        };

        public readonly int[] underCount;
        public readonly int[] thousandsCount;

        private GeoNum()
        {
            Console.OutputEncoding = Encoding.UTF8;
            try
            {
                under = ReadResourceLines("under1000.txt").Select(s => s.Split(':')[1].Trim()).ToArray();
            }
            catch
            {
                #region under 20
                under[0] = "ნული";
                under[1] = "ერთი";
                under[2] = "ორი";
                under[3] = "სამი";
                under[4] = "ოთხი";
                under[5] = "ხუთი";
                under[6] = "ექვსი";
                under[7] = "შვიდი";
                under[8] = "რვა";
                under[9] = "ცხრა";
                under[10] = "ათი";
                under[11] = "თერთმეტი";
                under[12] = "თორმეტი";
                under[13] = "ცამეტი";
                under[14] = "თოთხმეტი";
                under[15] = "თხუთმეტი";
                under[16] = "თექვსმეტი";
                under[17] = "ჩვიდმეტი";
                under[18] = "თვრამეტი";
                under[19] = "ცხრამეტი";
                under[20] = "ოცი";
                #endregion
                #region x10
                under[40] = "ორმოცი";
                under[60] = "სამოცი";
                under[80] = "ოთხმოცი";
                for (int i = 30; i <= 90; i += 20)
                    under[i] = under[i - 10][..^1] + "და" + under[10];
                #endregion
                #region x100
                under[100] = "ასი";
                for (int i = 200; i <= 900; i += 100)
                    under[i] = (i < 800 ? under[i / 100][..^1] : under[i / 100]) + under[100];
                #endregion
                #region under 100
                for (int i = 1; i <= 19; ++i)
                    under[20 + i] = "ოცდა" + under[i];
                for (int i = 1; i <= 19; ++i)
                    under[40 + i] = "ორმოცდა" + under[i];
                for (int i = 1; i <= 19; ++i)
                    under[60 + i] = "სამოცდა" + under[i];
                for (int i = 1; i <= 19; ++i)
                    under[80 + i] = "ოთხმოცდა" + under[i];
                #endregion
                #region over 100
                for (int i = 1; i <= 9; ++i)
                {
                    string hund = under[i * 100][..^1] + ' ';
                    for (int j = 1; j <= 99; ++j)
                        under[i * 100 + j] = hund + under[j];
                }
                #endregion
            }
            underCount = under.Select(s => s.Length).ToArray();
            thousandsCount = thousands.Select(t => t.Item1.Length).ToArray();
        }

        public string LongToGeorgian(long number, string separator = ", ", bool dropOnes = true)
        {
            if (number < 1000)
            {
                return under[(int)number];
            }
            else
            {
                StringBuilder builder = new StringBuilder();
                int[] separated = SeparateNumber(number);
                int nonZero = 0;
                while (separated[nonZero] == 0) ++nonZero;
                int l = separated.Length - 1;
                if (!dropOnes || separated[l] != 1)
                    builder.Append(under[separated[l]])
                        .Append(' ');
                if (nonZero == separated.Length - 1)
                    return builder.Append(thousands[--l].Item1).ToString();
                else
                    builder.Append(thousands[--l].Item2);
                while (l > nonZero)
                {
                    if (separated[l] == 0)
                    {
                        --l;
                        continue;
                    }
                    builder.Append(separator);
                    if (!dropOnes || separated[l] != 1)
                        builder.Append(under[separated[l]])
                            .Append(' ');
                    builder.Append(thousands[--l].Item2);
                }
                builder.Append(separator);
                if (nonZero == 0)
                    builder.Append(under[separated[nonZero]]);
                else
                {
                    if (separated[nonZero] != 1)
                        builder.Append(under[separated[nonZero]])
                            .Append(' ');
                    builder.Append(thousands[nonZero - 1].Item1);
                }
                return builder.ToString();
            }
        }

        public int CountLong(long number, int separatorCount = 2, bool dropOnes = true)
        {
            if (number < 1000)
            {
                return underCount[number];
            }
            else
            {
                int count = 0;
                int[] separated = SeparateNumber(number);
                int nonZero = 0;
                while (separated[nonZero] == 0) ++nonZero;
                int l = separated.Length - 1;
                if (!dropOnes || separated[l] != 1)
                    count += underCount[separated[l]] + 1;
                if (nonZero == separated.Length - 1)
                    return count + thousandsCount[--l];
                else
                    count += thousandsCount[--l] - 1;
                while (l > nonZero)
                {
                    if (separated[l] == 0)
                    {
                        --l;
                        continue;
                    }
                    count += separatorCount;
                    if (separated[l] != 1)
                        count += underCount[separated[l]] + 1;
                    count += thousandsCount[--l] - 1;
                }
                count += separatorCount;
                if (nonZero == 0)
                    count += underCount[separated[nonZero]];
                else
                {
                    if (separated[nonZero] != 1)
                        count += underCount[separated[nonZero]] + 1;
                    count += thousandsCount[nonZero - 1];
                }
                return count;
            }
        }

        private int[] SeparateNumber(long number)
        {
            List<int> separated = new List<int>();
            while (number >= 1000)
            {
                separated.Add((int)(number % 1000));
                number /= 1000;
            }
            separated.Add((int)(number % 1000));
            return separated.ToArray();
        }

        public string BigIntegerToGeorgian(BigInteger number, string separator = ", ", bool dropOnes = true)
        {
            if (number < 1000)
            {
                return under[(int)number];
            }
            else
            {
                StringBuilder builder = new StringBuilder();
                int[] separated = SeparateNumber(number);
                int nonZero = 0;
                while (separated[nonZero] == 0) ++nonZero;
                int l = separated.Length - 1;
                if (!dropOnes || separated[l] != 1)
                    builder.Append(under[separated[l]])
                        .Append(' ');
                if (nonZero == separated.Length - 1)
                    return builder.Append(thousands[--l].Item1).ToString();
                else
                    builder.Append(thousands[--l].Item2);
                while (l > nonZero)
                {
                    if (separated[l] == 0)
                    {
                        --l;
                        continue;
                    }
                    builder.Append(separator);
                    if (!dropOnes || separated[l] != 1)
                        builder.Append(under[separated[l]])
                            .Append(' ');
                    builder.Append(thousands[--l].Item2);
                }
                builder.Append(separator);
                if (nonZero == 0)
                    builder.Append(under[separated[nonZero]]);
                else
                {
                    if (separated[nonZero] != 1)
                        builder.Append(under[separated[nonZero]])
                            .Append(' ');
                    builder.Append(thousands[nonZero - 1].Item1);
                }
                return builder.ToString();
            }
        }

        private int[] SeparateNumber(BigInteger number)
        {
            List<int> separated = new List<int>();
            while (number >= 1000)
            {
                separated.Add((int)(number % 1000));
                number /= 1000;
            }
            separated.Add((int)(number % 1000));
            return separated.ToArray();
        }
    }
}
