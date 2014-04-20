using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using TagLib;
using LemsDotNetHelpers;
using NAudio.Mixer;
using NAudio.Wave;
using NAudio.Utils;
using NAudio.Wave.SampleProviders;
using NAudio.FileFormats.Mp3;
using NLayer.NAudioSupport;
using LemsUTools.WaveUtils;


namespace LemsUTools
{
    public  class WaveAndMp3Manipulation
    {
        public delegate void WaveManEventHandler(string message);
        public event WaveManEventHandler WaveManEvent;
        public void RaiseEvent(string message)
        {
            if (WaveManEvent != null)
            {
                WaveManEvent(message);
            }
        }
        public WaveAndMp3Manipulation(bool logFadeSamples = false)
        {
            masterMix = new MasterMix();
            LogFadeSamples = logFadeSamples;

            
        }
        public  string ConvertMp3ToWave(string mp3,bool overwrightIfExists = true,string useThisPath = "")
        {
            RaiseEvent(string.Format("Converting {0} to wave...",mp3));
            if ((!mp3.ToLower().Contains(".mp3") ) && (!mp3.ToLower().Contains(".wav")) || mp3.ToLower().Contains(".sfk"))
            {
                RaiseEvent(string.Format("Invalid File... {0}", mp3));
                throw new Exception("Invalid File");

            }

            
            var outputFile = System.IO.Path.GetFileNameWithoutExtension(mp3);
           
            var dir = new System.IO.FileInfo(mp3).Directory.FullName;
            if (useThisPath != "")
            {
                dir = useThisPath;
            }
            
            var tempFolder = @"\convertedfrommp3\";
            outputFile = string.Format("{0}{1}{2}.wav", dir, tempFolder, outputFile);

            if (System.IO.File.Exists(outputFile))
            {
                if (!overwrightIfExists)
                {
                    return outputFile;

                }
            }

            if(!System.IO.Directory.Exists(dir + tempFolder))
            {
                System.IO.Directory.CreateDirectory(dir + tempFolder);

            }
            if (mp3.ToLower().Contains(".wav"))
            {

                using (WaveFileReader readerwave = new WaveFileReader(mp3))
                {
                    if (readerwave.WaveFormat.SampleRate != 44100)
                    {
                        RaiseEvent(string.Format("mp3 samplerate is {0}, using WaveFormatConversionStream to convert to 44k",readerwave.WaveFormat.SampleRate.ToString()));

                        using(WaveFormatConversionStream converter = new WaveFormatConversionStream(new WaveFormat(), readerwave)) {
                            WaveFileWriter.CreateWaveFile(outputFile, converter);
                            return outputFile;
                        }
                        
                      
                    }

                    WaveFileWriter.CreateWaveFile(outputFile, readerwave);

                    
                        return outputFile;
                 

                }
              

            }
            
            using (Mp3FileReader reader = new Mp3FileReader(mp3, new Mp3FileReader.FrameDecompressorBuilder(wf => new NLayer.NAudioSupport.Mp3FrameDecompressor(wf))))
            {

                if (reader.WaveFormat.SampleRate != 44100)
                {
                    WaveFormatConversionStream converter = new WaveFormatConversionStream(new WaveFormat(), reader);
                    WaveFileWriter.CreateWaveFile(outputFile, converter);
                    return outputFile;
                }
                Wave32To16Stream stream = new Wave32To16Stream(reader);

                WaveFileWriter.CreateWaveFile(outputFile, stream);
                //System.Threading.Thread.Sleep(1000);
                return outputFile;
            }

        }

        public bool LogFadeSamples { get; set; }

        public  List<string> ConvertMp3ListToWaveList(List<string> mp3s, bool overwrightIfExists = true)
        {
            var result = new List<string>();
            foreach (string mp3 in mp3s)
            {
                result.Add(ConvertMp3ToWave(mp3,overwrightIfExists));

            }
            return result;
        }

        public  string FadeWave(string sourceFile,string destFile,TimeSpan startTime,double duration,bool fadeIn,double fadeInDuration = 0,string useDirectory = "",bool logSamples = false,bool cleanOnly = false)
        {
            if (fadeIn)
            {

                string newPath = System.IO.Directory.GetParent(destFile).FullName + @"\" + System.IO.Path.GetFileNameWithoutExtension(destFile) + "-FADEIN.wav";
                string trimPath = CreatePathFor("trim_for_fadeIn", sourceFile, System.IO.Path.GetFileNameWithoutExtension(sourceFile) + ".wav", useThisDirectory: useDirectory);
                using( WaveFileReader red = new WaveFileReader(sourceFile))
                {
                    OffsetResult res = SpaceScan(red);
                    WaveUtils.WavFileUtils.TrimFile(sourceFile, trimPath, res.preSpace, TimeSpan.Zero);
                    sourceFile = trimPath;

                    destFile = newPath;
                }
               
              
            }
            try
            {

                using (var reader = new WaveFileReader(sourceFile))
                {
                    if (!cleanOnly)
                    {
                        if (startTime == TimeSpan.Zero)
                        {
                            startTime = reader.TotalTime.Subtract(TimeSpan.FromSeconds(45));
                        }

                    }
                   


                    if (duration == 0)
                    {
                        duration = 2000;
                    }
                    //WaveFormatConversionStream converter = new WaveFormatConversionStream(WaveFormat.CreateCustomFormat(WaveFormatEncoding.Pcm,reader.WaveFormat.SampleRate,reader.WaveFormat.Channels,reader.WaveFormat.AverageBytesPerSecond,reader.WaveFormat.BlockAlign,reader.WaveFormat.BitsPerSample), reader);
                    //WaveFileWriter.CreateWaveFile(@"C:/testconvertopcm.wav", converter);
                    //WaveFileWriter.CreateWaveFile(@"C:/testNOconvertopcm.wav", reader);


                    Pcm16BitToSampleProvider sample = new Pcm16BitToSampleProvider(reader);

                    FadeInOutWithStartControl fader = new FadeInOutWithStartControl(sample, reader, startTime, duration,this,logSamples);

                    if (fadeIn)
                    {

                        RaiseEvent(string.Format("Beginning fade in with duration of {0} ",fadeInDuration));

                        //RaiseEvent(string.Format("{0}'s song position before skipback is {1}...", System.IO.Path.GetFileNameWithoutExtension(sourceFile),reader.CurrentTime.ToString()));

                        reader.Skip((int)-reader.TotalTime.TotalSeconds);

                        //RaiseEvent(string.Format("{0}'s song position after skipback is {1}...", System.IO.Path.GetFileNameWithoutExtension(sourceFile), reader.CurrentTime.ToString()));


                        fader.BeginFadeIn(fadeInDuration);
                    }
                    
                    MixingSampleProvider mixer = new MixingSampleProvider(fader.WaveFormat);

                    mixer.AddMixerInput(fader);

                    RaiseEvent(string.Format("Writing fade wave to {0}...{1}", destFile, Environment.NewLine));
                    WaveFileWriter.CreateWaveFile(destFile, new SampleToWaveProvider16(mixer));

                }

            }
            catch 
            {
                return "";

            }
           

            return destFile;

        }

