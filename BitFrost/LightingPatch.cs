using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace BitFrost
{
    public sealed class LightingPatch
    {
        private readonly object _lock = new();
        private Dictionary<(int x, int y), LED> patch;
        private Dictionary<int, (int x, int y)> dmxAddressMap;
        public delegate void LEDUpdateHandler(byte[] dmxData);
        public event LEDUpdateHandler? OnLEDUpdate;
        public bool IsAvailable;

        private LightingPatch()
        {
            patch = new Dictionary<(int x, int y), LED> ();
            dmxAddressMap = new Dictionary<int, (int x, int y)> ();
            IsAvailable = true;
        }

        public static LightingPatch Instance { get { return Nested.patchInstance; } }

        private class Nested
        {
            // Explicit static constructor to tell C# compiler
            // not to mark type as beforefieldinit
            static Nested()
            {
            }

            internal static readonly LightingPatch patchInstance = new LightingPatch (); 
        } 

        public Dictionary<(int, int), LED> GetPatch()
        {
            return patch;
        }

        public void AddLED(int x, int y, LED led)
        {
            lock ( _lock )
            {
                var coordinates = (x, y);
                var startDMXAddress = led.StartDMXAddress;

                if (patch.ContainsKey(coordinates))
                {
                    throw new ArgumentException($"LED already existing at coordinates ({x}, {y}).");
                }

                for (int i = startDMXAddress; i < startDMXAddress + led.LEDProfile.Channels; i++)
                {
                    if (dmxAddressMap.ContainsKey(i))
                    {
                        throw new ArgumentException($"DMX address {i} is already in use.");
                    }
                }

                patch.Add(coordinates, led);

                for (int i = startDMXAddress; i < startDMXAddress + led.LEDProfile.Channels; i++)
                {
                    dmxAddressMap.Add(i, coordinates);
                }

                // OnLEDUpdate?.Invoke(GetCurrentDMXData());
            }
        }

        public void RemoveLED(int x, int y)
        {
            lock ( _lock )
            {
                var coordinates = (x, y);

                if (!patch.ContainsKey(coordinates))
                {
                    throw new ArgumentException($"No LED at coordinates ({x}, {y}).");
                }

                int startDMXAddress = GetStartDMXChannel(x, y);
                var led = patch[coordinates];

                for (int i = startDMXAddress; i < startDMXAddress + led.LEDProfile.Channels; i++)
                {
                    dmxAddressMap.Remove(i);
                }

                patch.Remove(coordinates);

                OnLEDUpdate?.Invoke(GetCurrentDMXData());
            }
        }

        public int GetTotalLEDs()
        {
            return dmxAddressMap.Count;
        }

        public void ClearAll()
        {
            lock (_lock)
            {
                patch.Clear();
                dmxAddressMap.Clear();
            }
        }

        // Returns the coordinate of the LED based on the DMX address map
        public string GetLEDLocationString(int dmxAddress)
        {
            if (dmxAddressMap.ContainsKey(dmxAddress))
            {
                string response = $"The LED location is ({dmxAddressMap[dmxAddress].x}, {dmxAddressMap[dmxAddress].y})";
                return response;
            }

            throw new ArgumentException($"DMX address {dmxAddress} not found.");
        }

        public (int, int) GetLEDLocation(int dmxAddress)
        {
            if (dmxAddressMap.ContainsKey(dmxAddress))
            {
                var coordinates = dmxAddressMap[dmxAddress];
                return coordinates;
            }

            throw new ArgumentException($"DMX address {dmxAddress} not found.");
        }

        private int GetStartDMXChannel(int x, int y)
        {
            var coordinates = (x, y);
            var led = patch[coordinates];

            return led.StartDMXAddress;
        }

        // Adds an LED strip from left to right
        public void AddLEDLineHorizontal(int x, int y, int startAddress, int quantity, LEDProfile type)
        {
            if (!IsAvailable)
            {
                return;
            }

            IsAvailable = false;

            int addressIndex = startAddress;

            for (int i = x; i < x + quantity; i++)
            {
                LED led = new(addressIndex, type);

                try
                {
                    AddLED(i, y, led);
                }
                catch (ArgumentException e)
                {
                    Console.WriteLine(e.Message);

                    // Undo any changes made before catching error
                    for (int j = i - 1; j > x; j--)
                    {
                        try
                        {
                            RemoveLED(j, y);
                        }
                        catch
                        {
                            break;
                        }
                    }
                }

                addressIndex += led.LEDProfile.Channels;
            }

            IsAvailable = true;
        }

        public byte[] GetCurrentDMXData()
        {
            //if (!IsAvailable)
            //{
            //    return new byte[512];
            //}
            //IsAvailable = false;

            byte[] dmxData = new byte[512];
            foreach(var place in patch)
            {
                var led = place.Value;

                int baseAddress = led.StartDMXAddress - 1; // DMX addressing is from 1 but this will be stored at index 0.
                byte[] ledData = led.LEDProfile.GetDMXData();

                if (baseAddress + ledData.Length <= 512)
                {
                    Array.Copy(ledData, 0, dmxData, baseAddress, ledData.Length);
                }

            }

            return dmxData;
        }

        public void SendDMX()
        {
            byte[] dmxData = new byte[512];

            foreach (var place in patch)
            {
                var led = place.Value;
                // Debug.WriteLine($"GET: DMX address: {led.StartDMXAddress} Data: {led.LEDProfile.GetDMXData()[0]} {led.LEDProfile.GetDMXData()[1]} {led.LEDProfile.GetDMXData()[2]} ");
                int baseAddress = led.StartDMXAddress - 1; // Adjust for 0-based indexing.

                byte[] ledData = led.LEDProfile.GetDMXData();

                if (baseAddress >= 0 && baseAddress + ledData.Length <= 512)
                {
                    Array.Copy(ledData, 0, dmxData, baseAddress, ledData.Length);
                }
                else
                {
                    Debug.WriteLine($"LED at {place.Key} with DMX address {led.StartDMXAddress} exceeds DMX array bounds.");
                }
            }

            Debug.WriteLine("Invoking DMX Trigger");
            OnLEDUpdate?.Invoke(dmxData);
        }

        public void SetDMXValue(int x, int y, byte[] data)
        {
            var coordinates = (x, y);

            if (!patch.ContainsKey(coordinates))
            {
                return;
            }

            if (data.Length > 3)
            {
                return;
            }

            var led = patch[coordinates];
            Debug.WriteLine($"SET: DMX address: {led.StartDMXAddress} Data: {data[0]} {data[1]} {data[2]} ");
            led.LEDProfile.SetDMXData(data);


            // Testing First Element in patch
            //var testLed = patch[(0, 0)];
            //byte[] testData = testLed.LEDProfile.GetDMXData();
            //Debug.WriteLine($"GET - First Fixture Channels: {testData[0]} {testData[1]} {testData[2]}");
        }

    }
}
