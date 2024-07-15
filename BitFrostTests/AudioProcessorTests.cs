using BitFrost;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BitFrostTests
{
    [TestClass]
    public class AudioProcessorTests
    {
        [TestMethod]
        public void Start_RecordingStartsSuccessfully()
        {
            var audioProcessor = new AudioProcessor();
            audioProcessor.Start();

            Assert.IsTrue(audioProcessor.IsRecording);

            audioProcessor.Stop(); // Cleanup
        }

        [TestMethod]
        public void Stop_RecordingStopsSuccessfully()
        {
            var audioProcessor = new AudioProcessor();
            audioProcessor.Start();
            audioProcessor.Stop();

            Assert.IsFalse(audioProcessor.IsRecording);
        }

        [TestMethod]
        public void ProcessAudio_ValidData_ProcessesSuccessfully()
        {
            var audioProcessor = new AudioProcessor();
            double[] audioData = new double[audioProcessor.DataSize];
            for (int i = 0; i < audioData.Length; i++)
            {
                audioData[i] = Math.Sin(2 * Math.PI * i / audioData.Length); // Example sine wave data
            }

            audioProcessor.ProcessAudio(audioData);

            var magnitudeBuffer = audioProcessor.GetMagnitudeBuffer();

            Assert.IsTrue(magnitudeBuffer.All(m => m >= 0));
        }

        [TestMethod]
        public void GetMagnitudeBuffer_ReturnsCorrectData()
        {
            var audioProcessor = new AudioProcessor();
            double[] audioData = new double[audioProcessor.DataSize];
            audioProcessor.ProcessAudio(audioData);

            var magnitudeBuffer = audioProcessor.GetMagnitudeBuffer();

            Assert.IsNotNull(magnitudeBuffer);
            Assert.AreEqual(audioProcessor.DataSize, magnitudeBuffer.Length);
        }

        [TestMethod]
        public void GetFrequencyBuffer_ReturnsCorrectData()
        {
            var audioProcessor = new AudioProcessor();

            var frequencyBuffer = audioProcessor.GetFrequencyBuffer();

            Assert.IsNotNull(frequencyBuffer);
            Assert.AreEqual(audioProcessor.DataSize, frequencyBuffer.Length);
        }

        [TestMethod]
        public void OnDataAvailable_ProcessesAudioData()
        {
            var audioProcessor = new AudioProcessor();
            var waveInEventArgs = new WaveInEventArgs(new byte[audioProcessor.DataSize * 2], audioProcessor.DataSize * 2);

            audioProcessor.Start();
            typeof(AudioProcessor).GetMethod("OnDataAvailable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                                  .Invoke(audioProcessor, new object[] { null, waveInEventArgs });

            var magnitudeBuffer = audioProcessor.GetMagnitudeBuffer();

            Assert.IsTrue(magnitudeBuffer.All(m => m >= 0));
            audioProcessor.Stop(); // Cleanup
        }

        [TestMethod]
        public void ProcessTransformedData_ValidData_ProcessesSuccessfully()
        {
            var audioProcessor = new AudioProcessor();
            var complexData = new Complex[audioProcessor.DataSize];
            for (int i = 0; i < complexData.Length; i++)
            {
                complexData[i] = new Complex(Math.Sin(2 * Math.PI * i / complexData.Length), 0);
            }

            typeof(AudioProcessor).GetMethod("ProcessTransformedData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                                  .Invoke(audioProcessor, new object[] { complexData });

            var magnitudeBuffer = audioProcessor.GetMagnitudeBuffer();

            Assert.IsTrue(magnitudeBuffer.All(m => m >= 0));
        }
    }
}
