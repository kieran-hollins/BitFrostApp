using BitFrost;
using TerraFX.Interop.Windows;

namespace BitFrostTests
{
    [TestClass]
    public class IntegrationTesting
    {
        [TestMethod]
        public void AddDMXData()
        {

            // Setup
            ArtNetController Controller = new("127.0.0.1", 0, LightingPatch.Instance);
            Controller.Enable();

            LightingPatch Patch = LightingPatch.Instance;
            Patch.ClearAll();
            Patch.AddRGBLEDLineHorizontal(0, 0, 1, 30);

            FXGenerator generator = FXGenerator.Instance;

            byte[] dmxData = new byte[512];
            Random random = new Random();

            for (int i = 0; i < dmxData.Length; i++)
            {
                dmxData[i] = (byte)random.Next(255);
            }

            generator.UpdatePatch(dmxData, 90);
        }
    }
}


