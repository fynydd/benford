using System;
using System.Collections.Concurrent;
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

            output.Append("Digit   Benford [%]   Observed [%]   Deviation\r\n");
            output.Append("=====   ===========   ============   =========\r\n");

            for (int x = 0; x < 9; x++)
            {
                double temp = (double)(package.Data[x]/package.Total); // Calc % of total for x
                double ben = await Benford(x+1); // Calc Benford's value % for x

                // GenerateOutput output that also includes standard deviation for Benford's value versus observed value
                var _output = (string.Format("{0:0}", (x + 1)) + "       " + string.Format("{0:00.00}", ben * 100) + "         " + string.Format("{0:00.00}", temp * 100) + "          " + string.Format("{0:0.0000}", (ben - temp)));

                output.Append(_output + "\r\n");
            }

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
            StringBuilder output = new StringBuilder();
            StringBuilder output2 = new StringBuilder();
            var jpegFiles = Directory.GetFiles("images", "*.jpg");
            var tiffFiles = Directory.GetFiles("images", "*.tiff");
            var benford = new double[9];
            var files = jpegFiles.Concat(tiffFiles);

            ConcurrentQueue<Thread> threads = new ConcurrentQueue<Thread>();
            int threadsRunning = 0;
            int maxThreadCount = 16;
            int sleepMs = 50;

            await Console.Out.WriteLineAsync();

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
                    var _file = file + " (" + string.Format("{0:0,000}", package.Total) + " pixels):";
                    var _output = new StringBuilder();

                    _output.Append(_file + "\r\n---------------------------------------------------------------------------\r\n");
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

            #region Queue "list.txt" processing thread

            Thread t2 = new Thread (async () => {

                var list = File.ReadAllLines("list.txt");
                var package = new Package();
                var _output = new StringBuilder();

                _output.Append("list.txt\r\n---------------------------------------------------------------------------\r\n");

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

            // Append list.txt processing to the end of the results
            output.Append(output2);

            // Write results to disk
            File.WriteAllText("output.txt", output.ToString());

            await Task.FromResult<object>(null); // this prevents compiler warnings when no await operators are used
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
