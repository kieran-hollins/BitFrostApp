using BitFrost;

namespace BitFrostTests
{
    [TestClass]
    public class LEDTests
    {
        [TestMethod]
        public void CreateRGBLED_ValidAddress_CreatesLED()
        {
            int address = 1;
            LED led = LED.CreateRGBLED(address);

            Assert.AreEqual(address, led.StartDMXAddress);
            Assert.AreEqual(3, led.LEDProfile.Channels);
            Assert.IsInstanceOfType(led.LEDProfile, typeof(RGB));
        }

        [TestMethod]
        public void CreateGRBLED_ValidAddress_CreatesLED()
        {
            int address = 1;
            LED led = LED.CreateGRBLED(address);

            Assert.AreEqual(address, led.StartDMXAddress);
            Assert.AreEqual(3, led.LEDProfile.Channels);
            Assert.IsInstanceOfType(led.LEDProfile, typeof(GRB));
        }

        [TestMethod]
        public void CreateRGBWLED_ValidAddress_CreatesLED()
        {
            int address = 1;
            LED led = LED.CreateRGBWLED(address);

            Assert.AreEqual(address, led.StartDMXAddress);
            Assert.AreEqual(4, led.LEDProfile.Channels);
            Assert.IsInstanceOfType(led.LEDProfile, typeof(RGBW));
        }

        [TestMethod]
        public void RGB_SetColours_SetsCorrectValues()
        {
            RGB rgb = new RGB();
            rgb.SetColours(255, 128, 64);

            Assert.AreEqual(255, rgb.Red);
            Assert.AreEqual(128, rgb.Green);
            Assert.AreEqual(64, rgb.Blue);
        }

        [TestMethod]
        public void RGBW_SetDMXData_SetsCorrectValues()
        {
            RGBW rgbw = new RGBW();
            byte[] data = { 255, 128, 64, 32 };

            rgbw.SetDMXData(data);

            Assert.AreEqual(255, rgbw.Red);
            Assert.AreEqual(128, rgbw.Green);
            Assert.AreEqual(64, rgbw.Blue);
            Assert.AreEqual(32, rgbw.White);
        }

        [TestMethod]
        public void GRB_GetDMXData_ReturnsCorrectValues()
        {
            GRB grb = new GRB();           
            grb.ConvertColour(255, 128, 64);

            byte[] expectedData = { 128, 255, 64 };
            CollectionAssert.AreEqual(expectedData, grb.GetDMXData());
        }
    }
}
