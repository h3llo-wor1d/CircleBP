using Device.Net;
using Microsoft.Extensions.Logging;
using Hid.Net.Windows;
using System.Drawing;

namespace CircleRGB_v2
{
    internal class Simpad
    {
        public static IDevice? instance;
        private static async Task sendBuffer(byte[] buffer)
        {
            await instance.WriteAsync(buffer).ConfigureAwait(false);
        }

        public async Task setMode(string mode)
        {
            var buffer = new byte[65];
            buffer[1] = 0x08;
            buffer[3] = buffer[4] = 0xFF;
            buffer[5] = 0x04;
            switch (mode) {
                case "rainbow":
                    buffer[2] = 0x06;
                    buffer[6] = 0x02;
                    break;
                case "on":
                    buffer[2] = 0x02;
                    buffer[6] = 0x06;
                    break;
                case "reset":
                    buffer[2] = buffer[3] = buffer[4] = 0x00;
                    buffer[5] = buffer[6] = 0x04;
                    break;
            }
            await sendBuffer(buffer);
        }

        public async Task setKeypad(string color)
        {
            Color rgb = ColorTranslator.FromHtml(color);
            var buffer = new byte[65];
            buffer[1] = 0x06; // Key
            buffer[2] = rgb.R; // R
            buffer[3] = rgb.G; // G
            buffer[4] = rgb.B; // B
            buffer[5] = 0x04; // Brightness
            buffer[6] = (byte)(buffer[2] ^ buffer[3] ^ buffer[4] ^ buffer[5]); // Still no idea what this is
            await sendBuffer(buffer);
            buffer[1] = 0x07;
            await sendBuffer(buffer);
        }

        public async void Init()
        {

            var loggerFactory = LoggerFactory.Create((builder) =>
            {
                _ = builder.AddDebug().SetMinimumLevel(LogLevel.Trace);
            });

            //Register the factory for creating Hid devices. 
            var hidFactory =
                new FilterDeviceDefinition(vendorId: 0x8088)
                .CreateWindowsHidDeviceFactory(loggerFactory);

            //Register the factory for creating Usb devices.
            var usbFactory =
                new FilterDeviceDefinition(vendorId: 0x8088)
                .CreateWindowsHidDeviceFactory(loggerFactory);


            var factories = hidFactory.Aggregate(usbFactory);

            //Get connected device definitions
            var deviceDefinitions = (await factories.GetConnectedDeviceDefinitionsAsync().ConfigureAwait(false)).ToList();

            if (deviceDefinitions.Count() == 0)
            {
                return;
            }

            //Get the device from its definition
            instance = await hidFactory.GetDeviceAsync(deviceDefinitions[1]).ConfigureAwait(false);

            //Initialize the device
            await instance.InitializeAsync().ConfigureAwait(false);
        }
    }
}
