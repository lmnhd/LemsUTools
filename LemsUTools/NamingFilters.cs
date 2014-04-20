using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.VisualBasic.FileIO;
using Microsoft.VisualBasic.MyServices;
using Microsoft.VisualBasic.Devices;
using LemsDotNetHelpers;
using UntameVBUtils;




namespace LemsUTools
{
    public class NamingFilters
    {
        private List<VersioningArgs> Competitors;
        public struct MixStatus
        {
            public enum Status
            {
                NotMixed,
                PreMixed,
                FinalMixed,
                ReMixed,
                Instrumental,
                Accapella,
                ChoppedAndScrewed,
                Specialty
            }
            public Status status { get; set; }

            public string MixedBy { get; set; }
            public DateTime DateMixed { get; set; }
            public string[] ComparingStringArray { get; set; }
            

        }
        
        public NamingFilters(List<VersioningArgs> allNamingCompetitors)
        {
            Competitors = allNamingCompetitors;
            
        }
        public  MixStatus OrganizeMixStatus(string songName)
        {
            string[] names;
            List<string> allnames = new List<string>();
            var result = songName;
            names = new string[5] { " hook", " hookonly ", " nohook ", " w/", " w/0 " };
            allnames.AddRange(names);
            foreach (string name in names)
            {
                if (songName.ToLower().Contains(name))
                {
                    return new MixStatus { status = MixStatus.Status.Specialty ,ComparingStringArray = names};
                }
            }
            names = new string[4] { " chopped and"," screwed", "_screwed", " screwd" };
            allnames.AddRange(names);
            foreach (string name in names)
            {
                if (songName.ToLower().Contains(name))
                {
                    return new MixStatus { status = MixStatus.Status.ChoppedAndScrewed, ComparingStringArray = names };
                }
            }
            names = new string[2] { " acca", "_acca" };
            allnames.AddRange(names);
            foreach (string name in names)
            {
                if (songName.ToLower().Contains(name))
                {
                    return new MixStatus { status = MixStatus.Status.Accapella, ComparingStringArray = names };
                }
            }
            names = new string[3] { " inst ", "_inst", "instrumental" };
            allnames.AddRange(names);
            foreach (string name in names)
            {
                if (songName.ToLower().Contains(name))
                {
                    return new MixStatus { status = MixStatus.Status.Instrumental, ComparingStringArray = names };
                }
            }
            names = new string[4] { "remix", " (remix) ", " re-mix ", " re mix " };
            allnames.AddRange(names);
            foreach (string name in names)
            {
                if (songName.ToLower().Contains(name))
                {
                    return new MixStatus { status = MixStatus.Status.ReMixed, ComparingStringArray = names };
                }
            }
            names = new string[3] { "premix", "pre-mix", "pre mix" };
            allnames.AddRange(names);
            foreach (string name in names)
            {
                if (songName.ToLower().Contains(name))
                {
                    return new MixStatus { status = MixStatus.Status.PreMixed, ComparingStringArray = names };
                }
            }
            names = new string[5] { "_fnl", "_lj", "_final", "_ljuiced", "_lemonjuiced" };
            allnames.AddRange(names);
            foreach (string name in names)
            {
                if (songName.ToLower().Contains(name))
                {
                    return new MixStatus { status = MixStatus.Status.FinalMixed, ComparingStringArray = names };
                }
            }
            names = new string[8] { "unmix", "unmixed", "un mix", "un mixed", "un-mix","unfinish","un finish","un-fin" };
            allnames.AddRange(names);
            foreach (string name in names)
            {
                if (songName.ToLower().Contains(name))
                {
                    return new MixStatus { status = MixStatus.Status.NotMixed, ComparingStringArray = names };
                }
            }

            return new MixStatus { status = MixStatus.Status.NotMixed };
        }

        public string GetFinalFormattedTitle(int songID)
        {

            VersioningArgs ve = Competitors.Find(v => v.ID == songID);
            string result = CleanUpForCommercial(ve.SongName);
            if (ve.FeaturedArtists != null && ve.FeaturedArtists.Count > 0)
            {
                
                result = NamingConventions.addFeatureText(result, ve.FeaturedArtists,ve.ArtistName);

            }
            return result;
        }

        public string GetCommercialFormattedTitle(int songID)
        {
            VersioningArgs ve = Competitors.Find(v => v.ID == songID);
            
            string result = CleanUpForCommercial(ve.SongName,true);
            if (ve.FeaturedArtists != null && ve.FeaturedArtists.Count > 0)
            {
                
                result = NamingConventions.addFeatureText(result, ve.FeaturedArtists,ve.ArtistName);

            }
            return string.Format("{0} - {1}",ve.ArtistName,result);
        }

        public string GetShortFreindlyTitle(int songID,bool includeVersionInfo)
         {
            var ver = Competitors.Find(v => v.ID == songID);

            string result = CleanUpForCommercial( ver.SongName );
            if(includeVersionInfo) {
                result = string.Format("{0} {1}", result, GetSongVersionTag(ver, false));
            }
            return result;
        
        }
        public class VersioningArgs : IComparable<VersioningArgs>
        {
            public string SongName { get; set; }
            public DateTime OriginiationDate { get; set; }
            public int ID { get; set; }
            public int Version { get; set; }
            public string ArtistName { get; set; }
            public List<string> FeaturedArtists { get; set; }

