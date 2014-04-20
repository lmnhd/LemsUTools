using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using LemsUTools.WaveUtils;
using TagLib;
using LemsDotNetHelpers;
using NAudio.Mixer;
using NAudio.Wave;
using NAudio.Utils;
using NAudio.Wave.SampleProviders;


namespace LemsUTools
{
    public class Mp3Utils
    {
        public static bool WriteTagAndReturnFile(TagLib.Tag toWrite, string path, bool dispose = true)
        {
            File result = null;
            try
            {
                result = TagLib.File.Create(new File.LocalFileAbstraction(path));
                toWrite.CopyTo(result.Tag, false);

                result.Save();

                if (dispose)
                {
                    result.Dispose();
                }

            }
            catch
            {
                return false;

            }
            return true ;


        }
        //public static void writebpm() {
        //    TagLib.Id3v2.Tag tag = new TagLib.Id3v2.Tag();



        //    tag.BeatsPerMinute = 138;

        //    TagLib.File file = TagLib.File.Create(new File.LocalFileAbstraction(@"K:\wamp\www\mp3\Pedagree\Purp.mp3"));



        //}
        public static class Mp3Helpers
        {
            public static bool AddPictureToTag(string mp3filePath, byte[] imageData)
            {
                var bytes = new byte[0];
                TagLib.File tagfile = TagLib.File.Create(mp3filePath);
                //Stream image = new MemoryStream(tagfile.Tag.Pictures[0].Data.Data);
                //this.pictureBox1.Image = new Bitmap(image);
                tagfile.Tag.Clear();
                List<TagLib.IPicture> listpic = new List<TagLib.IPicture>();
                listpic.Add(new TagLib.Picture
                {
                    Data = new TagLib.ByteVector(bytes),
                    Description = "",
                    MimeType = "image/jpg",
                    Type = TagLib.PictureType.FileIcon
                });
                listpic.Add(new TagLib.Picture
                {
                    Data = new TagLib.ByteVector(imageData),
                    Description = "",
                    MimeType = "image/jpg",
                    Type = TagLib.PictureType.FrontCover
                });
                listpic.Add(new TagLib.Picture
                {
                    Data = new TagLib.ByteVector(imageData),
                    Description = "",
                    MimeType = "image/jpg",
                    Type = TagLib.PictureType.BackCover
                });

                if (tagfile.Tag.Pictures.Length == 0)
                    tagfile.Tag.Pictures = listpic.ToArray();
                else
                    foreach (TagLib.IPicture tp in tagfile.Tag.Pictures)
                        tp.Data = new TagLib.ByteVector(imageData);
                tagfile.Save();

                tagfile.Dispose();
                listpic = null;

                return true;


            }

            public static bool ConvertWaveToMp3(string wavfilePath, string mp3Path, TimeSpan cutFromStart, TimeSpan cutFromEnd)
            {

                return LemsUTools.WaveUtils.WavFileUtils.TrimFile(wavfilePath, mp3Path, cutFromStart, cutFromEnd);
            }
        }
        public static string ConvertMp3ToWave(string mp3)
        {

            var outputFile = DamnDat.StringSplit(mp3, ".mp3")[0] + ".wav";
            using (Mp3FileReader reader = new Mp3FileReader(mp3))
            {
                WaveFileWriter.CreateWaveFile(outputFile, reader);
                return outputFile;
            }

        }
        public void addFade()
        {


        }


