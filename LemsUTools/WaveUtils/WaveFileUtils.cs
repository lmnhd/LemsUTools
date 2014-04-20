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
using NAudio.Lame;
using NAudio.Wave.SampleProviders;

namespace LemsUTools.WaveUtils
{
    public static class WavFileUtils
    {
        public static bool TrimFile(string inPath, string outPath, TimeSpan cutFromStart, TimeSpan cutFromEnd,  byte[] returnBytes = null,WaveAndMp3Manipulation waveMan = null)
        {
            using (WaveFileReader reader = new WaveFileReader(inPath))
            {
                if (outPath.EndsWith(".wav"))
                {
                    using (WaveFileWriter writer = new WaveFileWriter(outPath, reader.WaveFormat))
                    {
                        if (waveMan != null)
                        {
                            waveMan.RaiseEvent(string.Format("trimming {0}", outPath));
                        }
                        
                        int bytesPerMillisecond = reader.WaveFormat.AverageBytesPerSecond / 1000;

                        int startPos = (int)cutFromStart.TotalMilliseconds * bytesPerMillisecond;
                        startPos = startPos - startPos % reader.WaveFormat.BlockAlign;

                        int endBytes = (int)cutFromEnd.TotalMilliseconds * bytesPerMillisecond;
                        endBytes = endBytes - endBytes % reader.WaveFormat.BlockAlign;
                        int endPos = (int)reader.Length - endBytes;
                        if (waveMan != null)
                        {
                            waveMan.RaiseEvent(string.Format("start pos : {0} , end pos : {1}", startPos, endPos));
                        }
                        
                        
                       return TrimWaveFile(reader, writer, startPos, endPos,returnBytes);
                    }

                }
                else
                {
                    using (var stream = System.IO.File.Create(outPath))
                    {
                        using (LameMP3FileWriter writer = new NAudio.Lame.LameMP3FileWriter(stream, reader.WaveFormat, 192))
                        {
                            int bytesPerMillisecond = reader.WaveFormat.AverageBytesPerSecond / 1000;

                            int startPos = (int)cutFromStart.TotalMilliseconds * bytesPerMillisecond;
                            startPos = startPos - startPos % reader.WaveFormat.BlockAlign;

                            int endBytes = (int)cutFromEnd.TotalMilliseconds * bytesPerMillisecond;
                            endBytes = endBytes - endBytes % reader.WaveFormat.BlockAlign;
                            int endPos = (int)reader.Length - endBytes;

                            TrimMp3File(reader, writer, startPos, endPos);
                            writer.Flush();
                            writer.Dispose();

                        }
                    }
                    
                   // System.IO.FileStream stream = new System.IO.FileStream(
                   

                }


                return true;

            }
        }

        private static bool TrimWaveFile(WaveFileReader reader, WaveFileWriter writer, int startPos, int endPos,byte[] returnBytes = null)
        {
            reader.Position = startPos;
            byte[] buffer = new byte[1024];
            var i = 0;
            while (reader.Position < endPos)
            {
                int bytesRequired = (int)(endPos - reader.Position);
                if (bytesRequired > 0)
                {
                    int bytesToRead = Math.Min(bytesRequired, buffer.Length);
                    int bytesRead = reader.Read(buffer, 0, bytesToRead);
                    if (bytesRead > 0)
                    {
                        if (returnBytes == null)
                        {
                            writer.Write(buffer, 0, bytesRead);
                        }
                        else
                        {
                            buffer.CopyTo(returnBytes, i * 1024);

                        }
                        
                    }

                }
            }
            return true;

        }
        private static void TrimMp3File(WaveFileReader reader, NAudio.Lame.LameMP3FileWriter writer, int startPos, int endPos)
        {
            reader.Position = startPos;
            byte[] buffer = new byte[reader.WaveFormat.AverageBytesPerSecond];
            while (reader.Position < endPos)
            {
                int bytesRequired = (int)(endPos - reader.Position);
                if (bytesRequired > 0)
                {
                    int bytesToRead = Math.Min(bytesRequired, buffer.Length);
                    int bytesRead = reader.Read(buffer, 0, bytesToRead);
                    if (bytesRead > 0)
                    {
                        writer.Write(buffer, 0, bytesRead);
                    }
                }
            }
        }

        public static byte[] TrimWavFileDB(string inPath, TimeSpan cutFromStart, TimeSpan cutFromEnd)
        {
           
            using (WaveFileReader reader = new WaveFileReader(inPath))
            {
                using (WaveFileWriter writer = new WaveFileWriter("dummy.wav", reader.WaveFormat))
                {
                    int bytesPerMillisecond = reader.WaveFormat.AverageBytesPerSecond / 1000;

                    int startPos = (int)cutFromStart.TotalMilliseconds * bytesPerMillisecond;
                    startPos = startPos - startPos % reader.WaveFormat.BlockAlign;

                    int endBytes = (int)cutFromEnd.TotalMilliseconds * bytesPerMillisecond;
                    endBytes = endBytes - endBytes % reader.WaveFormat.BlockAlign;
                    int endPos = (int)reader.Length - endBytes;

                    return TrimWavFileDB(reader, writer, startPos, endPos);
                }
            }
        }

        private static byte[]  TrimWavFileDB(WaveFileReader reader, WaveFileWriter writer, int startPos, int endPos)
        {
            byte[] result = new byte[reader.SampleCount * reader.WaveFormat.Channels * reader.WaveFormat.BlockAlign];
            reader.Position = startPos;
            byte[] buffer = new byte[1024];
            var i = 0;
            while (reader.Position < endPos)
            {
                int bytesRequired = (int)(endPos - reader.Position);
               
                if (bytesRequired > 0)
                {
                    int bytesToRead = Math.Min(bytesRequired, buffer.Length);
                    int bytesRead = reader.Read(buffer, 0, bytesToRead);
                    if (bytesRead > 0)
                    {

                        buffer.CopyTo(result, 1024 * i++);
                    }
                }
            } 
            return result;

           // NAudio.Wave.RawSourceWaveStream st = new 

        }
    }
}