            public int CompareTo(VersioningArgs obj)
            {
                return -DateTime.Compare(this.OriginiationDate, obj.OriginiationDate);
            }

            public int Copies { get; set; }
        }
        /// <summary>
        /// Returns the version string for the specified song
        /// </summary>
        /// <param name="song"></param>
        /// <returns></returns>
        public string GetSongVersionTag(VersioningArgs song, bool capitalize = true)
        {
            if (Competitors.Find(c => c.ID == song.ID).Version > 0)
            {
                return CreateVersionString(Competitors.Find(c => c.ID == song.ID), capitalize);
            }
            InsideAndOutResult completeCheck = CompareInsideAndOut(song.SongName, GetCompetitorNamesAsArray());
            var testString = CleanUpForComparison(song.SongName);
            testString = RemoveMixTags(testString);
            List<VersioningArgs> copyCats = new List<VersioningArgs>();
            copyCats.Add(song);
            foreach (VersioningArgs args in Competitors)
            {
                var test = CleanUpForComparison(args.SongName);
                test = RemoveMixTags(test, false);

                if (test == testString)
                {
                    var check = copyCats.Find(v => v.OriginiationDate == args.OriginiationDate);
                    if (check == null)
                    {
                        copyCats.Add(args);
                    }


                }
                else if (completeCheck.result)
                {

                    if (IsTrickySameName(args.SongName, completeCheck.matches[1]))
                    {

                        
                        copyCats.Add(args);
                    }

                }
            }
                if (copyCats.Count > 1)
                {
                    copyCats.Sort();
                    copyCats.Reverse();
                    foreach (VersioningArgs a in copyCats)
                    {
                        a.Version = copyCats.IndexOf(a) + 1;
                        a.Copies = copyCats.Count;
                        if (a.ID == song.ID)
                        {
                            song.Version = a.Version;
                        }
                    }
                    copyCats.Reverse();
                    RefreshCompetitors(copyCats);

                    return CreateVersionString(song, capitalize);


                }
                else
                {

                    return "";
                }

            
        }

        public string GetFeaturesString(List<string> features,string owner = "")
        {
            return NamingConventions.GetFeatureText(features,owner);


        }
        //private bool CheckIfNameIsTheSame()
        //{

        private bool GradeInsideAndOutTestResults(List<InsideAndOutResult> testResults)
        {
            var flse = false;
            var tru = false;
            foreach (InsideAndOutResult res in testResults)
            {
                if (!res.result)
                {
                    flse = true;
                }
                else
                {

                    tru = true;
                }
            }
            return flse = tru;

        }

        //}
        /// <summary>
        /// Use this one last...
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        /// 
        private string[] GetCompetitorNamesAsArray()
        {
            List<string> result = new List<string>();
            foreach (VersioningArgs arg in Competitors)
            {
                result.Add(arg.SongName);
            }
            return result.ToArray();
        }
        private string RemoveUnderScores(string name)
        {
            return DamnDat.StringSplit(name, "_")[0];
        }
        public string ReplaceHyphens(string name)
        {
            return name.Replace("'", "`");
        }
        
