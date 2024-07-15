using MathNet.Filtering.FIR;
using MathNet.Numerics.IntegralTransforms;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using NAudio.Wave;
using System.Diagnostics;
using System.Timers;

namespace BitFrost
{
    public class AudioProcessor
    {
        //public delegate void BeatEventHandler(double frequency, double magnitude);
        //public event BeatEventHandler? OnBeatEvent;
        public delegate void AudioBufferEventHandler(float[] MagnitudeBuffer, float[] FrequencyBuffer);
        public event AudioBufferEventHandler? OnAudioBufferEvent;
        private readonly WaveInEvent waveIn;
        private readonly int sampleRate = 44100;
        private readonly int bufferMs = 20;
        public bool IsRecording = false;
        private System.Timers.Timer? SendBuffer;

        public float[] MagnitudeBuffer { get; private set; }
        public float[] FrequencyBuffer { get; private set; }
        public int DataSize { get; private set; }

        private readonly OnlineFirFilter BandPassFilter;

        public AudioProcessor()
        {
            waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(sampleRate, 1),
                BufferMilliseconds = bufferMs,
                DeviceNumber = 0 // 0 is the default audio input device

            };
            waveIn.DataAvailable += OnDataAvailable;

            DataSize = (sampleRate / 1000) * bufferMs; // Number of samples in the buffer
            MagnitudeBuffer = new float[DataSize];
            FrequencyBuffer = new float[DataSize];

            for (int i = 0; i < DataSize; i++)
            {
                FrequencyBuffer[i] = i * sampleRate / DataSize;
            }

            // Design the band-pass filter (20 Hz to 15,000 Hz)
            int filterOrder = 128; // Adjust order as needed
            double lowCutoff = 20.0 / (sampleRate / 2.0);
            double highCutoff = 20000.0 / (sampleRate / 2.0);
            double[] coefficients = MathNet.Filtering.FIR.FirCoefficients.BandPass(filterOrder, lowCutoff, highCutoff);
            BandPassFilter = new OnlineFirFilter(coefficients);
        }

        public void Start()
        {
            if (IsRecording)
            {
                Debug.WriteLine("Already recording");
                return;
            }
            waveIn.StartRecording();
            IsRecording = true;
            Debug.WriteLine("Recording Started");

            //SendBuffer = new System.Timers.Timer(bufferMs);
            //SendBuffer.Elapsed += SendAudioBuffers;
            //SendBuffer.AutoReset = true;
            //SendBuffer.Start();
        }

        public void Stop()
        {
            if (!IsRecording)
            {
                Debug.WriteLine("Not currently recording");
                return;
            }
            waveIn.StopRecording();
            //SendBuffer.Stop();
            IsRecording = false;
            Debug.WriteLine("Recording Stopped");
        }

        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            double[] audioData = new double[e.BytesRecorded / 2]; // Audio signal conversion

            // Each audio sample is a 16-bit value, so two bytes are processed at once.
            for (int i = 0; i < e.BytesRecorded; i += 2)
            {
                short sample = (short)((e.Buffer[i + 1] << 8) |
                                e.Buffer[i + 0]);
                //short sample = BitConverter.ToInt16(e.Buffer, i);
                audioData[i / 2] = sample / 32768.0; // Normalises data to range [-1.0, 1.0]
            }

            ProcessAudio(audioData);
        }

        public void ProcessAudio(double[] audioData)
        {
            // double[] filteredData = BandPassFilter.ProcessSamples(audioData); // Apply filter to audible range

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
            int N = transformedData.Length;

            for (int i = 0; i < N / 2; i++)
            {
                MagnitudeBuffer[i] = (float)transformedData[i].Magnitude;
            }

            if (IsRecording)
            {
                OnAudioBufferEvent?.Invoke(MagnitudeBuffer, FrequencyBuffer);
            }
        }

        public float[] GetMagnitudeBuffer()
        {
            return MagnitudeBuffer;
        }

        public float[] GetFrequencyBuffer()
        {
            return FrequencyBuffer;
        }

        private void SendAudioBuffers(object? sender, ElapsedEventArgs? e)
        {
            OnAudioBufferEvent?.Invoke(MagnitudeBuffer, FrequencyBuffer);
        }

    }
}