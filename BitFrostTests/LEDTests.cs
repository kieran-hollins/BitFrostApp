using BitFrost;

namespace BitFrostTests
{
    [TestClass]
    public class LEDTests
    {
        [TestMethod]
        public void CheckRGBLEDChannels()
        {
            RGB rgbType = new RGB();
            LED led = new(1, rgbType);

            Console.WriteLine("Number of channels expected: 3");
            Console.WriteLine($"Number of channels found: {led.LEDProfile.Channels}");
        }
    }
}
