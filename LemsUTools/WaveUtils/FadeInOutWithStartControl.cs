using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TagLib;
using LemsDotNetHelpers;
using NAudio.Mixer;
using NAudio.Wave;
using NAudio.Utils;
using NAudio.Wave.SampleProviders;

namespace LemsUTools.WaveUtils
{
    public class FadeInOutWithStartControl : ISampleProvider
    {
        enum FadeState
        {
            Silence,
            FadingIn,
            FullVolume,
            FadingOut,
        }

        private readonly object lockObject = new object();
        private readonly ISampleProvider source;
        private int fadeSamplePosition;
        private int fadeSampleCount;
        private FadeState fadeState;
        private WaveStream waveStream;
        private double fadeDuration;
        private TimeSpan start;
        private WaveAndMp3Manipulation waveman;
        private bool _debugSamplesOnFades;

        public FadeInOutWithStartControl(ISampleProvider source, WaveStream sourceStream, TimeSpan startTime, double fadeDurationInMilliseconds,WaveAndMp3Manipulation parent = null,bool debugSample = false)
        {
            this.source = source;
            this.fadeState = FadeState.FullVolume;
            this.waveStream = sourceStream;
            this.fadeDuration = fadeDurationInMilliseconds;
            this.start = startTime;
            if (parent != null)
            {
                waveman = parent;

            }
            _debugSamplesOnFades = debugSample;
        }

        public void BeginFadeIn(double fadeDurationInMilliseconds)
        {
            lock (lockObject)
            {
                if (waveman != null)
                {
                    waveman.RaiseEvent(string.Format("Begginning actual fade in for {0}", fadeDurationInMilliseconds.ToString()));
                }
                fadeSamplePosition = 0;
                fadeSampleCount = (int)((fadeDurationInMilliseconds * source.WaveFormat.SampleRate) / 1000);
                fadeState = FadeState.FadingIn;
            }
        }

        public void BeginFadeOut(double fadeDurationInMilliseconds)
        {
            lock (lockObject)
            {
                if (waveman != null)
                {
                    waveman.RaiseEvent(string.Format("Begginning actual fade out for {0}", fadeDurationInMilliseconds.ToString()));
                }
                fadeSamplePosition = 0;
                fadeSampleCount = (int)((fadeDurationInMilliseconds * source.WaveFormat.SampleRate) / 1000);
                fadeState = FadeState.FadingOut;
            }
        }

        public int Read(float[] buffer, int offset, int count)
        {
            
            if (waveStream.CurrentTime.TotalMilliseconds > start.TotalMilliseconds)
            {
                if (fadeState != FadeState.FadingOut && fadeState != FadeState.Silence)
                {
                    BeginFadeOut(fadeDuration);
                }


            }
            int sourceSamplesRead = source.Read(buffer, offset, count);
            lock (lockObject)
            {
                if (fadeState == FadeState.FadingIn)
                {
                    FadeIn(buffer, offset, sourceSamplesRead);
                }
                else if (fadeState == FadeState.FadingOut)
                {
                    FadeOut(buffer, offset, sourceSamplesRead);
                }
                else if (fadeState == FadeState.Silence)
                {
                    ClearBuffer(buffer, offset, count);
                }
            }
            return sourceSamplesRead;
        }

        private  void ClearBuffer(float[] buffer, int offset, int count)
        {
            
            for (int n = 0; n < count; n++)
            {
                buffer[n + offset] = 0;
            }
        }

        private void FadeOut(float[] buffer, int offset, int sourceSamplesRead)
        {
            if (waveman != null)
            {
                waveman.RaiseEvent(string.Format("FadeOut:"));
            }
            int sample = 0;
            while (sample < sourceSamplesRead)
            {
                float multiplier = 1.0f - (fadeSamplePosition / (float)fadeSampleCount);
                for (int ch = 0; ch < source.WaveFormat.Channels; ch++)
                {
                    buffer[offset + sample++] *= multiplier;
                }
                fadeSamplePosition++;
                if (fadeSamplePosition > fadeSampleCount)
                {
                    fadeState = FadeState.Silence;
                    // clear out the end
                    ClearBuffer(buffer, sample + offset, sourceSamplesRead - sample);
                    break;
                }
            }
        }

        private void FadeIn(float[] buffer, int offset, int sourceSamplesRead)
        {
            if (waveman != null)
            {
                waveman.RaiseEvent(string.Format("FadeIn:"));
            }
            int sample = 0;
            while (sample < sourceSamplesRead)
            {
                float multiplier = (fadeSamplePosition / (float)fadeSampleCount);

                if (waveman != null)
                {
                    if (_debugSamplesOnFades)
                    {
                        waveman.RaiseEvent(string.Format("multiplier = {0}", multiplier.ToString()));
                    }
                    
                }


                for (int ch = 0; ch < source.WaveFormat.Channels; ch++)
                {
                    buffer[offset + sample++] *= multiplier;
                    if (waveman != null)
                    {
                        if (_debugSamplesOnFades)
                        {
                            waveman.RaiseEvent(string.Format("sample := {0}", sample));
                        }
                        
                    }
                }
                fadeSamplePosition++;
                if (fadeSamplePosition > fadeSampleCount)
                {
                    fadeState = FadeState.FullVolume;
                    if (waveman != null)
                    {
                        waveman.RaiseEvent(string.Format("Fade in at full volume! , multiplier = {0}, sample = {1}", multiplier.ToString(),sample));
                    }

                    // no need to multiply any more
                    break;
                }
            }
        }

        public WaveFormat WaveFormat
        {
            get { return source.WaveFormat; }
        }
    }

    
}
