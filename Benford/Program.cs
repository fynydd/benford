using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace Benford
{
    class Program
    {
        static async Task<double> Benford(int z)
        {
            if (z <= 0 || z > 9)
            {
                // Ignore anything other than 1-9
                return await Task.FromResult(0);
            }

            else
            { 
                // Calculate Benford's % for a given value from 1-9 (z)
                return await Task.FromResult(Math.Log10((double)1 + (double)1 / (double)z));
            }
        }

        static async Task<StringBuilder> GenerateOutput(Package package)
        {
            StringBuilder output = new StringBuilder();
            var devs = new List<decimal>();
            var passed = false;
            decimal largestVariance = 0;

            output.Append("Digit   Benford [%]   Observed [%]   Deviation\r\n");
            output.Append("=====   ===========   ============   =========\r\n");

            for (int x = 0; x < 9; x++)
            {
                decimal temp = (decimal)(package.Data[x]/package.Total); // Calc % of total for x
                decimal ben = (decimal) await Benford(x+1); // Calc Benford's value % for x
                decimal deviation = ben - temp;

                // GenerateOutput output that also includes standard deviation for Benford's value versus observed value
                var _output = (string.Format("{0:0}", (x + 1)) + "       " + string.Format("{0:00.00}", ben * 100) + "         " + string.Format("{0:00.00}", temp * 100) + "          " + string.Format("{0:0.000000}", deviation));

                devs.Add(Math.Abs(deviation));

                if (Math.Abs(deviation) > Math.Abs(largestVariance))
                {
                    largestVariance = deviation;
                }

                output.Append(_output + "\r\n");
            }

            if (Math.Abs(largestVariance) < (decimal)0.02)
            {
                if (devs.Average() < (decimal)0.007)
                {
                    passed = true;
                }
            }

            output.Append("\r\n");
            output.Append("LARGEST VARIANCE:                    " + string.Format("{0:0.000000}", largestVariance) + "\r\n");
            output.Append("AVERAGE VARIANCE (ABS):              " + string.Format("{0:0.000000}", devs.Average()) + "\r\n");
            output.Append("RESULT:                              " + (passed ? "PASSED" : "FAILED") + "\r\n");

            output.Append("\r\n");

            return output;
        }

        static async Task<Package> ProcessImageFile(string file)
        {
            Package package = new Package(); // Use package so data can be passed to methods by ref
            Bitmap img = new Bitmap(file);

            // Iterate each row of pixels
            for (int i = 0; i < img.Width; i++)
            {
                // Iterate each pixel in the row
                for (int j = 0; j < img.Height; j++)
                {
                    Color pixel = img.GetPixel(i, j);

                    // Convert pixel data into 32-bit RGBA value
                    var _string = ((uint)((pixel.R > 0 ? pixel.R : 1) * (pixel.G > 0 ? pixel.G : 1) * (pixel.B > 0 ? pixel.B : 1) * (pixel.A > 0 ? pixel.A : 1))).ToString();

                    // Get the first digit of the value
                    var val = _string.Substring(0, 1);

                    //if (file.Contains("santa"))
                    //    await Console.Out.WriteLineAsync(_string);

                    // Increment the count for each first digit value (1-9)
                    switch (val)
                    {
                        case "1": package.Data[0]++; break;
                        case "2": package.Data[1]++; break;
                        case "3": package.Data[2]++; break;
                        case "4": package.Data[3]++; break;
                        case "5": package.Data[4]++; break;
                        case "6": package.Data[5]++; break;
                        case "7": package.Data[6]++; break;
                        case "8": package.Data[7]++; break;
                        case "9": package.Data[8]++; break;
                    }

                    // Count total pixels evaluated
                    package.Total++;
                }
            }

            return await Task.FromResult(package);
        }

        static async Task Main(string[] args)
        {
            var pathPrefix = "Benford";
            var depthCount = 1;

            while (Directory.Exists(pathPrefix) == false && depthCount < 10)
            {
                depthCount++;

                var paths = new string[depthCount];
                for (int x = 0; x < depthCount - 1; x++) paths[x] = "..";
                paths[depthCount - 1] = "Benford";
                pathPrefix = Path.Combine(paths);
            }

            if (Directory.Exists(pathPrefix))
            {
                StringBuilder output = new StringBuilder();
                StringBuilder output2 = new StringBuilder();
                var benford = new double[9];

                ConcurrentQueue<Thread> threads = new ConcurrentQueue<Thread>();
                int threadsRunning = 0;
                int maxThreadCount = 8;
                int sleepMs = 50;
                int counter = 0;

                await Console.Out.WriteLineAsync();
                await Console.Out.WriteLineAsync("BENFORD ANALYSIS RUNNING...");
                await Console.Out.WriteLineAsync();

                #region Analyze List

                Thread t2 = new Thread (async () => {

                    var list = File.ReadAllLines(Path.Combine(pathPrefix, "list.txt"));
                    var package = new Package();
                    var _output = new StringBuilder();

                    _output.Append("Analyzing list.txt (" + string.Format("{0:0,000}", list.Length) + " items)...\r\n---------------------------------------------------------------------------\r\n");

                    foreach (var item in list.OrderBy(i => i))
                    {
                        var val = item.Substring(0, 1);

                        switch (val)
                        {
                            case "1": package.Data[0]++; break;
                            case "2": package.Data[1]++; break;
                            case "3": package.Data[2]++; break;
                            case "4": package.Data[3]++; break;
                            case "5": package.Data[4]++; break;
                            case "6": package.Data[5]++; break;
                            case "7": package.Data[6]++; break;
                            case "8": package.Data[7]++; break;
                            case "9": package.Data[8]++; break;
                        }

                        package.Total++;
                    }

                    _output.Append(await GenerateOutput(package));

                    output2.Append(_output);

                    await Console.Out.WriteLineAsync(_output);
                })
                {
                    IsBackground = true
                };
                t2.Start();

                threads.Enqueue(t2);

                #region Wait for list processing thread to complete

                do
                {
                    threadsRunning = 0;

                    foreach (var thread in threads)
                    {
                        if (thread.IsAlive)
                        {
                            threadsRunning++;
                        }
                    }

                    System.Threading.Thread.Sleep(sleepMs);

                } while (threadsRunning > 0);

                #endregion

                #endregion

                #region Analyze Images

                var jpegFiles = Directory.GetFiles(Path.Combine(pathPrefix, "images"), "*.jpg");
                var tiffFiles = Directory.GetFiles(Path.Combine(pathPrefix, "images"), "*.tiff");
                var files = jpegFiles.Concat(tiffFiles);

                foreach (var file in files.OrderBy(f => f))
                {
                    #region If too many threads are running, wait here

                    do
                    {
                        threadsRunning = 0;

                        foreach (var thread in threads)
                        {
                            if (thread.IsAlive)
                            {
                                threadsRunning++;
                            }
                        }

                        System.Threading.Thread.Sleep(sleepMs);

                    } while (threadsRunning >= maxThreadCount);

                    #endregion

                    #region Add a thread for the current file

                    Thread t = new Thread (async () => {

                        var package = await ProcessImageFile(file);
                        var _file = file + " (" + string.Format("{0:0,000}", package.Total) + " pixels)";
                        var _output = new StringBuilder();

                        counter++;

                        _output.Append("Image " + counter + " of " + files.Count() + ": " + _file.Split(Path.DirectorySeparatorChar).Last() + "...\r\n---------------------------------------------------------------------------\r\n");
                        _output.Append(await GenerateOutput(package));
                        output.Append(_output);

                        await Console.Out.WriteLineAsync(_output);
                    })
                    {
                        IsBackground = true
                    };
                    t.Start();

                    threads.Enqueue(t);

                    #endregion

                    #region Wait here if too many threads are running

                    do
                    {
                        threadsRunning = 0;

                        foreach (var thread in threads)
                        {
                            if (thread.IsAlive)
                            {
                                threadsRunning++;
                            }
                        }

                        System.Threading.Thread.Sleep(sleepMs);

                    } while (threadsRunning >= maxThreadCount);

                    #endregion
                }

                #endregion

                #region Wait for all threads to complete

                do
                {
                    threadsRunning = 0;

                    foreach (var thread in threads)
                    {
                        if (thread.IsAlive)
                        {
                            threadsRunning++;
                        }
                    }

                    System.Threading.Thread.Sleep(sleepMs);

                } while (threadsRunning > 0);

                #endregion

                // Append image results to list.txt results
                output2.Append(output);

                // Write results to disk
                File.WriteAllText(Path.Combine(pathPrefix, "output.txt"), output2.ToString());

                await Console.Out.WriteLineAsync("BENFORD ANALYSIS COMPLETE");
                await Console.Out.WriteLineAsync("The file 'output.txt' contains these results.");
                await Console.Out.WriteLineAsync();
            }

            else
            {
                await Console.Out.WriteLineAsync();
                await Console.Out.WriteLineAsync("BENFORD ANALYSIS FAILED");
                await Console.Out.WriteLineAsync("Could not find the project directory. Run from Visual Studio or cd into the 'Benford' folder and use 'dotnet run'.");
                await Console.Out.WriteLineAsync();
            }
        }
    }

    class Package
    {
        public Package()
        {
            Clear();
        }

        public double[] Data { get; set; }

        public double Total { get; set; } = 0;

        public void Clear()
        {
            Total = 0;
            Data = new double[9];
        }
    }
}
