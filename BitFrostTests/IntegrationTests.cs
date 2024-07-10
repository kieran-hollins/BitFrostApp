using BitFrost;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BitFrostTests
{
    [TestClass]
    public class IntegrationTests
    {
        [TestMethod]
        public void FXGenerator_ApplyMovementEffect_TriggerShader()
        {
            var fxGenerator = FXGenerator.Instance;
            fxGenerator.ApplyMovementEffect("red");

            var patch = LightingPatch.Instance;
            byte[] currentData = patch.GetCurrentDMXData();

            // Validate that the data has been processed by the shader
            for (int i = 0; i < patch.GetTotalLEDs(); i += 3)
            {
                Assert.AreEqual(255, currentData[i]);     // Red
                Assert.AreEqual(0, currentData[i + 1]);   // Green
                Assert.AreEqual(0, currentData[i + 2]);   // Blue
            }
        }

        [TestMethod]
        public void FXGenerator_StartAverageColour_AppliesShaderEffect()
        {
            var fxGenerator = FXGenerator.Instance;

            // Mocking the AudioProcessor to simulate audio data
            var mockAudioProcessor = new Mock<AudioProcessor>();

            fxGenerator.AudioProcessor = mockAudioProcessor.Object;
            fxGenerator.ApplyMovementEffect("average");

            var patch = LightingPatch.Instance;
            byte[] currentData = patch.GetCurrentDMXData();

            // Validate that the data has been processed by the shader
            Assert.IsNotNull(currentData);
        }

        [TestMethod]
        public void ArtNetController_SendDMX_PacketsSent()
        {
            var patch = LightingPatch.Instance;
            var controller = new ArtNetController("127.0.0.1", 0, patch);
            IPEndPoint endpoint = new(IPAddress.Parse("127.0.0.1"), 6454);
            UdpClient receiver = new UdpClient(endpoint);
            byte[] currentData = patch.GetCurrentDMXData();


            controller.Enable();
            byte[] data = new byte[512];

            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (byte)(i % 256);
            }

            controller.SetData(data);

            byte[] received = receiver.Receive(ref endpoint);

            // Wait to ensure packets are sent
            System.Threading.Thread.Sleep(2000);

            controller.Disable();

            // Validate that the DMX data was processed
            CollectionAssert.AreEqual(data, received[18..]); //Account for Art-Net header
        }

        [TestMethod]
        public void AudioProcessor_ProcessAudio_DataProcessed()
        {
            var audioProcessor = new AudioProcessor();
            double[] audioData = new double[audioProcessor.DataSize];
            for (int i = 0; i < audioData.Length; i++)
            {
                audioData[i] = Math.Sin(2 * Math.PI * i / audioData.Length); // Example sine wave data
            }

            audioProcessor.ProcessAudio(audioData);

            var magnitudeBuffer = audioProcessor.GetMagnitudeBuffer();
            var frequencyBuffer = audioProcessor.GetFrequencyBuffer();

            Assert.IsTrue(magnitudeBuffer.Length > 0);
            Assert.IsTrue(frequencyBuffer.Length > 0);
        }

        [TestMethod]
        public void Shaders_RunAllShaders_ValidateOutput()
        {
            var fxGenerator = FXGenerator.Instance;
            string[] effects = new string[] { "red", "average", "truchet", "kaleidoscope", "kaleidoscope-audio", "level-meter", "spectral-test", "sound-eclipse", "warm-white" };

            foreach (var effect in effects)
            {
                fxGenerator.ApplyMovementEffect(effect);
                var patch = LightingPatch.Instance;
                byte[] currentData = patch.GetCurrentDMXData();

                Assert.IsNotNull(currentData);
                Assert.IsTrue(currentData.Length > 0);
            }
        }
    }
}