        public struct OffsetResult
        {
           public TimeSpan preSpace { get; set; }
           public TimeSpan endSpace { get; set; }
           public TimeSpan endTime { get; set; }
           public TimeSpan endClip { get; set; }
           public TimeSpan totalTime { get; set; }
           public TimeSpan newStart { get; set; }
           
           public WaveOffsetStream offset { get; set; }

        }
        public  TimeSpan GetTotalLength(string filePath)
        {
            var result = TimeSpan.Zero;
            if (filePath.Contains(".wav"))
            {
                using (WaveFileReader reader = new WaveFileReader(filePath))
                {
                    result = reader.TotalTime;

                }
            }
            else if (filePath.Contains(".mp3"))
            {
                using (Mp3FileReader reader = new Mp3FileReader(filePath))
                {
                    result = reader.TotalTime;

                }

            }
            else
            {
                throw new Exception("Invalid File Type");

            }

            return result;
        }
        public  List<Tuple<string, int>> GetTags(string tagPath,string firstTag,List<TimeSpan> startsArray,bool overwrightIfExists = true)
        {
            var rand2 = new Random();
            var alltags = System.IO.Directory.GetFiles(tagPath).ToList();
            if (alltags.Count == 0)
            {
                throw new Exception("No Tags");

            }
            var temps = new List<string>();

            var allwavetags = System.IO.Directory.GetFiles(tagPath, ".wav").ToList(); ;
            alltags.AddRange(allwavetags);
            foreach (string tg in alltags)
            {
                if ((tg.ToLower().Contains(".mp3")) || (tg.ToLower().Contains(".wav")))
                {
                    temps.Add(tg);

                }

            }
            alltags = temps;
            alltags = ConvertMp3ListToWaveList(alltags,overwrightIfExists);
            var tags = new List<Tuple<string, int>>();
            tags.Add(new Tuple<string, int>(ConvertMp3ToWave(tagPath + @"\" + firstTag,overwrightIfExists), 0));
            foreach (TimeSpan start in startsArray)
            {
                var st = startsArray.IndexOf(start);
                var tagnum = alltags[rand2.Next(alltags.Count())];
                if (startsArray.IndexOf(start) != 0)
                {
                    tags.Add(new Tuple<string, int>(alltags[rand2.Next(alltags.Count - 1)], st));
                }

            }
            return tags;
        }

        public  string CleanAndFade(string path, TimeSpan timeToFade,double cutOffBeg, bool IsTag, double millisecondsToFade = 0, bool noFade = false, bool fadeIn = false,double fadeInDuration = 0,string useThisPathForEdits = "",bool log = false, bool logSamplesOnFades = false,bool cleanOnly = false)
        {
            if (IsTag)
            {
                path = ConvertMp3ToWave(path, false,useThisPathForEdits);
            }
            else
            {
                path = ConvertMp3ToWave(path,true,useThisPathForEdits);
                

                ////for convert 1 song (non mixtape) return path
                if (cleanOnly)
                {
                    return path;
                }
                string destin = CreatePathFor("clean_and_fade", path, useThisDirectory: useThisPathForEdits);
                if ((destin = FadeWave(path, destin, timeToFade.Subtract(TimeSpan.FromMilliseconds(cutOffBeg)), millisecondsToFade, fadeIn,fadeInDuration,"",logSamplesOnFades,cleanOnly )) != "")
                {
                    return destin;

                }
                else
                {

                    return path;
                }

            }

            return path;
           

        }
        private  List<MasterMixArgs> TPLList;
        private  int convertedCount = 0;
        private   string preTagPath;
        private  string finalWave;
        private MasterMix masterMix;

        public  void CleanAndFadeDone(string result,int index,int total)
        {
            var templist = TPLList.ToArray();
            templist[index].trackPath = result;
            TPLList = templist.ToList();
            convertedCount++;

            if (convertedCount == total)
            {
                masterMix.FinishMasterMix(this,TPLList, preTagPath, finalWave);

            }



        }
        public    List<MasterMixArgs> BeginMasterMixAsync(List<MasterMixArgs> args,CancellationToken token,string pretagPath,string finalWavePath, string useThisPathForEdits = "")
        {
            //convert each track to wave at the same time
            var returnlist = new List<MasterMixArgs>();

            TPLList = args;
            preTagPath = pretagPath;
            finalWave = finalWavePath;
            var result = new List<string>();



            var convertAndTrim = new TransformBlock<MasterMixArgs, MasterMixArgs>(arg =>
                {
                    var res = ConvertMp3ToWave(arg.trackPath, true, useThisPathForEdits);
                    if (arg.truncateStart.TotalMilliseconds > 0)
                    {
                        var trimpath = CreatePathFor("trim-beg", res, useThisDirectory: useThisPathForEdits);
                        if (LemsUTools.WaveUtils.WavFileUtils.TrimFile(res, trimpath, arg.truncateStart, TimeSpan.Zero))
                        {

                            res = trimpath;

                        }
                    }
                    arg.trackPath = res;

                    return arg;

                }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount });
            var fade = new TransformBlock<MasterMixArgs, MasterMixArgs>(arg =>
                {

                    var res = arg.trackPath;
                    var fadePath = CreatePathFor("fade", res, useThisDirectory: useThisPathForEdits);
                    res = FadeWave(res, fadePath, arg.endtime, arg.fadeOutDuration, arg.fadeIn, arg.fadeInDuration, useThisPathForEdits);
                    arg.trackPath = res;
                    return arg;

                });


            var finish = new ActionBlock<MasterMixArgs>(arg =>
                {
                    var argarray = TPLList.ToArray();
                    argarray[arg.trackOrder] = arg;
                    TPLList = argarray.ToList();

                });

            convertAndTrim.LinkTo(fade);
            fade.LinkTo(finish);

          convertAndTrim.Completion.ContinueWith( a =>
                {
                   
                    if (a.IsFaulted) ((IDataflowBlock)convertAndTrim).Fault(a.Exception);
                    else fade.Complete();


                });
          fade.Completion.ContinueWith(f =>
              {
                  if (f.IsFaulted) ((IDataflowBlock)fade).Fault(f.Exception);
                  else finish.Complete();


              });
            foreach (MasterMixArgs arg in TPLList)
            {
                convertAndTrim.Post(arg);
            }

            convertAndTrim.Complete();

            finish.Completion.Wait();

            return TPLList;
           
             
        }

        public  string CreatePathFor(string whatAreYouDoing,string origFilePath,string fileNameWithExt = "",string extension = ".wav",string useThisDirectory = "")
        {
            
            string oldPath = new System.IO.FileInfo(origFilePath).Directory.FullName;
            string oldName = System.IO.Path.GetFileNameWithoutExtension(origFilePath);
            string newPath = string.Format(@"{0}\{1}",oldPath,whatAreYouDoing);
            if (useThisDirectory != "")
            {
                newPath = string.Format(@"{0}\{1}", useThisDirectory, whatAreYouDoing);

            }
            string finalPath = string.Format(@"{0}\{1}",newPath,oldName + extension);
            if(fileNameWithExt != "") {
                 finalPath = string.Format(@"{0}\{1}",newPath,fileNameWithExt);
            }

            if (!System.IO.Directory.Exists(newPath))
            {
                System.IO.Directory.CreateDirectory(newPath);
            }
           
           

            return finalPath;
        }
        public  TimeSpan CalculateExtraSpaceForTag(string tagPath) 
        {

            var result = TimeSpan.Zero;
            if (tagPath.Contains(".mp3"))
            {
                Mp3FileReader reader = new Mp3FileReader(tagPath);
                if (reader.TotalTime.TotalSeconds > 2)
                {
                    result = reader.TotalTime.Subtract(TimeSpan.FromSeconds(2));

                }
            }
            else if (tagPath.Contains(".wav"))
            {
                WaveFileReader reader = new WaveFileReader(tagPath);
                if (reader.TotalTime.TotalSeconds > 2)
                {

                    result = reader.TotalTime.Subtract(TimeSpan.FromSeconds(2));
                }

            }

            return result;
        }

        public  OffsetResult SpaceScan(WaveFileReader stream)
        {
            var result = new OffsetResult();
            result.endClip = TimeSpan.Zero;
            var sampleCount = 0;
            stream.Skip((int)-stream.TotalTime.TotalSeconds);
            while (stream.ReadNextSampleFrame()[0] < .1)
            {
                result.preSpace = stream.CurrentTime;

            }

            while (stream.HasData(1) && sampleCount < 88202)
            {
                var cnt = stream.ReadNextSampleFrame();
                var check = false;
                for (var i = 0; i < stream.WaveFormat.Channels; i++)
                {
                    if (cnt[i] == 0 )
                    {
                        sampleCount++;
                        check = true;
                        if (sampleCount > 88200)
                        {

                            result.endTime = stream.CurrentTime;
                            result.endSpace = stream.TotalTime.Subtract(stream.CurrentTime);
                            break;
                        }

                    }
                   


                }
                if (!check)
                {
                    sampleCount = 0;
                }
              
            }
                TimeSpan microtime = TimeSpan.Zero;

                stream.Skip(-2);
                var lastSampleCount = 0;
                while (stream.HasData(1) && lastSampleCount < 6)
                {

                    var samp = stream.ReadNextSampleFrame();
                    for (var j = 0; j < stream.WaveFormat.Channels; j++)
                    {
                        if (samp[j] == 0 )
                        {

                            lastSampleCount++;

                            if (lastSampleCount > (35))
                            {
                                // endSpace = sng1.TotalTime.Subtract(sng1.CurrentTime);
                                if (stream.TotalTime.Subtract(stream.CurrentTime).Equals(TimeSpan.Zero))
                                {
                                    result.endSpace = TimeSpan.FromMilliseconds(stream.CurrentTime.Milliseconds);
                                }
                                else
                                {
                                    result.endSpace = stream.TotalTime.Subtract(stream.CurrentTime);
                                }
                                result.endTime = stream.CurrentTime;
                                break;
                            }

                        }
                    }
                   

                }

                if (result.endTime.TotalMilliseconds == 0)
                {
                    result.endTime = stream.TotalTime;
                }
            return result;

        }


        public  OffsetResult GetOffset(WaveFileReader stream1,ref OffsetResult lastResults)
        {
            OffsetResult currentProps = SpaceScan(stream1);
            currentProps.totalTime = stream1.TotalTime;


           var newStart = lastResults.totalTime.Subtract(lastResults.endSpace.Add(lastResults.endClip))
                .Subtract(lastResults.preSpace).Add(lastResults.newStart);
            if(lastResults.preSpace.Equals(TimeSpan.FromHours(2)))
            {
                newStart = TimeSpan.FromSeconds(2);
            }
            

            //var offset2 = stream1.TotalTime.Subtract(currentProps.preSpace);
           // offset2 = offset2.Subtract(currentProps.endSpace.Add(currentProps.endClip));
           
            stream1.Skip((int)-stream1.TotalTime.TotalSeconds);

           // WaveOffsetStream result = new WaveOffsetStream(stream1,newStart,currentProps.preSpace,currentProps.totalTime.Subtract(currentProps.endSpace.Add(currentProps.endClip)));
            currentProps.newStart = newStart;
            WaveOffsetStream result = new WaveOffsetStream(stream1, newStart, currentProps.preSpace, currentProps.totalTime);
            currentProps.newStart = newStart;
            currentProps.offset = result;
            lastResults = currentProps;
            return lastResults;


        }

        public  WaveOffsetStream GetTransitionTagOffset(WaveFileReader stream,TimeSpan startTime,int songPos)
        {
            WaveOffsetStream result;
            if (songPos == 0)
            {
                 result = new WaveOffsetStream(stream, TimeSpan.Zero, TimeSpan.Zero, stream.TotalTime);
            }
            else
            {
                 result = new WaveOffsetStream(stream, startTime.Subtract(TimeSpan.FromSeconds(2)), TimeSpan.Zero, stream.TotalTime);

            }
            

            return result;

        }

        public  WaveOffsetStream GetOverlayTagOffset(WaveFileReader stream, TimeSpan startTime, int songPos)
        {

            WaveOffsetStream result;
            
        
                result = new WaveOffsetStream(stream, startTime.Add(TimeSpan.FromMinutes(1)), TimeSpan.Zero, stream.TotalTime);

          


            return result;
        }

        public  TimeSpan FigureOverlayStartQuick(int tagorder, int numberOfOverlays, string localSongPath)
        {
            return FigureOutOverlayStartBasedOnCount(tagorder, numberOfOverlays, GetTotalLength(localSongPath).TotalMilliseconds);

        }
        public  TimeSpan GetOverlayStartTime(TimeSpan trackDuration, int position, int numOverlays)
        {
            var div = trackDuration.TotalMilliseconds / numOverlays;
            var start = (div * (position) - (div / 4));

            return TimeSpan.FromMilliseconds(start);

        }
        public  TimeSpan FigureOutOverlayStartBasedOnCount(int tagorder, int numberOfOverlays, double songDurationInMills)
        {
            var div = songDurationInMills / numberOfOverlays;
            var start = (div * (tagorder + 1) - (div / 4));

            return TimeSpan.FromMilliseconds(start);

        }

        public  List<Tuple<string, TimeSpan>> GetRandomOverlayList(int numOverlays, string trackPath, List<string> tags,TimeSpan songEnd,string useThisPath)
        {
            List<Tuple<string, TimeSpan>> result = new List<Tuple<string, TimeSpan>>();
            if (songEnd.TotalMilliseconds == 0)
            {
                trackPath = ConvertMp3ToWave(trackPath, true, useThisPath + @"\random_trackspace_check");

                using (var song = new WaveFileReader(trackPath))
                {
                    OffsetResult res = SpaceScan(song);

                    var length = res.endTime;


                    Random rand = new Random();
                    var chooseList = new List<int>();
                    for (var i = 0; i < numOverlays; i++)
                    {
                        double div = length.TotalMilliseconds / numOverlays;
                        double start = (div * (i + 1) - (div / 4));

                        var tagId = rand.Next(0, tags.Count - 1);
                        if (!chooseList.Contains(tagId) || (chooseList.Count >= tags.Count))
                        {
                            chooseList.Add(tagId);
                            result.Add(new Tuple<string, TimeSpan>(tags[tagId], TimeSpan.FromMilliseconds(start)));
                        }
                        else
                        {
                            i--;


                        }
                       
                      
                    }

                }
            }
            else
            {
                var length = songEnd;


                Random rand = new Random();
                var chooseList = new List<int>();
                for (var i = 0; i < numOverlays; i++)
                {
                    double div = length.TotalMilliseconds / numOverlays;
                    double start = (div * (i + 1) - (div / 4));

                    var tagId = rand.Next(0, tags.Count - 1);
                    if (!chooseList.Contains(tagId) || (chooseList.Count >= tags.Count))
                    {
                        chooseList.Add(tagId);
                        result.Add(new Tuple<string, TimeSpan>(tags[tagId], TimeSpan.FromMilliseconds(start)));
                    }
                    else
                    {
                        i--;


                    }
                }

            }
            
          

            return result;
        }
        public  string GetRandomTransitionTag(List<string> tagsToChooseFrom,Random rand)
        {
            
            return tagsToChooseFrom[rand.Next(0, tagsToChooseFrom.Count - 1)];

        }
        public struct MasterMixArgs
        {
            public int trackOrder { get; set; }
            public string trackPath { get; set; }
            public string preTagPath { get; set; }
            public TimeSpan endtime { get; set; }
            public double fadeOutDuration { get; set; }
            public bool fadeIn { get; set; }
            public double fadeInDuration { get; set; }
            public TimeSpan truncateStart { get; set; }
            public bool scanPretagsForSpace { get; set; }
            public bool scanOverlaysForSpace { get; set; }
            public TimeSpan preTagEarlystart { get; set; }
            public TimeSpan trackDelay { get; set; }
            public double preSongOverlapInMilliseconds { get; set; }
            public List<Tuple<string, TimeSpan>> overLayPathsAndStarts { get; set; }
            public string alternateEditsLocation { get; set; }
            public bool noFade { get; set; }


            public int songID { get; set; }

            public double mixtTapeTotalLength { get; set; }

            public double singleTagStartInSecs { get; set; }
        }
        public  class MasterMix
        {
            public  ITargetBlock<string> headBlock;
            public  CancellationTokenSource token;
            public enum TagType
            {
                transition,
                overlay,
                intro,
                outro,
                single

            }
            
            public class MixTag : IDisposable
            {
                public string path { get; set; }
                public TagType type { get; set; }
                public TimeSpan totalTime { get; set; }
                public TimeSpan startTime { get; set; }
                public int position { get; set; }
                public TimeSpan postsongAdjust { get; set; }
                public WaveFileReader stream { get; set; }
                public WaveOffsetStream offset { get; set; }
                public OffsetResult offres { get; set; }
                public TimeSpan overlayStart { get; set; }
                public double singleTagOffsetSeconds { get; set; }
                public MixTag(WaveAndMp3Manipulation obj, int pos,string tagPath, TagType tagType,TimeSpan delaySong,TimeSpan overlayStartTime, bool scan, double singleTagOffsetInSecs = 2)
                {
                    PARENT = obj;
                    path = obj.ConvertMp3ToWave(tagPath, false);
                    position = pos;
                    stream = new WaveFileReader(path);
                    type = tagType;
                    totalTime = stream.TotalTime;
                    singleTagOffsetSeconds = singleTagOffsetInSecs;
                    postsongAdjust = delaySong;

                    if (scan)
                    {
                        offres = obj.SpaceScan(stream);
                    }

                    //if (type == TagType.overlay)
                    //{
                    //    if (overlayStartTime.TotalMilliseconds == 0 )
                    //    {
                    //        throw new Exception("Overlay tag start time invalid!!!");
                    //    }
                    //    else
                    //    {
                    //      overlayStart = overlayStartTime;
                    //    }
                    //}
                    
                    
                }
                private WaveAndMp3Manipulation PARENT;
                public bool updateTimes(int pos,TimeSpan duration, int numOverlays, TimeSpan songStart,TimeSpan tagEarlyStart,TimeSpan delaySong)
                {

                    startTime = songStart.Subtract(tagEarlyStart);
                    if (type == TagType.transition)
                    {
                        
                        if (tagEarlyStart.TotalMilliseconds == 0)
                        {
                            startTime = songStart.Subtract(TimeSpan.FromMilliseconds(1000));
                        }
                        //postsongAdjust = TimeSpan.Zero.Subtract(TimeSpan.FromMilliseconds(2000));
                        //if (totalTime.TotalMilliseconds > 4000 )
                        //{
                        //    if (offres.endTime.TotalMilliseconds != 0)
                        //    {
                        //        postsongAdjust = offres.endTime.Subtract(TimeSpan.FromMilliseconds(2000)).Add(delaySong);
                        //    }
                        //    else
                        //    {
                        //        postsongAdjust = totalTime.Subtract(TimeSpan.FromSeconds(2)).Add(delaySong);

                        //    }
                            
                        //  }
                        if (delaySong.TotalMilliseconds != 0)
                        {
                            postsongAdjust = delaySong;
                        }
                    }else if(type == TagType.intro)
                    {
                        startTime = TimeSpan.Zero;
                        postsongAdjust = totalTime.Subtract(TimeSpan.FromSeconds(1)).Add(delaySong);

                    }
                    else if (type == TagType.single)
                    {
                        startTime = startTime.Add(TimeSpan.FromSeconds(singleTagOffsetSeconds));
                    }
                    else
                    {
                        overlayStart = PARENT.GetOverlayStartTime(duration, pos + 1, numOverlays);
                        startTime = startTime.Add(overlayStart);
                    }
                   

                    offset = new WaveOffsetStream(stream, startTime, offres.preSpace, offres.totalTime.TotalMilliseconds == 0 ? totalTime : offres.totalTime);
                    return true;
                }

                public void Dispose()
                {
                    stream.Dispose();
                }
            }
            
            public class MixSong : IDisposable
            {

                public MixSong(
                    WaveAndMp3Manipulation obj,
                    int songID,
                    string songPath,
                    int songPosition,
                    double mixTapeTotalLength,
                    MixTag tagBeforeSong,
                    TimeSpan tagEarlyStart,
                    MixSong previousTrack,
                    TimeSpan extraTimeBeforeTrackStarts,
                    TimeSpan timeToFade, //user defined
                    TimeSpan clipBeginnig, //user defined
                    double fadeDuration,
                    List<Tuple<string,TimeSpan>> overlays,
                    bool scanOverlays,
                    bool scanPretag,
                    bool fadeIn, 
                    double fadeInDuration ,
                    double preSongOverlapInMilliseconds,
                    string useThisPathForEdits = "",
                    bool noFade = false,
                    bool singleTagged = false,
                    double singleTaggedStartinSecs = 0)
                {
                    PARENT = obj;
                    overLays = new List<MixTag>();
                    mixTapeLength = mixTapeTotalLength;
                    singleTagStartInSecs = singleTaggedStartinSecs;
                    this.songID = songID;
                    if (clipBeginnig.TotalMilliseconds > 0)
                    {
                        string trimPath = PARENT.CreatePathFor("trim_wave",songPath,useThisDirectory: useThisPathForEdits);
                        if (songPath.Contains(".mp3"))
                        {
                            songPath = PARENT.ConvertMp3ToWave(songPath, true, useThisPathForEdits + @"\trim_beginning\");
                        }
                        WaveUtils.WavFileUtils.TrimFile(songPath, trimPath, clipBeginnig, TimeSpan.Zero,waveMan:PARENT);
                        songPath = trimPath;
                    }
                    cutOffTime = clipBeginnig.TotalMilliseconds;
                    path = PARENT.CleanAndFade(songPath, timeToFade, cutOffTime, false, fadeDuration, noFade, fadeIn, fadeInDuration, useThisPathForEdits,log:true,logSamplesOnFades:PARENT.LogFadeSamples,cleanOnly:noFade );
                    pos = songPosition;
                    if (tagBeforeSong != null)
                    {
                        preTag = tagBeforeSong;
                    }
                   
                    reader = new WaveFileReader(path);
                    offres = PARENT.SpaceScan(reader);
                    totalTime = reader.TotalTime.Subtract(offres.endSpace);
                    _fadeInDuration = fadeInDuration;
                    _fadeOutDuration = fadeDuration;
                    _fadeOutTime = timeToFade.Subtract(offres.preSpace).Subtract(TimeSpan.FromMilliseconds(_fadeOutDuration));
                    preSongOverlap = preSongOverlapInMilliseconds;
                    _tagEarlyStart = tagEarlyStart;
                    _delaySong = extraTimeBeforeTrackStarts;
                    if (previousTrack != null)
                    {
                        trackBefore = previousTrack;

                    }
                    if (overlays != null)
                    {
                        foreach (Tuple<string, TimeSpan> ovrlay in overlays)
                        {
                            overLays.Add(new MixTag(
                                PARENT,
                                overlays.IndexOf(ovrlay) + 1,
                                ovrlay.Item1,
                                singleTagged ? TagType.single : TagType.overlay,
                                TimeSpan.Zero,
                                ovrlay.Item2,
                                scanOverlays,
                                singleTagStartInSecs
                                ));


                        }
                    }
                    else
                    {

                    }
                   

                    
                    readjustTimes(TimeSpan.Zero,_tagEarlyStart,_delaySong);
                    
                }
                private WaveAndMp3Manipulation PARENT;
                public void readjustTimes(TimeSpan timeToAdd,TimeSpan tagEarlyStart,TimeSpan delaySong )
                {
                    
                    startTime = TimeSpan.Zero;
                    if (trackBefore != null)
                    {
                        startTime = trackBefore.stopTime;
                    }
                    if (delaySong.TotalMilliseconds != 0)
                    {
                        startTime = startTime.Add(delaySong);

                    }
                    
                    startTime = startTime.Add(timeToAdd);
                    if (preTag != null)
                    {
                        if (!preTag.updateTimes(0, totalTime, 0, startTime, tagEarlyStart, delaySong))
                        {
                            throw new Exception("Pretag " + preTag.path + " decided to skip update times called from " + path);
                        }
                        



                        startTime = startTime.Add(preTag.postsongAdjust);

                    }
                  

                    offset = new WaveOffsetStream(reader,startTime,offres.preSpace,totalTime);
                   // OffsetResult lastRes = new OffsetResult();
                    //if (trackBefore != null)
                    //{
                    //    lastRes = trackBefore.offres;
                    //}
                   
                    //offres = GetOffset(reader, ref lastRes);
                    //offset = offres.offset ;
                    foreach (MixTag ov in overLays)
                    {
                        ov.updateTimes(overLays.IndexOf(ov), offset.SourceLength, overLays.Count, startTime, TimeSpan.Zero, TimeSpan.Zero);
                    }

                    stopTime = startTime.Add(totalTime).Subtract(offres.preSpace).Subtract(TimeSpan.FromMilliseconds(preSongOverlap)).Subtract(TimeSpan.FromSeconds(1)) ;
                }

                public void PlugIntoMixer(WaveMixerStream32 mixer)
                {
                    WaveChannel32 mainChannel = new WaveChannel32(offset);
                    
                    mainChannel.PadWithZeroes = false;
                    if (preTag != null)
                    {
                        WaveChannel32 preTagChannel = new WaveChannel32(preTag.offset);
                        preTagChannel.PadWithZeroes = false;
                        mixer.AddInputStream(preTagChannel);
                    }
                   
                    mixer.AddInputStream(mainChannel);


                    foreach (MixTag overlay in overLays)
                    {
                        WaveChannel32 layChan = new WaveChannel32(overlay.offset);
                        layChan.PadWithZeroes = false;
                        mixer.AddInputStream(layChan);

                    }

                }
                public List<MixTag> overLays { get; set; }
                public MixTag preTag { get; set; }
                public MixSong trackBefore { get; set; }
                public string path { get; set; }
                public int pos { get; set; }
                public int songID { get; set; }
                public TimeSpan startTime { get; set; }
                public TimeSpan totalTime { get; set; }
                public TimeSpan stopTime { get; set; }
                private WaveFileReader reader { get; set; }
                public double preSongOverlap { get; set; }
                public OffsetResult offres { get; set; }
                public WaveOffsetStream offset { get; set; }
                private TimeSpan _tagEarlyStart { get; set; }
                private TimeSpan _delaySong { get; set; }
                private TimeSpan _fadeOutTime { get; set; }
                private double _fadeInDuration { get; set; }
                private double _fadeOutDuration { get; set; }
                private double singleTagStartInSecs { get; set; }
                public void Dispose()
                {
                    reader.Dispose();
                    foreach (MixTag tg in overLays)
                    {
                        tg.Dispose();
                    }
                    if (preTag != null)
                    {
                        preTag.Dispose();
                    }
                }

                public double cutOffTime { get; set; }

                public double mixTapeLength { get; set; }
            }

            public class MixTagTPL : IDisposable
            {

                public string path { get; set; }
                public TagType type { get; set; }
                public TimeSpan totalTime { get; set; }
                public TimeSpan startTime { get; set; }
                public int position { get; set; }
                public TimeSpan postsongAdjust { get; set; }
                public WaveFileReader stream { get; set; }
                public WaveOffsetStream offset { get; set; }
                private OffsetResult offres { get; set; }
                private TimeSpan overlayStart { get; set; }
                WaveAndMp3Manipulation PARENT { get; set; }
                public MixTagTPL(WaveAndMp3Manipulation obj, int pos, string tagPath, TagType tagType, TimeSpan delaySong, TimeSpan overlayStartTime, bool scan)
                {
                    PARENT = obj;
                    path = PARENT.ConvertMp3ToWave(tagPath, false);
                    position = pos;
                    stream = new WaveFileReader(path);
                    type = tagType;
                    totalTime = stream.TotalTime;
                    postsongAdjust = delaySong;

                    if (scan)
                    {
                        offres = PARENT.SpaceScan(stream);
                    }

                    //if (type == TagType.overlay)
                    //{
                    //    if (overlayStartTime.TotalMilliseconds == 0 )
                    //    {
                    //        throw new Exception("Overlay tag start time invalid!!!");
                    //    }
                    //    else
                    //    {
                    //      overlayStart = overlayStartTime;
                    //    }
                    //}


                }

                public void updateTimes(int pos, TimeSpan duration, int numOverlays, TimeSpan songStart, TimeSpan tagEarlyStart, TimeSpan delaySong)
                {

                    startTime = songStart.Subtract(tagEarlyStart);
                    if (type == TagType.transition)
                    {

                        if (tagEarlyStart.TotalMilliseconds == 0)
                        {
                            startTime = songStart.Subtract(TimeSpan.FromMilliseconds(1000));
                        }
                        //postsongAdjust = TimeSpan.Zero.Subtract(TimeSpan.FromMilliseconds(2000));
                        //if (totalTime.TotalMilliseconds > 4000 )
                        //{
                        //    if (offres.endTime.TotalMilliseconds != 0)
                        //    {
                        //        postsongAdjust = offres.endTime.Subtract(TimeSpan.FromMilliseconds(2000)).Add(delaySong);
                        //    }
                        //    else
                        //    {
                        //        postsongAdjust = totalTime.Subtract(TimeSpan.FromSeconds(2)).Add(delaySong);

                        //    }

                        //  }
                        if (delaySong.TotalMilliseconds != 0)
                        {
                            postsongAdjust = delaySong;
                        }
                    }
                    else if (type == TagType.intro)
                    {
                        startTime = TimeSpan.Zero;
                        postsongAdjust = totalTime.Subtract(TimeSpan.FromSeconds(1)).Add(delaySong);

                    }
                    else
                    {
                        overlayStart = PARENT.GetOverlayStartTime(duration, pos + 1, numOverlays);
                        startTime = startTime.Add(overlayStart);
                    }


                    offset = new WaveOffsetStream(stream, startTime, offres.preSpace, offres.totalTime.TotalMilliseconds == 0 ? totalTime : offres.totalTime);

                }

                public void Dispose()
                {
                    stream.Dispose();
                }
            }


            
            public class MixSongTPL : IDisposable
            {

                public MixSongTPL(
                    WaveAndMp3Manipulation obj,
                    int songID,
                    string songPath,
                    int songPosition,
                    double mixTapeTotalLength,
                    MixTagTPL tagBeforeSong,
                    TimeSpan tagEarlyStart,
                    MixSongTPL previousTrack,
                    TimeSpan extraTimeBeforeTrackStarts,
                    TimeSpan timeToFade, //user defined
                    TimeSpan clipBeginnig, //user defined
                    double fadeDuration,
                    List<Tuple<string, TimeSpan>> overlays,
                    bool scanOverlays,
                    bool scanPretag,
                    bool fadeIn,
                    double fadeInDuration,
                    double preSongOverlapInMilliseconds,
                    string useThisPathForEdits = "",
                    bool noFade = false)
                {
                    PARENT = obj;
                    overLays = new List<MixTagTPL>();
                    mixTapeLength = mixTapeTotalLength;
                    this.songID = songID;
                    path = songPath;
                    //if (clipBeginnig.TotalMilliseconds > 0)
                    //{
                    //    string trimPath = CreatePathFor("trim_wave", songPath, useThisDirectory: useThisPathForEdits);
                    //    if (songPath.Contains(".mp3"))
                    //    {
                    //        songPath = ConvertMp3ToWave(songPath, true, useThisPathForEdits + @"\trim_beginning\");
                    //    }
                    //    WaveUtils.WavFileUtils.TrimFile(songPath, trimPath, clipBeginnig, TimeSpan.Zero);
                    //    songPath = trimPath;
                    //}
                   
                    //path = CleanAndFade(songPath, timeToFade, cutOffTime, false, fadeDuration, noFade, fadeIn, fadeInDuration, useThisPathForEdits);
                   
                    if (tagBeforeSong != null)
                    {
                        preTag = tagBeforeSong;
                    }
                    pos = songPosition;
                    cutOffTime = clipBeginnig.TotalMilliseconds;
                    reader = new WaveFileReader(path);
                    offres = PARENT.SpaceScan(reader);
                    totalTime = reader.TotalTime.Subtract(offres.endSpace);
                    _fadeInDuration = fadeInDuration;
                    _fadeOutDuration = fadeDuration;
                    _fadeOutTime = timeToFade.Subtract(offres.preSpace).Subtract(TimeSpan.FromMilliseconds(_fadeOutDuration));
                    preSongOverlap = preSongOverlapInMilliseconds;
                    _tagEarlyStart = tagEarlyStart;
                    _delaySong = extraTimeBeforeTrackStarts;
                    if (previousTrack != null)
                    {
                        trackBefore = previousTrack;

                    }
                    foreach (Tuple<string, TimeSpan> ovrlay in overlays)
                    {
                        overLays.Add(new MixTagTPL(
                            PARENT,
                            overlays.IndexOf(ovrlay) + 1,
                            ovrlay.Item1,
                            TagType.overlay,
                            TimeSpan.Zero,
                            ovrlay.Item2,
                            scanOverlays));


                    }

                    readjustTimes(TimeSpan.Zero, _tagEarlyStart, _delaySong);

                }
                private WaveAndMp3Manipulation PARENT;
                public void readjustTimes(TimeSpan timeToAdd, TimeSpan tagEarlyStart, TimeSpan delaySong)
                {

                    startTime = TimeSpan.Zero;
                    if (trackBefore != null)
                    {
                        startTime = trackBefore.stopTime;
                    }
                    if (delaySong.TotalMilliseconds != 0)
                    {
                        startTime = startTime.Add(delaySong);

                    }

                    startTime = startTime.Add(timeToAdd);
                    if (preTag != null)
                    {
                        preTag.updateTimes(0, totalTime, 0, startTime, tagEarlyStart, delaySong);



                        startTime = startTime.Add(preTag.postsongAdjust);

                    }


                    offset = new WaveOffsetStream(reader, startTime, offres.preSpace, totalTime);
                    // OffsetResult lastRes = new OffsetResult();
                    //if (trackBefore != null)
                    //{
                    //    lastRes = trackBefore.offres;
                    //}

                    //offres = GetOffset(reader, ref lastRes);
                    //offset = offres.offset ;
                    foreach (MixTagTPL ov in overLays)
                    {
                        ov.updateTimes(overLays.IndexOf(ov), offset.SourceLength, overLays.Count, startTime, TimeSpan.Zero, TimeSpan.Zero);
                    }

                    stopTime = startTime.Add(totalTime).Subtract(offres.preSpace).Subtract(TimeSpan.FromMilliseconds(preSongOverlap)).Subtract(TimeSpan.FromSeconds(1));
                }

                public void PlugIntoMixer(WaveMixerStream32 mixer)
                {
                    WaveChannel32 mainChannel = new WaveChannel32(offset);

                    mainChannel.PadWithZeroes = false;
                    if (preTag != null)
                    {
                        WaveChannel32 preTagChannel = new WaveChannel32(preTag.offset);
                        preTagChannel.PadWithZeroes = false;
                        mixer.AddInputStream(preTagChannel);
                    }

                    mixer.AddInputStream(mainChannel);


                    foreach (MixTagTPL overlay in overLays)
                    {
                        WaveChannel32 layChan = new WaveChannel32(overlay.offset);
                        layChan.PadWithZeroes = false;
                        mixer.AddInputStream(layChan);

                    }

                }
                public List<MixTagTPL> overLays { get; set; }
                public MixTagTPL preTag { get; set; }
                public MixSongTPL trackBefore { get; set; }
                public string path { get; set; }
                public int pos { get; set; }
                public int songID { get; set; }
                public TimeSpan startTime { get; set; }
                public TimeSpan totalTime { get; set; }
                public TimeSpan stopTime { get; set; }
                private WaveFileReader reader { get; set; }
                public double preSongOverlap { get; set; }
                private OffsetResult offres { get; set; }
                private WaveOffsetStream offset { get; set; }
                private TimeSpan _tagEarlyStart { get; set; }
                private TimeSpan _delaySong { get; set; }
                private TimeSpan _fadeOutTime { get; set; }
                private double _fadeInDuration { get; set; }
                private double _fadeOutDuration { get; set; }
                public void Dispose()
                {
                    reader.Dispose();
                    foreach (MixTagTPL tg in overLays)
                    {
                        tg.Dispose();
                    }
                    if (preTag != null)
                    {
                        preTag.Dispose();
                    }
                }

                public double cutOffTime { get; set; }

                public double mixTapeLength { get; set; }
            }

            public MixSong SimpleTagTrack(WaveAndMp3Manipulation parent, MasterMixArgs trackWithTag, string finalWavePath)
            {
                var tag = trackWithTag.overLayPathsAndStarts[0];
                var result = new MixSong(parent,
                    trackWithTag.songID,
                    trackWithTag.trackPath,
                    0,
                    trackWithTag.mixtTapeTotalLength,
                    null,
                    trackWithTag.preTagEarlystart,
                    null,
                    TimeSpan.Zero,
                    trackWithTag.endtime,
                    TimeSpan.Zero,
                    trackWithTag.fadeOutDuration,
                    trackWithTag.overLayPathsAndStarts,
                    trackWithTag.scanOverlaysForSpace,
                    trackWithTag.scanPretagsForSpace,
                    trackWithTag.fadeIn,
                    trackWithTag.fadeInDuration,
                    0,
                    trackWithTag.alternateEditsLocation,
                    true,
                    true,
                    trackWithTag.singleTagStartInSecs);
                    
                   var mixer = new WaveMixerStream32();



                
                    result.PlugIntoMixer(mixer);

              

                WaveFileWriter.CreateWaveFile(finalWavePath, new Wave32To16Stream(mixer));

               
                    result.Dispose();
               

                return result;

            

                    

            }

            public  List<MixSong> CreateMasterMix(WaveAndMp3Manipulation parent, List<MasterMixArgs> trackListWithTags,string IntroTag,string finalWavePath)
            {

                


                parent.RaiseEvent("Starting New MasterMix!!!");
                var i = 0;
                var obj = this;
                Tracks = new List<MixSong>();
                foreach (MasterMixArgs args in trackListWithTags)
                {
                    //if (args.fadeInDuration > (args.endtime.Subtract(TimeSpan.FromMilliseconds(10000)).TotalMilliseconds))
                    //{
                    //    throw new Exception("Fade in duration too long...");
                    //}
                    //if(args.truncateStart.TotalMilliseconds > args.endtime.TotalMilliseconds){
                    //    throw new Exception("Truncate start too late...");
                    //}
                    parent.RaiseEvent("Creating mixsong for " + args.trackPath);
                    var tagtype = TagType.transition;
                    if(args.trackOrder == 0) {
                        tagtype = TagType.intro;
                    }
                    MixSong prevTrack = null;
                    if(i > 0) {

                        prevTrack = Tracks[i - 1];
                    }
                    if(Tracks.Count == 0) {
                         MasterMix.MixSong track = new MixSong(
                            parent,
                             args.songID,
                        args.trackPath,
                        trackListWithTags.IndexOf(args),
                        args.mixtTapeTotalLength,
                        new MixTag(parent,0, IntroTag, tagtype, args.trackDelay, TimeSpan.Zero, args.scanPretagsForSpace),
                        args.preTagEarlystart,
                        prevTrack,
                        args.trackDelay,
                        args.endtime,
                        args.truncateStart,
                        args.fadeOutDuration,
                        args.overLayPathsAndStarts,
                        args.scanOverlaysForSpace,
                        args.scanPretagsForSpace,
                        args.fadeIn,
                        args.fadeInDuration,
                        args.preSongOverlapInMilliseconds,
                        args.alternateEditsLocation,
                        args.noFade);


                    Tracks.Add(track);
                    parent.RaiseEvent(args.ToString());
                    }else{
                            MasterMix.MixSong track = new MixSong(
                                parent,
                                args.songID,
                        args.trackPath,
                        trackListWithTags.IndexOf(args),
                        args.mixtTapeTotalLength,
                      args.preTagPath == "" ? null :  new MixTag(parent,0,args.preTagPath, tagtype, args.trackDelay, TimeSpan.Zero, args.scanPretagsForSpace),
                        args.preTagEarlystart,
                        prevTrack,
                        args.trackDelay,
                        args.endtime,
                        args.truncateStart,
                        args.fadeOutDuration,
                        args.overLayPathsAndStarts,
                        args.scanOverlaysForSpace,
                        args.scanPretagsForSpace,
                        args.fadeIn,
                        args.fadeInDuration,
                        args.preSongOverlapInMilliseconds,
                        args.alternateEditsLocation,
                        args.noFade);


                    Tracks.Add(track);

                    }

                   
                        
                        i++;
                }

                var mixer = new WaveMixerStream32();



                foreach (MixSong trk in Tracks)
                {
                    trk.PlugIntoMixer(mixer);

                }
               parent.RaiseEvent(string.Format("WRITING FINAL WAVE to {0}...{1}", finalWavePath, Environment.NewLine));
                WaveFileWriter.CreateWaveFile(finalWavePath, new Wave32To16Stream(mixer));
                parent.RaiseEvent("Disposing...");
                foreach (MixSong trk in Tracks)
                {
                    trk.Dispose();
                }
                
                return Tracks;

            }

            public int ConvertAndTrimAudioFile(string originalFilePath, string destinationFilePath,WaveAndMp3Manipulation waveMan,string editWorkPath)
            {
                string editsPath = waveMan .CreatePathFor ("cleanup",originalFilePath ,useThisDirectory:editWorkPath );
                if(originalFilePath .Contains (".mp3")){
                    originalFilePath = waveMan .ConvertMp3ToWave (originalFilePath );
                }
                if (System.IO.File.Exists(destinationFilePath))
                {
                    System.IO.File.Delete(destinationFilePath);
                }
                var mixsong = new MixSong(waveMan, 0, originalFilePath, 0, 0, null, TimeSpan.Zero, null, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero, 0, null, false, false, false, 0, 0, editWorkPath,noFade:true);
                var mixer = new NAudio.Wave.WaveMixerStream32();
                mixsong.PlugIntoMixer(mixer);
                WaveFileWriter.CreateWaveFile(editsPath, new Wave32To16Stream(mixer));
                if (LemsUTools.WaveUtils.WavFileUtils.TrimFile(editsPath, destinationFilePath, TimeSpan.Zero, TimeSpan.Zero,waveMan:waveMan))
                {

                    System.IO.File.Delete(editsPath);
                    var mp3Dir = System.IO.Directory.GetParent(destinationFilePath).FullName ;
                    foreach (string wave in System.IO.Directory.GetFiles(mp3Dir))
                    {
                        if(wave.Contains(".wav"))
                        System.IO.File.Delete(wave);
                    }
                   
                    return (int) mixsong .offset .TotalTime .TotalMilliseconds ;
                }
                return 0;
            }

            public  void CreateMasterMixAsync(List<MasterMixArgs> trackListWithTags, string IntroTag, string finalWavePath, string useThisPath = "")
            {

              //  BeginMasterMixAsync(trackListWithTags, new CancellationToken(), IntroTag, finalWavePath, useThisPath);

            }


            public  List<MixSongTPL> FinishMasterMix(WaveAndMp3Manipulation parent, List<MasterMixArgs> trackListWithTags, string IntroTag, string finalWavePath,string useThisPath = "")
            {


               





                var i = 0;
                TracksTPL = new List<MixSongTPL>();
                foreach (MasterMixArgs args in trackListWithTags)
                {
                    var tagtype = TagType.transition;
                    if (args.trackOrder == 0)
                    {
                        tagtype = TagType.intro;
                    }
                    MixSongTPL prevTrack = null;
                    if (i > 0)
                    {
                        prevTrack = TracksTPL[i - 1];
                    }
                    if (TracksTPL.Count == 0)
                    {
                        MasterMix.MixSongTPL track = new MixSongTPL(
                            parent,
                            args.songID,
                       args.trackPath,
                       trackListWithTags.IndexOf(args),
                       args.mixtTapeTotalLength,
                       new MixTagTPL(parent,0, IntroTag, tagtype, args.trackDelay, TimeSpan.Zero, args.scanPretagsForSpace),
                       args.preTagEarlystart,
                       prevTrack,
                       args.trackDelay,
                       args.endtime,
                       args.truncateStart,
                       args.fadeOutDuration,
                       args.overLayPathsAndStarts,
                       args.scanOverlaysForSpace,
                       args.scanPretagsForSpace,
                       args.fadeIn,
                       args.fadeInDuration,
                       args.preSongOverlapInMilliseconds,
                       args.alternateEditsLocation,
                       args.noFade);


                        TracksTPL.Add(track);
                    }
                    else
                    {
                        MasterMix.MixSongTPL track = new MixSongTPL(
                            parent,
                            args.songID,
                    args.trackPath,
                    trackListWithTags.IndexOf(args),
                    args.mixtTapeTotalLength,
                  args.preTagPath == "" ? null : new MixTagTPL(parent,0, args.preTagPath, tagtype, args.trackDelay, TimeSpan.Zero, args.scanPretagsForSpace),
                    args.preTagEarlystart,
                    prevTrack,
                    args.trackDelay,
                    args.endtime,
                    args.truncateStart,
                    args.fadeOutDuration,
                    args.overLayPathsAndStarts,
                    args.scanOverlaysForSpace,
                    args.scanPretagsForSpace,
                    args.fadeIn,
                    args.fadeInDuration,
                    args.preSongOverlapInMilliseconds,
                    args.alternateEditsLocation,
                    args.noFade);


                        TracksTPL.Add(track);

                    }



                    i++;
                }

                var mixer = new WaveMixerStream32();



                foreach (MixSongTPL trk in TracksTPL)
                {
                    trk.PlugIntoMixer(mixer);

                }

                WaveFileWriter.CreateWaveFile(finalWavePath, new Wave32To16Stream(mixer));

                foreach (MixSongTPL trk in TracksTPL)
                {
                    trk.Dispose();
                }

                return TracksTPL;

            }


            



            public  List<MixSong> Tracks { get; set; }
            public  List<MixSongTPL> TracksTPL { get; set; }

        }
        //public static void testBase()
        //{
        //    var wavePaths = new List<string>();

        //    var mp3list = System.IO.Directory.GetFiles(@"K:\wamp\www\mp3\Hump", "*.mp3", System.IO.SearchOption.AllDirectories);
        //    var rand = new Random();
        //    var templist = new List<string>();
        //    for (var i = 0; i < 1; i++)
        //    {

        //        //  templist.Add(mp3list[rand.Next(mp3list.Count() - 1)]);
        //        templist.Add(mp3list[0]);




        //    }


        //    var tempPath = @"K:\wamp\www\wave_manipulation_temp\";
        //   // string song3 = @"K:\wamp\www\mp3\City Boy\To Ligit.mp3";
        //    // wavePaths.Add(WaveAndMp3Manipulation.ConvertMp3ToWave(song3));
        //    foreach (string sng in templist)
        //    {

        //        wavePaths.Add(WaveAndMp3Manipulation.ConvertMp3ToWave(sng));

        //    }
        //    var cnt = 0;
        //    var results = new WaveAndMp3Manipulation.OffsetResult();
        //    WaveMixerStream32 mixer = new WaveMixerStream32();
        //    var startsArray = new List<TimeSpan>();
        //    var offsets = new List<WaveOffsetStream>();
        //    foreach (string wave in wavePaths)
        //    {
        //        var fadeWave = tempPath + "fade" + cnt++ + ".wav";

        //        if (WaveAndMp3Manipulation.FadeWave(wave, fadeWave, TimeSpan.Zero, 10000))
        //        {
        //            WaveFileReader red = new WaveFileReader(fadeWave);
        //            results = WaveAndMp3Manipulation.GetOffset(red, results);
        //            startsArray.Add(results.newStart);
        //            offsets.Add(results.offset);
        //            WaveChannel32 channel = new WaveChannel32(results.offset);
        //            channel.PadWithZeroes = true;
        //            mixer.AddInputStream(channel);


        //        }






        //    }
        //    // mixer.AutoStop = true;

        //    WaveFileWriter.CreateWaveFile(@"K:\wamp\www\mp3\rocco.wav", new Wave32To16Stream(mixer));
        //}
    }

    
}
