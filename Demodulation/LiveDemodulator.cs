using NAudio.Wave;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Rs41Decoder.Demodulation
{
    /// <summary>
    /// Represents a demodulator for the RS41 radiosonde where the data source is an audio input device.
    /// </summary>
    internal class LiveDemodulator : DemodulatorBase
    {
        /// <summary>
        /// The rate to sample the audio input device at.
        /// </summary>
        private const int SAMPLE_RATE = 37500;

        /// <summary>
        /// The number of the audio input device to use as the data source.
        /// </summary>
        private readonly int deviceNumber;

        /// <summary>
        /// The audio input device.
        /// </summary>
        private WaveInEvent? audioDevice = null;

        /// <summary>
        /// Buffers bytes received from the audio input device.
        /// </summary>
        private readonly ConcurrentQueue<byte> audioBuffer = new ConcurrentQueue<byte>();

        /// <summary>
        /// Initialises a new instance of the <see cref="FileDemodulator"/> class.
        /// </summary>
        /// <param name="deviceNumber">
        /// The number of the audio input device to use as the data source. The device should output mono-channel audio
        /// at 16 bits per sample.
        /// </param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/>, used for cancelling the demodulation.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="deviceNumber"/> is less than zero.
        /// </exception>
        public LiveDemodulator(int deviceNumber, CancellationToken cancellationToken)
            : base(cancellationToken)
        {
            if (deviceNumber < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(deviceNumber),
                    nameof(deviceNumber) + " must be greater than zero");
            }

            this.deviceNumber = deviceNumber;
            samplesPerDemodBit = (double)SAMPLE_RATE / Constants.BAUD_RATE;
        }

        /// <summary>
        /// Opens the demodulator.
        /// </summary>
        public override void Open()
        {
            audioDevice = new WaveInEvent()
            {
                DeviceNumber = deviceNumber,
                WaveFormat = new WaveFormat(
                    SAMPLE_RATE, bitsPerWavSample, numberOfChannels)
            };

            audioDevice.DataAvailable += AudioDevice_DataAvailable;
            audioDevice.StartRecording();
        }

        private void AudioDevice_DataAvailable(object? sender, WaveInEventArgs e)
        {
            foreach (byte b in e.Buffer)
                audioBuffer.Enqueue(b);
        }

        /// <summary>
        /// Closes the demodulator.
        /// </summary>
        public override void Close()
        {
            audioDevice?.StopRecording();
        }

        /// <summary>
        /// Reads a byte from the WAV data, hanging indefinitely until a byte is available.
        /// </summary>
        /// <returns>
        /// The byte.
        /// </returns>
        /// <exception cref="OperationCanceledException">
        /// Thrown if the demodulation is cancelled.
        /// </exception>
        protected override byte ReadWavByte()
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (audioBuffer.TryDequeue(out byte b))
                    return b;
            }
        }

        /// <summary>
        /// Disposes the demodulator.
        /// </summary>
        public override void Dispose()
        {
            Close();
            audioDevice?.Dispose();
        }
    }
}
