using OsuMemoryDataProvider;
using OsuMemoryDataProvider.OsuMemoryModels;
using OsuMemoryDataProvider.OsuMemoryModels.Direct;
using System.Diagnostics;
using Buttplug;

namespace CircleBP
{

    internal class Program

    {
        private static StructuredOsuMemoryReader _sreader = StructuredOsuMemoryReader.Instance.GetInstanceForWindowTitleHint("");
        private static ButtplugClient client = new ButtplugClient("CircleBP v1.1 Alpha by Wrench");
        private static ButtplugClientDevice device = null;
        private static string ip = $"ws://{ReadIP()}";

        public static string ReadIP()
        {
            /*
             * Read Previously Configured IP, if it exists.
             * Otherwise, create a new file called ip.config in the working directory,
             * and set the port to 127.0.0.1 (localhost)
             * 
             * The IP file should (i forgot if I actually did this tho) include the port as well?
            */

            string path = Directory.GetCurrentDirectory() + "/ip.config";
            if (File.Exists(path))
            {
                return File.ReadAllText(@"ip.config");
            }
            else
            {
                using (StreamWriter sw = File.CreateText(path))
                    sw.WriteLine("partners.ip.here");
                return "127.0.0.1";
            }
        }
        public static async Task ExecuteVibrate(int i)
        {
            // This is literally refactored from CircleRGB.
            // The strength dict matches the value of <int> i to a double value for the vibration intensity
            // TODO: why the fuck is there no 1.0 strength? Did I remove it or smf?
            var strengthDict = new Dictionary<int, double>()
            {
                {1, .75 },
                {2, .5 },
                {3, .25 },
                {4, 0 }
            };
            device.SendVibrateCmd(strengthDict[i]);
        }

        public static async Task Main()
        {
            var currentScore = new int[4]; // This is now int because why the fuck was it a string array
            Console.WriteLine($"Using IP {ip}");
            
            // Connect to buttplug remote ip.
            // The program will basically crash or not do anything until it does such.
            var connector = new ButtplugWebsocketConnectorOptions(
                new Uri(ip));
            try
            {
                await client.ConnectAsync(connector);
            }
            catch (Exception ex)
            {
                return;
            }
            await client.StartScanningAsync();
            await client.StopScanningAsync();
            device = client.Devices[0];

            Console.WriteLine("CircleBP v1.1 Alpha || By Wrench\n\nPlease Keep This Window Open!!!");

            await Task.Run(async () =>
            {
                Stopwatch stopwatch;
                _sreader.WithTimes = true;
                var baseAddresses = new OsuBaseAddresses();
                bool devicePlay = false; // Keep track of whether it's a restarted level or not (?)
                int curComboValue = 0; // Changed to 16-bit integer for optimization + rename to curComboValue


                while (true)
                {

                    stopwatch = Stopwatch.StartNew();
                    _sreader.TryRead(baseAddresses.GeneralData); // Initialize data reader

                    if (baseAddresses.GeneralData.OsuStatus == OsuMemoryStatus.SongSelect)
                    {
                        // Reinitialize everything on SongSelect screen
                        currentScore = new int[4];
                        curComboValue = 0;

                        // Set vibration intensity to 0.0
                        device.SendVibrateCmd(0.0);
                        if (devicePlay)
                        {
                            devicePlay = false;

                        }
                    }
                    else
                        baseAddresses.SongSelectionScores.Scores.Clear();

                    if (baseAddresses.GeneralData.OsuStatus == OsuMemoryStatus.ResultsScreen)
                    {
                        if (devicePlay)
                        {
                            // Set vibration intensity to 0.0 and reset everything on ResultsScreen as well.
                            device.SendVibrateCmd(0.0);
                            curComboValue = 0;
                            currentScore = new int[4];
                            devicePlay = false;
                        }
                    }


                    if (baseAddresses.GeneralData.OsuStatus == OsuMemoryStatus.Playing)
                    {
                        // This is something I wrote for restarting levels iirc.
                        // osumemoryreader is kinda weird, and this was like a quick patch that worked
                        // very efficiently to fix the issues I had
                        if (!devicePlay)
                        {
                            devicePlay = true;
                            device.SendVibrateCmd(0.0);
                        }
                        
                        // Keep track of our 4 important values lol
                        // Hit 300, 100, 50, and miss change the vibration intensity.
                        _sreader.TryReadProperty(baseAddresses.Player, nameof(Player.Hit300), out var hit300);
                        _sreader.TryReadProperty(baseAddresses.Player, nameof(Player.Hit100), out var hit100);
                        _sreader.TryReadProperty(baseAddresses.Player, nameof(Player.Hit50), out var hit50);
                        _sreader.TryReadProperty(baseAddresses.Player, nameof(Player.HitMiss), out var hitmiss);

                        try
                        {
                            // Optimized to List<int> idk why the fuck this was a string list that's so weird
                            List<int> testList = new List<int>();
                            testList.Add((int)hit300);
                            testList.Add((int)hit100);
                            testList.Add((int)hit50);
                            testList.Add((int)hitmiss);

                            for (int i = 0; i < testList.Count; i++)
                            {
                                if (currentScore[i] != testList[i])
                                {
                                    // Compare the temp currentScore at [i] for the array of hit values
                                    // to the testList of hit values. If new values are there, sync the arrays and
                                    // update the vibrator, unless if it's 0, which we ignore.
                                    currentScore[i] = testList[i];
                                    if (testList[i] != 0)
                                    {
                                        // Right yeah curComboValue was originally for CircleRGB
                                        // (obv by the name, it kept track of the combo color)
                                        if (i != curComboValue)
                                        {
                                            // i is actually an int, and it's a list index,
                                            // so it's like 0-3, so I add 1 for some reason, which
                                            // is like really inefficient, I should probably change it lol

                                            // oh yeah it was bc I think I did something stupid while making this lol
                                            // i should fix it eventually, tbh it's fine for now I think

                                            await ExecuteVibrate(i + 1);

                                            // also yeah like I said, keeps track of the value.
                                            // it was more efficient to just make it a string at this point rather than
                                            // to convert it every time I want to compare shit.
                                            curComboValue = i;
                                        }
                                    }
                                }
                            }
                        }
                        catch
                        {
                            // Also iirc this was another thing that caught restarted levels and resets everything (?)
                            if (devicePlay)
                            {
                                currentScore = new int[4];
                                devicePlay = false;
                                curComboValue = 0;
                                device.SendVibrateCmd(0.0);
                            }
                        }

                    }
                    else
                    {
                        if (devicePlay)
                        {
                            currentScore = new int[4];
                            devicePlay = false;
                            device.SendVibrateCmd(0.0);
                        }
                    }

                    stopwatch.Stop();

                    _sreader.ReadTimes.Clear();
                    await Task.Delay(33); // Only check every 33ms for value changes, so the computer doesn't fucking blow up lol
                }
            });
        }
    }
}