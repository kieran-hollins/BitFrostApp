using BitFrost;

namespace BitFrostTests
{
    [TestClass]
    public class LEDPatchTests
    {
        [TestMethod]
        public void CreatePatchWithOneValidLED()
        {
            LightingPatch patch = new();

            int x = 0, y = 0;
            int address = 1;
            RGB type = new();
            LED led = new(address, type);

            patch.AddLED(x, y, led);

            Console.WriteLine($"Looking for LED at DMX address {address}.");
            Console.WriteLine($"Response: DMX address {address} is located at {patch.getLEDLocation(1)}");
        }
    }
}