        public static void testConverAndTrimAudioFile()
        {

            var waveMan = new WaveAndMp3Manipulation();
            var wavePath = @"K:\wamp\www\mp3\Outhouse\Cc\1.wav";
            var mp3Path = @"K:\wamp\www\mp3\Outhouse\Cc\000test.mp3";
            var workPath = @"k:\wamp\tempshit\";
            if(!System.IO.Directory.Exists(workPath)){
                System.IO.Directory.CreateDirectory(workPath);
            }

            var mm = new WaveAndMp3Manipulation.MasterMix();

            var done = mm.ConvertAndTrimAudioFile(wavePath, mp3Path, waveMan, workPath);

            Console.ReadLine();






        }
        public static void handleWaveManEvent(string message)
        {
            Console.WriteLine(message);
            //Console.Read();
        }
        public static void testMasterMix()
        {
            var waveman = new WaveAndMp3Manipulation(true);
            waveman.WaveManEvent += handleWaveManEvent;
            List<WaveAndMp3Manipulation.MasterMixArgs> args = new List<WaveAndMp3Manipulation.MasterMixArgs>();
            var MIXTAPETESTFOLDER = @"K:\wamp\www\MIXTAPE_MAKER_TESTS";
            var testPath = MIXTAPETESTFOLDER + @"\MasterMixMakerEdits";
            var firstTag = @"TAG_UNTAMEMUSIC.mp3";
            var tagsPath = @"K:\wamp\www\test_tags\";
            var tagsList = System.IO.Directory.GetFiles(tagsPath).ToList();
            var mp3list = System.IO.Directory.GetFiles(MIXTAPETESTFOLDER + @"\base mp3\", "*.mp3", System.IO.SearchOption.AllDirectories).ToList();
            var rand = new Random();
            var templist = new List<string>();

            //if (System.IO.Directory.Exists(testPath))
            //{
            //    System.IO.Directory.Delete(testPath,true);
            //}
            //for (var i = 0; i < 5; i++)
            //{
                
            //    templist.Add(mp3list[rand.Next(mp3list.Count() - 1)]);
            //   // templist.Add(mp3list[i + 21]);
            //    //templist.Add(mp3list[0]);




            //}
            foreach (string path in mp3list)
            {
                WaveAndMp3Manipulation.MasterMixArgs arg = new WaveAndMp3Manipulation.MasterMixArgs()
                {
                    trackOrder = mp3list.IndexOf(path),
                    preTagPath = waveman.GetRandomTransitionTag(tagsList, rand),
                    overLayPathsAndStarts = waveman.GetRandomOverlayList(rand.Next(5, 6), path, tagsList, TimeSpan.FromSeconds(120), testPath),
                    trackPath = path,
                    scanPretagsForSpace = true,
                    scanOverlaysForSpace = true,
                    fadeOutDuration = 0d,
                    endtime = TimeSpan.FromSeconds(60),
                    preSongOverlapInMilliseconds = 1000d,
                    alternateEditsLocation = testPath,
                    fadeIn = true,
                    fadeInDuration = 4000d,
                     truncateStart = TimeSpan.FromSeconds(30)

                    
                    
                };

                args.Add(arg);

            }

            var mm = new WaveAndMp3Manipulation.MasterMix();

            mm.CreateMasterMix(waveman, args, tagsPath + firstTag, MIXTAPETESTFOLDER + @"\Mix.wav");
            //var ct = new CancellationToken();
            //System.Diagnostics.Stopwatch clock = new System.Diagnostics.Stopwatch();
            //clock.Start();
            //try
            //{

            //    //var step1 = waveman.BeginMasterMixAsync(args, ct, tagsPath + firstTag, @"C:\Users\BricklyfeA\Desktop" + @"\Mix.wav", @"C:\Users\BricklyfeA\Desktop\EDITS");

            //    //clock.Stop();
                
            //    //Console.WriteLine(clock.ElapsedMilliseconds);

            //    //var finalPackage = WaveAndMp3Manipulation.MasterMix.FinishMasterMix(step1, tagsPath + firstTag, @"C:\Users\BricklyfeA\Desktop" + @"\Mix.wav", @"C:\Users\BricklyfeA\Desktop\EDITS");
            //    System.IO.Directory.Delete(@"C:\Users\BricklyfeA\Desktop\EDITS", true);
            //    Console.WriteLine("done");

            //}
            //catch(AggregateException e)
            //{
            //   // WaveAndMp3Manipulation.MasterMix.CreateMasterMix(args, tagsPath + firstTag, @"C:\Users\BricklyfeA\Desktop" + @"\Mix.wav");
            //    System.IO.Directory.Delete(@"C:\Users\BricklyfeA\Desktop\EDITS", true);
            //    Console.WriteLine("done");
            //}
          
        }

        public static void test2()
        {
            var wavePaths = new List<string>();

            var mp3list = System.IO.Directory.GetFiles(@"K:\wamp\www\mp3\Hump", "*.mp3", System.IO.SearchOption.AllDirectories);
            var rand = new Random();
            var templist = new List<string>();
            for (var i = 0; i < 1; i++)
            {

                //  templist.Add(mp3list[rand.Next(mp3list.Count() - 1)]);
                templist.Add(mp3list[0]);




            }


            foreach (string sng in templist)
            {

              //  wavePaths.Add(WaveAndMp3Manipulation.ConvertMp3ToWave(sng));

            }







            //var sng2 = new WaveFileReader(wavePaths[0]);
            var sng1 = new WaveFileReader(wavePaths[0]);
            //var sng1fade = new WaveFileReader(wavePaths[1]);
            //var sng3 = new WaveFileReader(wavePaths[2]);

            var mixer = new WaveMixerStream32();

            mixer.AutoStop = true;





            var preSpace1 = TimeSpan.Zero;
            while (sng1.ReadNextSampleFrame()[0] < .1)
            {

                preSpace1 = sng1.CurrentTime;
            }
            var endTime = TimeSpan.Zero;
            var endSpace = TimeSpan.Zero;
            var sampleCount = 0;
            var ii = 0;
            while (sng1.HasData(1))
            {
                var cnt = sng1.ReadNextSampleFrame();
                if (cnt[0] == 0 || cnt[1] == 0)
                {
                    sampleCount++;
                    if (sampleCount > 88200)
                    {
                        endTime = sng1.CurrentTime;
                        endSpace = sng1.TotalTime.Subtract(sng1.CurrentTime);
                        break;
                    }

                }
                else
                {

                    sampleCount = 0;
                }
            }


            //var preSpace2 = TimeSpan.Zero;
            //while (sng2.ReadNextSampleFrame()[0] < .1)
            //{

            //    preSpace2 = sng2.CurrentTime;
            //}

            //var endSpace2 = TimeSpan.Zero;
            //var sampleCount2 = 0;

            //while (sng2.HasData(1))
            //{
            //    var cnt = sng2.ReadNextSampleFrame();
            //    if (cnt[0] == 0)
            //    {
            //        sampleCount2++;
            //        if (sampleCount2 > 88200)
            //        {
            //            endSpace2 = sng2.TotalTime.Subtract(sng2.CurrentTime);
            //            break;
            //        }

            //    }
            //    else
            //    {

            //        sampleCount2 = 0;
            //    }
            //}
            var endClip1 = TimeSpan.Zero;
            ////if (endSpace.TotalSeconds > 2)
            ////{
            ////   // endClip1 = TimeSpan.FromSeconds(10);
            ////}
            if (true)
            {
                TimeSpan microtime = TimeSpan.Zero;

                sng1.Skip(-2);
                var lastSampleCount = 0;
                while (sng1.HasData(1))
                {

                    var samp = sng1.ReadNextSampleFrame();

                    if (samp[0] == 0 || samp[1] == 0)
                    {

                        lastSampleCount++;

                        if (lastSampleCount > (5))
                        {
                            // endSpace = sng1.TotalTime.Subtract(sng1.CurrentTime);
                            if (sng1.TotalTime.Subtract(sng1.CurrentTime).Equals(TimeSpan.Zero))
                            {
                                endSpace = TimeSpan.FromMilliseconds(sng1.CurrentTime.Milliseconds);
                            }
                            else
                            {
                                endSpace = sng1.TotalTime.Subtract(sng1.CurrentTime);
                            }

                            break;
                        }

                    }
                }

                    //    }

                    //}


                    //    sng1.TotalTime.Subtract(TimeSpan.FromMilliseconds(50000)),
                    //   TimeSpan.Zero);
                    // sng1fade = new WaveFileReader(fadewavpath);


















                    var tlength = sng1.TotalTime.Subtract(endSpace.Add(endClip1))
                        .Subtract(preSpace1);
                    var preFadeLength = tlength.Subtract(TimeSpan.FromSeconds(2));
                    sng1.Skip((int)-sng1.TotalTime.TotalSeconds);


                    WaveOffsetStream offsetStream1 = new WaveOffsetStream(sng1, TimeSpan.Zero.Subtract(TimeSpan.FromSeconds(0)), preSpace1, sng1.TotalTime.Subtract(endSpace.Add(endClip1)).Subtract(TimeSpan.FromSeconds(2)));

                    //  WaveOffsetStream offsetStreamfade = new WaveOffsetStream(sng1fade, preFadeLength, TimeSpan.Zero,sng1.TotalTime);



                    //  //  var olength = offsetStream1.TotalTime;





                    var channel1 = new WaveChannel32(offsetStream1);
                    //var fadeChannel = new WaveChannel32(offsetStreamfade);
                    channel1.PadWithZeroes = false;
                    channel1.Volume = .5f;
                    // fadeChannel.PadWithZeroes = false;
                    mixer.AddInputStream(channel1);
                    // mixer.AddInputStream(fadeChannel);

                    //   //sng1.BeginFadeIn(3000);

                    WaveFileWriter.CreateWaveFile(@"K:\wamp\www\mp3\Pedagree\mycomposed.wav", new Wave32To16Stream(mixer));
                





                //    var offset2 = sng1.TotalTime.Subtract(preSpace1);


                //    var endClip2 = TimeSpan.Zero;
                //     if (true)
                //    {
                //        TimeSpan microtime = TimeSpan.Zero;

                //        sng2.Skip(-2);
                //        var lastSampleCount2 = 0;
                //        while (sng2.HasData(1))
                //        {

                //            var samp2 = sng2.ReadNextSampleFrame();

                //            if (samp2[0] == 0 || samp2[1] == 0)
                //            {

                //                lastSampleCount2++;

                //                if (lastSampleCount2 > (5))
                //                {
                //                    // endSpace = sng1.TotalTime.Subtract(sng1.CurrentTime);
                //                    if (sng2.TotalTime.Subtract(sng2.CurrentTime).Equals(TimeSpan.Zero))
                //                    {
                //                        endSpace2 = TimeSpan.FromMilliseconds(sng2.CurrentTime.Milliseconds);
                //                    }
                //                    else
                //                    {
                //                        endSpace2 = sng2.TotalTime.Subtract(sng2.CurrentTime);
                //                    }

                //                    break;

                //                }

                //            }

                //        }

                //    }




                //    offset2 = offset2.Subtract(endSpace.Add(endClip1));
                //    endClip2 = endClip2.Add(preSpace2);
                //    WaveOffsetStream offset2stream = new WaveOffsetStream(sng2, offset2, preSpace2, sng2.TotalTime.Subtract(endSpace2.Add(endClip2)));

                //  //  WaveOffsetStream sng2offsetted = new WaveOffsetStream(sng2, TimeSpan.FromSeconds(10), TimeSpan.Zero, sng2.TotalTime.Subtract(TimeSpan.FromSeconds(30)));

                //    var channel2  = new WaveChannel32(offset2stream);
                //   channel2.PadWithZeroes = false;

                //    //var preSpace2 = TimeSpan.Zero;
                //    //while (sng2.ReadNextSampleFrame()[0] < 0.04f)
                //    //{
                //    //    preSpace2 = sng2.CurrentTime;
                //    //}
                //   // channel2.Volume = 0.7f;
                //    //mixer.AddInputStream(channel2);

                //   // WaveFileWriter.CreateWaveFile(@"K:\wamp\www\mp3\Pedagree\mycomposed.wav", new SampleToWaveProvider16 (samplemixer));

                //    WaveFileWriter.CreateWaveFile(@"K:\wamp\www\mp3\Pedagree\mycomposed.wav", new Wave32To16Stream(mixer));
                //}


                //private static void ReadData(string filename,ref IntPtr format1,ref Byte[] data1)
                //{
                //    var dr1 = new Mp3Reader(System.IO.File.OpenRead(filename));
                //    format1 = dr1.ReadFormat();
                //    data1 = dr1.ReadData();
                //    dr1.Close();

                //    var format1Pcm = AudioCompressionManager.GetCompatibleFormat(format1, AudioCompressionManager.PcmFormatTag);
                //    var data1Pcm = AudioCompressionManager.Convert(format1,format1Pcm,data1,false);
                //    format1 = format1Pcm;
                //    data1 = data1Pcm;
                //}
            }
        }
    }


namespace UntameMusic2014.Helpers
{
   
}
}
