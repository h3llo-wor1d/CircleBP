using OsuMemoryDataProvider;
using OsuMemoryDataProvider.OsuMemoryModels;
using OsuMemoryDataProvider.OsuMemoryModels.Direct;
using System.Diagnostics;
using Buttplug;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.IO;
using System;
using System.Timers;

namespace CircleBP
{
    public class EnvelopeGenerator
    {
        
    }

    internal class Program

    {
        private static StructuredOsuMemoryReader _sreader = StructuredOsuMemoryReader.Instance.GetInstanceForWindowTitleHint("");
        private static ButtplugClient client = new ButtplugClient("CircleBP v1.0 (PRIVATE BETA!!!) by Willow");
        private static ButtplugClientDevice device = null;
        private static string ip = $"ws://{ReadIP()}";

        private static System.Timers.Timer aTimer;

        public double envValue; // The actual value of the envelope ATM.
        public int tickRate = 60;
        public int currentTime = 0;
        public double decay = .5;
        public int runTime = 0;

        // Envelope Generator Tests
        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            if (currentTime < runTime)
            {
                currentTime++;
                envValue = (double)currentTime / (double)runTime;
                device.SendVibrateCmd(envValue);
            }
            else
            {
                aTimer.Enabled = false;
            }
        }

        public void Run()
        {
            envValue = 0;
            currentTime = 0;
            runTime = (int)(decay * 60);
            aTimer = new System.Timers.Timer(10);
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
        }

        public static string ReadIP()
        {
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

            var currentScore = new string[4];
            Console.WriteLine($"Using IP {ip}");
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

            Console.WriteLine("CircleBP v1.0 (PRIVATE BETA) || By Willow \n\nPlease Keep This Window Open!!!");

            await Task.Run(async () =>
            {
                Stopwatch stopwatch;
                _sreader.WithTimes = true;
                var baseAddresses = new OsuBaseAddresses();
                bool devicePlay = false;
                string curComboColor = "";


                while (true)
                {

                    stopwatch = Stopwatch.StartNew();
                    _sreader.TryRead(baseAddresses.GeneralData); // Initialize data reader

                    if (baseAddresses.GeneralData.OsuStatus == OsuMemoryStatus.SongSelect)
                    {
                        currentScore = new string[4];
                        curComboColor = "";
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
                            device.SendVibrateCmd(0.0);
                            currentScore = new string[4];
                            curComboColor = "";
                            devicePlay = false;
                        }
                    }


                    if (baseAddresses.GeneralData.OsuStatus == OsuMemoryStatus.Playing)
                    {
                        if (!devicePlay)
                        {
                            devicePlay = true;
                            device.SendVibrateCmd(0.0);
                        }

                        _sreader.TryReadProperty(baseAddresses.Player, nameof(Player.Hit300), out var hit300);
                        _sreader.TryReadProperty(baseAddresses.Player, nameof(Player.Hit100), out var hit100);
                        _sreader.TryReadProperty(baseAddresses.Player, nameof(Player.Hit50), out var hit50);
                        _sreader.TryReadProperty(baseAddresses.Player, nameof(Player.HitMiss), out var hitmiss);

                        try
                        {
                            List<String> testList = new List<String>();
                            testList.Add(hit300.ToString());
                            testList.Add(hit100.ToString());
                            testList.Add(hit50.ToString());
                            testList.Add(hitmiss.ToString());

                            for (int i = 0; i < testList.Count; i++)
                            {
                                if (currentScore[i] != testList[i])
                                {
                                    currentScore[i] = testList[i];
                                    if (testList[i] != "0")
                                    {
                                        if (i.ToString() != curComboColor)
                                        {
                                            await ExecuteVibrate(i + 1);
                                            curComboColor = i.ToString();
                                        }
                                    }
                                }
                            }
                        }
                        catch
                        {
                            if (devicePlay)
                            {
                                currentScore = new string[4];
                                devicePlay = false;
                                curComboColor = "";
                                device.SendVibrateCmd(0.0);
                            }
                        }

                    }
                    else
                    {
                        if (devicePlay)
                        {
                            currentScore = new string[4];
                            devicePlay = false;
                            device.SendVibrateCmd(0.0);
                        }
                    }

                    stopwatch.Stop();

                    _sreader.ReadTimes.Clear();
                    await Task.Delay(33);
                }
            });
        }
    }
}