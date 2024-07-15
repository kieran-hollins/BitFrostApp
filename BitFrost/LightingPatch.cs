using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace BitFrost
{
    public sealed class LightingPatch
    {
        private readonly object _lock = new();
        private static  Dictionary<(int x, int y), LED> patch = new();
        private static Dictionary<int, (int x, int y)> dmxAddressMap = new();
        public delegate void LEDUpdateHandler(byte[] dmxData);
        public event LEDUpdateHandler? OnLEDUpdate;
        public bool IsAvailable;

        private LightingPatch()
        {
            //patch = new Dictionary<(int x, int y), LED> ();
            //dmxAddressMap = new Dictionary<int, (int x, int y)> ();
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
            var coordinates = (x, y);
            var startDMXAddress = led.StartDMXAddress;

            // Throw exception if LED already exists in this location
            if (patch.ContainsKey(coordinates))
            {
                throw new ArgumentException($"LED already existing at coordinates ({x}, {y}).");
            }

            // Each fixture channel requires a DMX address but each channel shares the same location
            for (int i = startDMXAddress; i < startDMXAddress + led.LEDProfile.Channels; i++)
            {
                if (dmxAddressMap.ContainsKey(i))
                {
                    throw new ArgumentException($"DMX address {i} is already in use.");
                }

                dmxAddressMap.Add(i, coordinates);
            }

            // Add fixture to patch at location (x, y)
            patch.Add(coordinates, led);

        }

        public void RemoveLED(int x, int y)
        {
            var coordinates = (x, y);

            // Throw exception if no LED exists at this location
            if (!patch.ContainsKey(coordinates))
            {
                throw new ArgumentException($"No LED at coordinates ({x}, {y}).");
            }

            // GetStartDMXChannel() is a helper function, which will retrieve the start address for the fixture at location (x, y)
            int startDMXAddress = GetStartDMXChannel(x, y);
            var led = patch[coordinates];

            // Remove each channel from the dmx address map 
            for (int i = startDMXAddress; i < startDMXAddress + led.LEDProfile.Channels; i++)
            {
                dmxAddressMap.Remove(i);
            }

            // Remove the fixture from patch at location (x, y)
            patch.Remove(coordinates);         
        }

        public int GetTotalLEDs()
        {
            // Actually returns number of DMX channels
            return dmxAddressMap.Count;
        }

        public void ClearAll()
        {
            patch.Clear();
            dmxAddressMap.Clear();
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
        public void AddRGBLEDLineHorizontal(int x, int y, int startAddress, int quantity)
        {
            if (!IsAvailable)
            {
                return;
            }

            IsAvailable = false;

            int addressIndex = startAddress;

            for (int i = x; i < x + quantity; i++)
            {
                LED led = new(addressIndex, new RGB());

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


        public void AddRGBLEDLineVertical(int x, int y, int startAddress, int quantity)
        {
            if (!IsAvailable)
            {
                return;
            }

            IsAvailable = false;

            int addressIndex = startAddress;

            for (int i = y; i < y + quantity; i++)
            {
                LED led = new(addressIndex, new RGB());

                try
                {
                    AddLED(x, i, led);
                }
                catch (ArgumentException e)
                {
                    Console.WriteLine(e.Message);

                    // Undo any changes made before catching error
                    for (int j = i - 1; j > x; j--)
                    {
                        try
                        {
                            RemoveLED(x, j);
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

            LED led = patch[coordinates];

            if (led != null && led != new LED())
            {
                Debug.WriteLine($"SET: DMX address: {led.StartDMXAddress} Data: {data[0]} {data[1]} {data[2]} ");
                led.LEDProfile.SetDMXData(data);
            } 
            
        }

    }
}
