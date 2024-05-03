using Microsoft.AspNetCore.Components.Forms;

namespace BitFrost
{
    public class LightingPatch
    {
        private Dictionary<(int x, int y), LED> patch;
        private Dictionary<int, (int x, int y)> dmxAddressMap;

        public LightingPatch()
        {
            patch = new Dictionary<(int x, int y), LED> ();
            dmxAddressMap = new Dictionary<int, (int x, int y)> ();
        }

        public void AddLED(int x, int y, LED led)
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
        }

        public void RemoveLED(int x, int y)
        {
            var coordinates = (x, y);

            if (!patch.ContainsKey(coordinates))
            {
                throw new ArgumentException($"No LED at coordinates ({x}, {y})");
            }

            int startDMXAddress = GetStartDMXChannel(x, y);
            var led = patch[coordinates];

            for (int i = startDMXAddress; i < startDMXAddress + led.LEDProfile.Channels; i++)
            {
                dmxAddressMap.Remove(i);
            }

            patch.Remove(coordinates); 
        }

        private int GetStartDMXChannel(int x, int y)
        {
            var coordinates = (x, y);
            var led = patch[coordinates];

            return led.StartDMXAddress;
        }
    }
}
