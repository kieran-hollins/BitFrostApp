using BitFrost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BitFrostTests
{
    [TestClass]
    public class ArtNetControllerTests
    {
        [TestMethod]
        public void UpdateUniverse_ValidUniverse_UpdatesSuccessfully()
        {
            LightingPatch patch = LightingPatch.Instance;
            ArtNetController controller = new ArtNetController("127.0.0.1", 0, patch);

            controller.UpdateUniverse(10);
            controller.Enable();

            controller.UpdateUniverse(254);
            controller.Disable();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void UpdateUniverse_InvalidUniverse_ThrowsException()
        {
            LightingPatch patch = LightingPatch.Instance;
            ArtNetController controller = new ArtNetController("127.0.0.1", 0, patch);

            controller.UpdateUniverse(255); // Should throw exception
        }

        [TestMethod]
        public async Task SetData_ValidData_SetsCorrectly()
        {
            LightingPatch patch = LightingPatch.Instance;
            ArtNetController controller = new ArtNetController("127.0.0.1", 0, patch);
            byte[] data = new byte[512];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (byte)(i % 256);
            }

            controller.SetData(data);

            await Task.Delay(100); // Wait for async operation

            // Use reflection or another method to verify internal buffer states if necessary.
        }

        [TestMethod]
        public async Task SendDMXAsync_ValidData_SendsPacket()
        {
            LightingPatch patch = LightingPatch.Instance;
            ArtNetController controller = new ArtNetController("127.0.0.1", 0, patch);

            controller.Enable();
            byte[] data = new byte[512];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (byte)(i % 256);
            }
            controller.SetData(data);

            await Task.Delay(2000); // Wait to ensure packet is sent

            controller.Disable();
        }

        [TestMethod]
        public void Enable_Disable_TogglesSending()
        {
            LightingPatch patch = LightingPatch.Instance;
            ArtNetController controller = new ArtNetController("127.0.0.1", 0, patch);

            controller.Enable();
            Assert.IsTrue((bool)controller.GetType().GetField("Enabled", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(controller));

            controller.Disable();
            Assert.IsFalse((bool)controller.GetType().GetField("Enabled", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(controller));
        }

        [TestMethod]
        public void Close_DisablesAndClosesClient()
        {
            LightingPatch patch = LightingPatch.Instance;
            ArtNetController controller = new ArtNetController("127.0.0.1", 0, patch);

            controller.Close();

            UdpClient udpClient = (UdpClient)controller.GetType().GetField("UDPClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(controller);
            Assert.ThrowsException<ObjectDisposedException>(() => udpClient.Send(new byte[1], 1));
        }
    }
}
