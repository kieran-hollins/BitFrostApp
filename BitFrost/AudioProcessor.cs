using MathNet.Numerics.IntegralTransforms;
using NAudio.Wave;
using System.Diagnostics;

namespace BitFrost
{
    public class AudioProcessor
    {
        public delegate void BeatEventHandler(double frequency, double magnitude);
        public event BeatEventHandler? OnBeatEvent;
        private readonly WaveInEvent waveIn;
        private readonly int sampleRate = 44100;
        private readonly int bufferMs = 100;
        public bool IsRecording = false;

        public AudioProcessor()
        {
            waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(sampleRate, 1),
                BufferMilliseconds = bufferMs,
                DeviceNumber = 0
                
            };
            waveIn.DataAvailable += OnDataAvailable;
        }

        public void Start()
        {
            waveIn.StartRecording();            
            IsRecording = true;
            Debug.WriteLine("Recording Started");
        }

        public void Stop()
        {
            waveIn.StopRecording();
            IsRecording = false;
            Debug.WriteLine("Recording Stopped");
        }

        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            double[] audioData = new double[e.BytesRecorded / 2]; // Audio signal conversion
            
            // Each audio sample is a 16-bit value, so two bytes are processed at once.
            for (int i = 0; i < e.BytesRecorded; i += 2)
            {
                short sample = BitConverter.ToInt16(e.Buffer, i);
                audioData[i / 2] = sample / 32768.0; // Normalises data to range [-1.0, 1.0]
            }

            ProcessAudio(audioData);
        }

        public void ProcessAudio(double[] audioData)
        {
            var complexData = new System.Numerics.Complex[audioData.Length];

            for (int i = 0; i < complexData.Length; i++)
            {
                complexData[i] = new System.Numerics.Complex(audioData[i], 0);
            }

            Fourier.Forward(complexData, FourierOptions.Matlab);

            ProcessTransformedData(complexData);
        }

        private void ProcessTransformedData(System.Numerics.Complex[] transformedData) 
        {
            double threshold = 75.0;
            int N = transformedData.Length;

            for (int i = 0; i < N / 2; i++)
            {
                double magnitude = transformedData[i].Magnitude;
                double frequency = i * sampleRate / N;
                if (magnitude > threshold)
                {
                    OnBeatEvent?.Invoke(frequency, magnitude);
                }
            }
        }

    }
}
