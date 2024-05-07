using BitFrost;

namespace BitFrostTests
{
    [TestClass]
    public class LEDPatchTests
    {
        [TestMethod]
        public void CreatePatchWithOneValidLED()
        {
            LightingPatch patch = LightingPatch.Instance;
            patch.ClearAll();

            int x = 0, y = 0;
            int address = 1;
            RGB type = new();
            LED led = new(address, type);

            patch.AddLED(x, y, led);

            Console.WriteLine($"Looking for LED at DMX address {address}.");
            Console.WriteLine($"Response: DMX address {address} is located at {patch.GetLEDLocation(1)}");
        }

        [TestMethod]
        public void AttemptToCreateTwoPatches()
        {
            LightingPatch patch = LightingPatch.Instance;
            patch.ClearAll();

            int x = 0, y = 0;
            int address = 1;
            RGB type = new();
            LED led = new(address, type);

            patch.AddLED(x, y, led);
            string response = patch.GetLEDLocation(1);
            Console.WriteLine(response);

            LightingPatch patch2 = LightingPatch.Instance;

            response = patch2.GetLEDLocation(1);
            Console.WriteLine(response);
        }

        [TestMethod]
        public void ClearPatchTest()
        {
            LightingPatch patch = LightingPatch.Instance;
            patch.ClearAll();

            int x = 0, y = 0;
            int address = 1;
            RGB type = new();

            patch.AddLEDLineHorizontal(x, y, address, 10, type);

            Console.WriteLine("Expecting DMX address 1 located at (0, 0)");
            Console.WriteLine($"DMX address located at {patch.GetLEDLocation(address)}");
            Console.WriteLine("Clearing patch now...");
            
            patch.ClearAll();

            try
            {
                Console.WriteLine($"Expecting exception when trying to get DMX address {address}");
                string result = patch.GetLEDLocation(address);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

        }
    }
}