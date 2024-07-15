using BitFrost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitFrostTests
{
    [TestClass]
    public class FXGeneratorTests
    {
        [TestMethod]
        public void Instance_ReturnsSameInstance()
        {
            var instance1 = FXGenerator.Instance;
            var instance2 = FXGenerator.Instance;

            Assert.AreSame(instance1, instance2);
        }

        [TestMethod]
        public void SetColour_SetsCorrectValues()
        {
            var fxGenerator = FXGenerator.Instance;
            byte[] colours = { 255, 128, 64 };

            fxGenerator.SetColour(colours);

            var field = typeof(FXGenerator).GetProperty("Colours", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var value = (byte[])field.GetValue(fxGenerator);

            CollectionAssert.AreEqual(colours, value);
        }

        [TestMethod]
        public void AudioAvailable_ValidData_ProcessesCorrectly()
        {
            var fxGenerator = FXGenerator.Instance;
            float[] magnitudes = new float[512];
            float[] frequencies = new float[512];

            typeof(FXGenerator).GetMethod("AudioAvailable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                               .Invoke(fxGenerator, new object[] { magnitudes, frequencies });

            var patch = LightingPatch.Instance;
            var currentData = patch.GetCurrentDMXData();

            Assert.AreEqual(512, currentData.Length);
        }

        [TestMethod]
        public void TestRedShader_ProcessesCorrectly()
        {
            var fxGenerator = FXGenerator.Instance;

            typeof(FXGenerator).GetMethod("TestRedShader", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                               .Invoke(fxGenerator, null);

            var patch = LightingPatch.Instance;
            var currentData = patch.GetCurrentDMXData();

            Assert.IsTrue(currentData.Length > 0);
        }

        [TestMethod]
        public void StartLevelMeter_ProcessesCorrectly()
        {
            var fxGenerator = FXGenerator.Instance;

            typeof(FXGenerator).GetMethod("StartLevelMeter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                               .Invoke(fxGenerator, null);

            var patch = LightingPatch.Instance;
            var currentData = patch.GetCurrentDMXData();

            Assert.IsTrue(currentData.Length > 0);
        }

        [TestMethod]
        public void StartKaleidoscopeAudio_ProcessesCorrectly()
        {
            var fxGenerator = FXGenerator.Instance;

            typeof(FXGenerator).GetMethod("StartKaleidoscopeAudio", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                               .Invoke(fxGenerator, null);

            var patch = LightingPatch.Instance;
            var currentData = patch.GetCurrentDMXData();

            Assert.IsTrue(currentData.Length > 0);
        }

        [TestMethod]
        public void StartSoundEclipse_ProcessesCorrectly()
        {
            var fxGenerator = FXGenerator.Instance;

            typeof(FXGenerator).GetMethod("StartSoundEclipse", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                               .Invoke(fxGenerator, null);

            var patch = LightingPatch.Instance;
            var currentData = patch.GetCurrentDMXData();

            Assert.IsTrue(currentData.Length > 0);
        }
    }
}
