using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Audio;

namespace ZXSpectrum
{
    /// <summary>
    /// Emulates the ZX Spectrum loudspeaker.
    /// 
    /// </summary>
    public class Loudspeaker
    {
        private DynamicSoundEffectInstance sfx;     //  Access to soundcard audio
        private List<float> buffer;                 //  Initial buffer used to sample the waveform         
        private List<byte> xnaBuffer;               //  Converted buffer in the format XNA expects
        private double waveTime = 0.0;              //  Keeps track of the 'time' at which to sample the waveform
        private double amplitude = 0.3;             //  Turned down to save ears!
        private Random r = new Random();
        /// <summary>
        /// Constructor.
        /// </summary>
        public Loudspeaker()
        {
            //  Standard CD sample-rate is 44.1KHz, but we only need a mono speaker
            sfx = new DynamicSoundEffectInstance(44100, AudioChannels.Mono);
            buffer = new List<float>();
            xnaBuffer = new List<byte>();
            sfx.Play();
        }

        /// <summary>
        /// Samples a sine wave of the given frequency at the given point in time.
        /// </summary>
        /// <param name="time"></param>
        /// <param name="frequency"></param>
        /// <returns></returns>
        private double SineWave(double time, double frequency)
        {
            return Math.Sin(time * 2 * Math.PI * frequency * amplitude);
        }

        /// <summary>
        /// Samples a square wave of the given frequency at the given point in time.
        /// </summary>
        /// <param name="time"></param>
        /// <param name="frequency"></param>
        /// <returns></returns>
        private double SquareWave(double time, double frequency)
        {
            return Math.Sin(frequency * time * 2 * Math.PI) >= 0 ? amplitude : -amplitude;
        }

        private double WhiteNoise()
        { 
            return r.NextDouble() - r.NextDouble();;
        }

        /// <summary>
        /// Samples a waveform and fills a buffer with the resulting samples.
        /// </summary>
        /// <param name="frequency">cycles per second</param>
        /// <param name="duration">seconds</param>
        private void CreateBuffer(double frequency, float duration)
        {
            buffer.Clear();
            waveTime = 0;
            int numSamples = (int)(duration * 44100);
            for (int i = 0; i < numSamples; i++)
            {
                // Sample the wave
                buffer.Add((float)SquareWave(waveTime, frequency));
                //buffer.Add((float)WhiteNoise());
                // Time at which to sample the waveform increases
                waveTime += 1.0 / 44100;

            }
        }

        /// <summary>
        /// Plays a note of given pitch and duration.
        /// </summary>
        /// <param name="frequency"></param>
        /// <param name="duration"></param>
        public void Play(double frequency, float duration)
        {
            CreateBuffer(frequency, duration);
            SendBuffer();     
        }

        /// <summary>
        /// Converts the waveform buffer to XNA format and submits it for playback.
        /// </summary>
        /// <param name="bufferIn"></param>
        /// <param name="bufferOut"></param>
        private void SendBuffer()
        {
            short sample;
            xnaBuffer.Clear();
            for (int i = 0; i < buffer.Count; i++)
            {
                //  Convert buffer to 16-bit representation   
                sample = (short)(buffer[i] * short.MaxValue);

                //  Output the converted samples
                xnaBuffer.Add((byte)sample);
                xnaBuffer.Add((byte)(sample >> 8));
                if (xnaBuffer.Count == 3000)
                {
                    sfx.SubmitBuffer(xnaBuffer.ToArray());
                    xnaBuffer.Clear();
                }
            }
            if (xnaBuffer.Count > 0)
            {
                sfx.SubmitBuffer(xnaBuffer.ToArray());
            }
        }
    }
}
