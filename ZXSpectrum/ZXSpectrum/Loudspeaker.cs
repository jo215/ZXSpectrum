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
        public DynamicSoundEffectInstance sfx;     //  Access to soundcard audio        
        private List<byte> xnaBuffer;               //  Converted buffer in the format XNA expects

        /// <summary>
        /// Constructor.
        /// </summary>
        public Loudspeaker()
        {
            //  Standard CD sample-rate is 44.1KHz, but we only need a mono speaker
            sfx = new DynamicSoundEffectInstance(44100, AudioChannels.Mono);
            xnaBuffer = new List<byte>();
            sfx.Play();
        }

        /// <summary>
        /// Converts the waveform buffer to XNA format and submits it for playback.
        /// </summary>
        /// <param name="bufferIn"></param>
        /// <param name="bufferOut"></param>
        public void SendBuffer(List<float> buffer)
        {
            short sample;
            for (int i = 0; i < buffer.Count; i++)
            {
                //  Convert buffer to 16-bit representation   
                sample = (short)(buffer[i] * short.MaxValue);

                //  Output the converted samples
                xnaBuffer.Add((byte)sample);
                xnaBuffer.Add((byte)(sample >> 8));
            }

            sfx.SubmitBuffer(xnaBuffer.ToArray());
            xnaBuffer.Clear();
        }
    }
}
