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
        private Dictionary<int, (int x, int y)> IDMap;
        private int IDCounter;
        public delegate void LEDUpdateHandler(byte[] dmxData, int universe);
        public event LEDUpdateHandler? OnLEDUpdate;
        public bool IsAvailable;

        private LightingPatch()
        {
            patch = new Dictionary<(int x, int y), LED> ();
            IDMap = new Dictionary<int, (int x, int y)> ();
            IDCounter = 0;
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



        public void AddLED(int x, int y, int DMXAddress, LEDProfile profile)
        {
            lock ( _lock )
            {
                var coordinates = (x, y);

                if (DMXAddress > 512)
                {
                    DMXAddress %= 512;
                }
                if (DMXAddress < 1)
                {
                    DMXAddress = 1;
                }

                int startDMXAddress = DMXAddress;

                if (patch.ContainsKey(coordinates))
                {
                    throw new ArgumentException($"LED already existing at coordinates ({x}, {y}).");
                }

                for (int i = startDMXAddress; i < startDMXAddress + profile.Channels; i++)
                {
                    if (IDMap.ContainsKey(i))
                    {
                        throw new ArgumentException($"ID {i} is already in use.");
                    }
                }

                IDCounter++;

                try
                {
                    patch.Add(coordinates, new LED(DMXAddress, profile, IDCounter));
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
                

                IDMap.Add(IDCounter, coordinates);
                

                // OnLEDUpdate?.Invoke(GetCurrentDMXData());
            }
        }



        public void RemoveLED(int x, int y)
        {
            var coordinates = (x, y);

            if (!patch.ContainsKey(coordinates))
            {
                throw new ArgumentException($"No LED at coordinates ({x}, {y}).");
            }

            // GetStartDMXChannel() is a helper function, which will retrieve the start address for the fixture at location (x, y)
            int startDMXAddress = GetStartDMXChannel(x, y);
            var led = patch[coordinates];

            // Remove each channel from the address map 
            for (int i = startDMXAddress; i < startDMXAddress + led.LEDProfile.Channels; i++)
            {
                IDMap.Remove(i);
            }

            patch.Remove(coordinates);
        }



        public int GetTotalLEDs()
        {
            return IDMap.Count;
        }



        public void ClearAll()
        {
            lock (_lock)
            {
                patch.Clear();
                IDMap.Clear();
            }
        }



        // Returns the coordinate of the LED based on the DMX address map
        public string GetLEDLocationString(int dmxAddress)
        {
            if (IDMap.ContainsKey(dmxAddress))
            {
                string response = $"The LED location is ({IDMap[dmxAddress].x}, {IDMap[dmxAddress].y})";
                return response;
            }

            throw new ArgumentException($"DMX address {dmxAddress} not found.");
        }



        public (int, int) GetLEDLocation(int id)
        {
            if (IDMap.ContainsKey(id))
            {
                var coordinates = IDMap[id];
                return coordinates;
            }

            throw new ArgumentException($"ID: {id} not found.");
        }



        public int GetStartDMXChannel(int x, int y)
        {
            var coordinates = (x, y);
            var led = patch[coordinates];

            return led.StartDMXAddress;
        }



        public int GetIDFromLocation(int x, int y)
        {
            var coordinates = (x, y);
            var led = patch[coordinates];
            
            return led.ID;
        }



        public LED GetLEDByLocation(int x, int y)
        {
            var coordinates = (x, y);
            try
            {
                var led = patch[coordinates];
                return led;
            }
            catch
            {
                throw new ArgumentException($"No LED found at location ({x}, {y})");
            }

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
                try
                {
                    AddLED(i, y, addressIndex, type);
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

                addressIndex += type.Channels;
            }

            IsAvailable = true;
        }



        // Adds an LED strip from left to right
        public void AddRGBLEDLineHorizontal(int x, int y, int startAddress, int quantity)
        {
            if (!IsAvailable)
            {
                return;
            }

            LEDProfile profile = new RGB();

            IsAvailable = false;

            int addressIndex = startAddress;

            for (int i = x; i < x + quantity; i++)
            {
                try
                {
                    AddLED(i, y, addressIndex, profile);
                }
                catch (ArgumentException e)
                {
                    Debug.WriteLine(e.Message);

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

                addressIndex += profile.Channels;
            }

            IsAvailable = true;
        }


        public void AddRGBLEDLineVertical(int x, int y, int startAddress, int quantity, LEDProfile profile)
        {
            if (!IsAvailable)
            {
                return;
            }

            IsAvailable = false;

            int addressIndex = startAddress;

            for (int i = y; i < y + quantity; i++)
            {
                try
                {
                    AddLED(x, i, addressIndex, profile);
                }
                catch (ArgumentException e)
                {
                    Debug.WriteLine(e.Message);

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

                addressIndex += profile.Channels;
            }

            IsAvailable = true;
        }



        public List<byte[]> GetCurrentDMXData()
        {
            List<byte[]> universes = new List<byte[]>
            {
                new byte[512]
            };

            foreach(var place in patch)
            {
                var led = place.Value;
                int baseAddress = led.StartDMXAddress - 1; // Adjust for 0-based indexing.
                int universe = 0;

                int uc = led.ID;
                while (uc > 512)
                {
                    universe++;
                    while (universes.Count < universe + 1)
                    {
                        universes.Add(new byte[512]);
                    }
                    uc -= 512;
                }

                byte[] ledData = led.LEDProfile.GetDMXData();

                if (baseAddress >= 0 && baseAddress + ledData.Length <= 512)
                {
                    Array.Copy(ledData, 0, universes[universe], baseAddress, ledData.Length);
                }
            }

            return universes;
        }

        public void SendDMX()
        {
            List<byte[]> packets = new List<byte[]>
            {
                new byte[512]
            };

            foreach (var place in patch)
            {
                var led = place.Value;
                int baseAddress = led.StartDMXAddress - 1; // Adjust for 0-based indexing.
                int universe = 0;

                int uc = led.ID;
                while (uc > 512)
                {
                    universe++;
                    while (packets.Count < universe + 1)
                    {
                        packets.Add(new byte[512]);
                    }
                    uc -= 512;
                }

                byte[] ledData = led.LEDProfile.GetDMXData();

                if (baseAddress >= 0 && baseAddress + ledData.Length <= 512)
                {
                    Array.Copy(ledData, 0, packets[universe], baseAddress, ledData.Length);
                }
                else
                {
                    Debug.WriteLine($"LED at {place.Key} with DMX address {led.StartDMXAddress} and universe {universe} exceeds DMX array bounds.");
                }
            }

            int counter = 0;
            foreach (var packet in packets)
            {
                Debug.WriteLine($"Invoking DMX Trigger. Universe: {counter}");
                OnLEDUpdate?.Invoke(packet, counter);
                counter++;
            }
            
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
        }

    }
}
