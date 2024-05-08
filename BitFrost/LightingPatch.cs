using System;
using System.Threading.Tasks;

namespace BitFrost
{
    public sealed class LightingPatch : IPatchHelper
    {
        private readonly object _lock = new();
        private Dictionary<(int x, int y), LED> patch;
        private Dictionary<int, (int x, int y)> dmxAddressMap;
        public delegate void LEDUpdateHandler(byte[] dmxData);
        public event LEDUpdateHandler OnLEDUpdate;

        private LightingPatch()
        {
            patch = new Dictionary<(int x, int y), LED> ();
            dmxAddressMap = new Dictionary<int, (int x, int y)> ();
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

                OnLEDUpdate?.Invoke(GetCurrentDMXData());
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
            }
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
        public string GetLEDLocation(int dmxAddress)
        {
            if (dmxAddressMap.ContainsKey(dmxAddress))
            {
                string response = $"The LED location is ({dmxAddressMap[dmxAddress].x}, {dmxAddressMap[dmxAddress].y})";
                return response;
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
        }

        public byte[] GetCurrentDMXData()
        {
            byte[] dmxData = new byte[512];
            foreach(var place in patch)
            {
                var coordinates = place.Key;
                var led = place.Value;

                int baseAddress = led.StartDMXAddress - 1; // DMX addressing is from 1 but we this will be stored at index 0.
                byte[] ledData = led.LEDProfile.GetDMXData();

                if (baseAddress + ledData.Length <= 512)
                {
                    Array.Copy(ledData, 0, dmxData, baseAddress, ledData.Length);
                }

            }

            OnLEDUpdate?.Invoke(dmxData);

            return dmxData;
        }

    }
}