        private struct InsideAndOutResult
        {
           public  bool result {get; set;}
          public  string[] matches {get; set;}
        }
        private InsideAndOutResult CompareInsideAndOut(string mainName,string[] namesToCompare)
        {
            string temp = CleanUpForComparison(mainName);
            foreach (string name in namesToCompare)
            {
                string tmp = CleanUpForComparison(name);
                if (temp.Equals(name))
                {
                    return new InsideAndOutResult { result = true, matches = new string[] { temp, name } };
                }
            }
            return new InsideAndOutResult { result = false };

        }
        private bool IsTrickySameName(string name1,string name2)
        {
            string temp1 = RemoveParenthesisTags(name1,false);
            temp1 = RemoveUnderScores(temp1);

            string temp2 = RemoveParenthesisTags(name2,false);
            temp2 = RemoveUnderScores(temp2);


            string[] breakup1 = DamnDat.StringSplit(temp1, " ");
            string[] breakup2 = DamnDat.StringSplit(temp2, " ");

            foreach (VersioningArgs arg in Competitors)
            {
                if (breakup1.Length > 0 && breakup2.Length > 0 && breakup2.Length == breakup1.Length)
                {
                    // List<Tuple<string,bool>> results1 = new List<Tuple<string,bool>>();
                    List<InsideAndOutResult> testResults = new List<InsideAndOutResult>();
                    for (var i = 0; i < breakup1.Length; i++)
                    {
                        var test1 = breakup1[i];
                        var test2 = breakup2[i];
                        var test = CompareInsideAndOut(test1, new string[1] { test2 });
                        testResults.Add(test);
                    }

                    
                    return GradeInsideAndOutTestResults(testResults);
                }
                else if (breakup1.Length > 0 && breakup2.Length > 0 && breakup2.Length != breakup1.Length)
                {
                    List<InsideAndOutResult> testResults = new List<InsideAndOutResult>();
                    var sizecheck = breakup1.Length > breakup2.Length;
                    var smallerArray = new List<string>();
                    var largerArray = new List<string>();
                    if (sizecheck)
                    {
                        largerArray = breakup1.ToList();
                        smallerArray = breakup2.ToList();
                    }
                    else
                    {
                        largerArray = breakup2.ToList();
                        smallerArray = breakup1.ToList();

                    }
                    if (largerArray.Count < 3)
                    {
                        return false;
                    }
                    foreach (string tester1 in smallerArray)
                    {
                        testResults.Add(CompareInsideAndOut(tester1, largerArray.ToArray()));
                    }
                    return GradeInsideAndOutTestResults(testResults);

                }
                else
                {
                    return CompareInsideAndOut(name1, new string[1] { name2 }).result;

                }

            } return false;
        }
        private void RefreshCompetitors(List<VersioningArgs> updates)
        {
            foreach (VersioningArgs v in updates)
            {
                Competitors.Find(c => c.ID == v.ID).Version = v.Version;
            }
        }
        private string CreateVersionString(VersioningArgs song,bool capitalize)
        {
            var resultstart = "-Version ";
            if (!capitalize)
            {
                resultstart = "-version ";
            }
            return String.Format("{0}{1} of {2}", resultstart, song.Version,song.Copies);

        }
        private string CleanUpForComparison(string name)
        {
            var temp = name;
            temp = RemoveDigits(temp);
            temp = NamingConventions.RemoveFeatureText(temp);
            temp = RemoveParenthesisTags(temp,false);
           
            temp = RemoveAbstractCharacters(temp);
            temp = NamingConventions.removeNumbers(temp);
            temp = RemoveCopys(temp);
            temp = NamingConventions.removeSpaces(temp).ToLower();
            return temp;

        }
        private string RemoveAbstractCharacters(string name)
        {

            string temp = DamnDat.StringSplit(name,"_")[0];
            temp = DamnDat.StringSplit(temp,"-")[0];
            temp = DamnDat.StringSplit(temp, ".")[0];
            return temp;
            
        }
        private string RemoveParenthesisTags(string name,bool cleanOrphansOnly)
        {
            string temp = name;
            if (cleanOrphansOnly)
            {
                if(temp.EndsWith("(") || (temp.Contains("(") && !temp.Contains(")")) || (temp.Contains(")") && !temp.Contains("(")) )
                {
                    if (temp.Contains("("))
                    {
                        return DamnDat.StringSplit(temp, "(")[0];
                    }
                    else
                    {

                        return DamnDat.StringSplit(temp, ")")[0];
                    }
                }
                return name;
            }
            
            temp = DamnDat.StringSplit(name, "(")[0];
            return temp;
        }
        private string RemoveMixTags(string name,bool onlyFinalMixes = true)
        {
            MixStatus stat = OrganizeMixStatus(name);
            if (stat.status != MixStatus.Status.NotMixed)
            {

                if (onlyFinalMixes && (stat.status == MixStatus.Status.FinalMixed || stat.status == MixStatus.Status.PreMixed))
                {
                    List<string> names = stat.ComparingStringArray.ToList();
                    foreach (string nam in names)
                    {
                        if (name.ToLower().Contains(nam.ToLower()))
                        {
                            var temp = DamnDat.StringSplit(name.ToLower(), nam)[0].ToLower();
                            return NamingConventions.ConverToTitleCase(temp);

                        }
                    }
                }
            }else
            {
             string[]   names = new string[8] { "unmix", "unmixed", "un mix", "un mixed", "un-mix","unfinish","un finish","un-fin" };
             for (var k = 0; k < names.Length; k++)
             {
                 if(name.ToLower().Contains(names[k]))
                 {
                     var temp = DamnDat.StringSplit(name.ToLower(), names[k]);
                     return NamingConventions.ConverToTitleCase(temp[0]);
                 }
             }

            }
            return name;

        }
        private int GetMixdownType(string name)
        {
            return (int)OrganizeMixStatus(name).status;

        }
        private string RemoveCopys(string name)
        {
            return DamnDat.StringSplit(name, "Copy")[0];
        }
        private string CleanUpForCommercial(string toClean,bool removeAllMixTagTypes = false,bool removeHyphens = false)
        {
            string result = NamingConventions.removeNumbers(toClean);
            result = RemoveCopys(result);
            result = NamingConventions.RemoveFeatureText(result);
           // result = RemoveDigits(result);
            result = RemoveParenthesisTags(result, true);
            result = RemoveAbstractCharacters(result);
            if (removeHyphens)
            {
                result = ReplaceHyphens(result);
            }
            
            result = NamingConventions.removeNumbers(result);
            result = NamingConventions.ConverToTitleCase(result, true);
            result = RemoveMixTags(result, !removeAllMixTagTypes);
            result = RemoveUnderScores(result);
            return result;
        }
        private string RemoveDigits(string key)
        {
           
                
                return Regex.Replace(key, @"\d", "");
           
        }
    }
}
