using BitFrost;
using System.Net;

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
            var response = patch.GetLEDLocation(1);
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

            patch.AddRGBLEDLineHorizontal(x, y, address, 10);

            Console.WriteLine("Expecting DMX address 1 located at (0, 0)");
            Console.WriteLine($"DMX address located at {patch.GetLEDLocation(address)}");
            Console.WriteLine("Clearing patch now...");

            patch.ClearAll();

            try
            {
                Console.WriteLine($"Expecting exception when trying to get DMX address {address}");
                var result = patch.GetLEDLocation(address);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AddLED_SameLocation_ThrowsException()
        {
            LightingPatch patch = LightingPatch.Instance;
            patch.ClearAll();

            int x = 0, y = 0;
            int address = 1;
            RGB type = new();
            LED led1 = new(address, type);
            LED led2 = new(address, type);

            patch.AddLED(x, y, led1);
            patch.AddLED(x, y, led2); // Should throw exception
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AddLED_SameDMXAddress_ThrowsException()
        {
            LightingPatch patch = LightingPatch.Instance;
            patch.ClearAll();

            int x1 = 0, y1 = 0;
            int x2 = 1, y2 = 0;
            int address = 1;
            RGB type = new();
            LED led1 = new(address, type);
            LED led2 = new(address, type);

            patch.AddLED(x1, y1, led1);
            patch.AddLED(x2, y2, led2); // Should throw exception
        }

        [TestMethod]
        public void RemoveLED_ValidLocation_RemovesLED()
        {
            LightingPatch patch = LightingPatch.Instance;
            patch.ClearAll();

            int x = 0, y = 0;
            int address = 1;
            RGB type = new();
            LED led = new(address, type);

            patch.AddLED(x, y, led);
            patch.RemoveLED(x, y);

            try
            {
                patch.GetLEDLocation(address); // Should throw exception
                Assert.Fail("Expected ArgumentException not thrown");
            }
            catch (ArgumentException) { }
        }

        [TestMethod]
        public void ClearAll_ClearsAllLEDs()
        {
            LightingPatch patch = LightingPatch.Instance;
            patch.ClearAll();

            int x = 0, y = 0;
            int address = 1;
            RGB type = new();
            LED led = new(address, type);

            patch.AddLED(x, y, led);
            patch.ClearAll();

            Assert.AreEqual(0, patch.GetTotalLEDs());
        }

        [TestMethod]
        public void AddRGBLEDLineHorizontal_AddsCorrectNumberOfLEDs()
        {
            LightingPatch patch = LightingPatch.Instance;
            patch.ClearAll();

            int x = 0, y = 0;
            int startAddress = 1;
            int quantity = 5;

            patch.AddRGBLEDLineHorizontal(x, y, startAddress, quantity);

            Assert.AreEqual(quantity * new RGB().Channels, patch.GetTotalLEDs());
        }

    }
}