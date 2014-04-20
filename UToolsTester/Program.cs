using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace UToolsTester
{
    class Program
    {
        static void Main(string[] args)
        {
            var wavpath = @"K:\wamp\www\mp3\Pedagree\Onswoll.wav";
            var wavpath2 = @"C:\Users\BricklyfeA\Documents\Visual Studio 2012\Projects\UntameMusic2014\UntameMusic2014\MixTapes\wav\Pedagree\Very True-version15.wav";
            var mp3path = @"K:\wamp\www\testmp37" + DateTime.Now.Ticks + ".mp3";
            var mp3path2 = @"K:\wamp\www\testmp37635241857333138592.mp3";

            var imgpath = @"C:\Users\BricklyfeA\Documents\Visual Studio 2012\Projects\UntameMusic2014\UntameMusic2014\ArtistPhotos\8\thumbs(150w)\87.jpg";
            //LemsUTools.Mp3Utils.testBase();
           // LemsUTools.WaveAndMp3Manipulation.testBase();
           // LemsUTools.Mp3Utils.test2();
          //  byte[] bytes = LemsUTools.WaveUtils.WavFileUtils.TrimWavFileDB(@"K:\wamp\www\mp3\Pedagree\Onswoll.wav", TimeSpan.FromMilliseconds(1000), TimeSpan.FromMilliseconds(1000));
            //if (System.IO.File.Exists(@"K:\wamp\www\testmp37.mp3"))
            //{
            //    System.IO.File.Delete(@"K:\wamp\www\testmp37.mp3");
            //}
           // LemsUTools.WaveUtils.WavFileUtils.TrimFile(wavpath2, mp3path, TimeSpan.FromMilliseconds(10000), TimeSpan.FromMilliseconds(30000));
          //  LemsUTools.WaveUtils.W  avFileUtils.TrimFile(wavpath, mp3path, TimeSpan.FromMilliseconds(10000), TimeSpan.FromMilliseconds(10000));

            //LemsUTools.Mp3Utils.testMasterMix();
           // Console.ReadLine();
            LemsUTools.Mp3Utils.testConverAndTrimAudioFile();

           //var pc = new LemsUTools.PhotosAndImagingCenter();
           //System.Drawing.Image img = System.Drawing.Image.FromFile(imgpath);
           //var img = pc.
           //LemsUTools.Mp3Utils.Mp3Helpers.AddPictureToTag(mp3path2, pc.GetBytesFromImage(img));
                Console.WriteLine("");
            //LemsUTools.Mp3Utils.testMasterMix();
        }
    }
}
