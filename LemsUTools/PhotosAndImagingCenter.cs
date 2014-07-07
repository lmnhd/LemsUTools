using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Web;
using StudioBContext.Concrete;
using StudioBContext.Entities;




namespace LemsUTools
{
    public class PhotoCenter
    {

        private string RelativeTopLevel = "";
        private string Authority = "";
        private string TopLevelAuthority = "";
        private string MidSizeFolder = "midsized(500w)/";
        private string ThumbFolder = "thumbs(150w)/";

        private string defaultImage = ("/Images/TheUAlone4.png");
        // private HttpRequestBase Request;

        public PhotoCenter(string storedImagesLocation, string localAuthorityString)
        {
            TopLevelAuthority = localAuthorityString;
            if ((!localAuthorityString.Contains("localhost")) && (!localAuthorityString.Contains("60139")))
            {
                localAuthorityString = localAuthorityString + "/untame14";
            }
            RelativeTopLevel = storedImagesLocation + "/ArtistPhotos/";
            Authority = localAuthorityString + "/ArtistPhotos/";
            // Request = _request;
        }

        public PhotoCenter()
        {


        }
        public string GetPhotoTypeString(ArtistPhoto pic)
        {
            switch (pic.PhotoType)
            {
                case ArtistPhoto.phototype.albumCover:
                    return "album";

                case ArtistPhoto.phototype.gallery:
                    return "gallery";

                case ArtistPhoto.phototype.liveEvent:
                    return "event";

                case ArtistPhoto.phototype.profile:
                    return "profile";

                case ArtistPhoto.phototype.songArt:
                    return "song";

            }
            return "gallery";
        }
        public string GetAlbumPhotoLinkOrThis(int albumID, Artist art, string thisText, bool thumb = true)
        {
            string result = thisText;

            if (art.Albums != null && art.Albums.Count > 0)
            {
                ArtistAlbum lp = art.Albums.Find(a => a.ArtistAlbumId == albumID);
                if (lp != null)
                {
                    if (lp.ArtistPhotoId != 0)
                    {
                        if (!thumb)
                        {
                            result = GetMidsizeLink(art.ArtImages.Find(i => i.ArtistPhotoId == lp.ArtistPhotoId), false, false);
                        }
                        else
                        {
                            result = GetThumbnailLink(art.ArtImages.Find(i => i.ArtistPhotoId == lp.ArtistPhotoId), false,false);


                        }


                    }

                }

            }
            return result;
        }
        public string GetArtistPhotoQuick(Artist art, bool thumb, int songId = 0)
        {
            

            if (songId > 0)
            {
                Song sng = art.AllSongs.Where(s => s.SongId == songId).FirstOrDefault();

                if (sng != null)
                {
                    if (sng.Albums == null)
                    {
                        sng.Albums = new List<ArtistAlbum>();
                    }
                    else
                    {
                        if (sng.PhotoId != 0)
                        {
                            if (thumb)
                            {

                                var tPhoto = art.ArtImages.Find(i => i.ArtistPhotoId == sng.PhotoId);

                                var path = GetThumbnailLink(tPhoto, true, false);

                                if (!System.IO.Directory.Exists(path))
                                {

                                }
                                else
                                {
                                    return GetThumbnailLink(art.ArtImages.Find(i => i.ArtistPhotoId == sng.PhotoId), false, false);
                                }


                            }
                            else
                            {
                                var mPhoto = art.ArtImages.Find(i => i.ArtistPhotoId == sng.PhotoId);

                                var path = GetMidsizeLink(mPhoto, true, false);

                                if (!System.IO.Directory.Exists(path))
                                {

                                }
                                else
                                {
                                    return GetMidsizeLink(art.ArtImages.Find(i => i.ArtistPhotoId == sng.PhotoId), false, false);
                                }



                            }
                        }
                    }
                    if ((sng.PrimaryAlbumID != 0 && sng.Albums.Find(a => a.ArtistAlbumId == sng.PrimaryAlbumID) != null) || sng.Albums != null && sng.Albums.Count > 0)
                    {
                        var lp = sng.Albums[0];
                        if (sng.PrimaryAlbumID != 0 && sng.Albums.Find(a => a.ArtistAlbumId == sng.PrimaryAlbumID) != null)
                        {
                            lp = sng.Albums.Find(a => a.ArtistAlbumId == sng.PrimaryAlbumID);
                        }
                        if (lp.ArtistPhotoId == 0)
                        {
                            foreach (ArtistAlbum alb in sng.Albums)
                            {
                                if (alb.ArtistPhotoId != 0)
                                {
                                    lp = alb;
                                }
                            }

                        }
                        if (thumb)
                        {
                            var tPhoto = art.ArtImages.Find(i => i.ArtistPhotoId == lp.ArtistPhotoId);

                            var path = GetThumbnailLink(tPhoto, true);

                            if (!System.IO.Directory.Exists(path))
                            {
                                return defaultImage;
                            }
                            else
                            {
                                return GetThumbnailLink(art.ArtImages.Find(i => i.ArtistPhotoId == lp.ArtistPhotoId), false, false, false);
                            }
                        }
                        else
                        {
                            var mPhoto = art.ArtImages.Find(i => i.ArtistPhotoId == lp.ArtistPhotoId);

                            var path = GetMidsizeLink(mPhoto, true, false);

                            if (!System.IO.Directory.Exists(path))
                            {
                                return defaultImage;
                            }
                            else
                            {

                                return GetMidsizeLink(art.ArtImages.Find(i => i.ArtistPhotoId == lp.ArtistPhotoId), false, false);
                            }
                        }
                    }
                }

                }
               

            var photo = art.GetAPhoto();

            if (photo == null)
            {
                return defaultImage;
            }

            if (thumb)
            {
                return GetThumbnailLink(photo, false);
            }


            return GetMidsizeLink(photo, false, false);

            //"\Images\TheUAlone4.png"

        }
        private string GetDefaultImage(bool relativeToServer, bool thumb = false)
        {
            if (relativeToServer)
            {
                return defaultImage;
            }
            else
            {
                return "http://" + TopLevelAuthority + defaultImage;
            }
        }
        public System.Drawing.Image ResizeImage(System.Drawing.Image image, Size size,
   bool preserveAspectRatio = true)
        {
            int newWidth;
            int newHeight;
            if (preserveAspectRatio)
            {
                int originalWidth = image.Width;
                int originalHeight = image.Height;
                float percentWidth = (float)size.Width / (float)originalWidth;
                float percentHeight = (float)size.Height / (float)originalHeight;
                float percent = percentHeight < percentWidth ? percentHeight : percentWidth;
                newWidth = (int)(originalWidth * percent);
                newHeight = (int)(originalHeight * percent);
            }
            else
            {
                newWidth = size.Width;
                newHeight = size.Height;
            }
            System.Drawing.Image newImage = new Bitmap(newWidth, newHeight);
            using (Graphics graphicsHandle = Graphics.FromImage(newImage))
            {
                graphicsHandle.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphicsHandle.DrawImage(image, 0, 0, newWidth, newHeight);
            }
            return newImage;
        }

        public byte[] GetBytesFromPostedFileBase(HttpPostedFileBase file)
        {

            byte[] imgbytes = new byte[file.ContentLength];
            file.InputStream.Read(imgbytes, 0, file.ContentLength);
            return imgbytes;

        }
        public byte[] GetBytesFromImage(System.Drawing.Image imag)
        {
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {
                imag.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                return ms.ToArray();
            }

          
            

        }
        public System.IO.MemoryStream GetMemoryStreamFromImage(System.Drawing.Image img)
        {
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            img.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
            return ms;
        }

        public System.Drawing.Image GetImageFromBytes(byte[] bytes)
        {
            System.IO.MemoryStream ms = new System.IO.MemoryStream(bytes);
            return System.Drawing.Image.FromStream(ms);

        }
        public System.Drawing.Image GetImageFromPostedFileBase(HttpPostedFileBase file)
        {

            System.Drawing.Image img = System.Drawing.Image.FromStream(file.InputStream);
            return img;
        }
        public enum filetype
        {
            jpg,
            png,
            gif

        }
        public string PictureDestination(int ArtistId, bool thumb, string title, int imageID, filetype filetype, bool FolderOnly, bool innerquery)
        {
            var result = string.Format("{0}{1}{2}{3}", "http://" + Authority, ArtistId + "/", thumb ? ThumbFolder : MidSizeFolder, imageID.ToString() + "." + Enum.GetName(typeof(filetype), filetype));
            if (innerquery)
            {

                result = string.Format("{0}{1}{2}{3}", RelativeTopLevel, ArtistId + "/", thumb ? ThumbFolder : MidSizeFolder, imageID.ToString() + "." + Enum.GetName(typeof(filetype), filetype));
                if (FolderOnly)
                {
                    result = string.Format("{0}{1}{2}", RelativeTopLevel, ArtistId + "/", thumb ? ThumbFolder : MidSizeFolder);

                }

            }
            else
            {
                if (FolderOnly)
                {
                    result = string.Format("{0}{1}{2}", Authority, ArtistId + "/", thumb ? ThumbFolder : MidSizeFolder);

                }

            }










            return result;

        }

        //public Boolean StoreImageFile(SSFMDomain.Entities.ArtistPhoto photo)
        //{
        //    try
        //    {

        //        Image _img = GetImageFromBytes(photo.Imagebytes);

        //        if (photo.Width > 500)
        //        {

        //            Image mid = ResizeImage(_img, new Size(500, 500), true);
        //            mid.Save(PictureDestination(photo.ArtistID,false,photo.Title,photo.ArtistPhotoId,filetype.jpg));



        //        }

        //        Image thumb = ResizeImage(_img, new Size(150, 150), false);
        //        thumb.Save(PictureDestination(photo.ArtistID,true,photo.Title,photo.ArtistPhotoId,filetype.jpg));



        //    }
        //    catch (Exception e)
        //    {

        //        return false;
        //    }

        //    return true;


        //}
        public String GetThumbnailLink(StudioBContext.Entities.ArtistPhoto photo, bool innerQuery, bool folderOnly = false,bool createNew = false )
        {
            try
            {
                if (!createNew)
                {
                    var path = PictureDestination(photo.ArtistID, true, photo.Title, photo.ArtistPhotoId, filetype.jpg, false, true);


                    if (!System.IO.File.Exists(path))
                    {
                        return GetDefaultImage(innerQuery);
                    }
                    else
                    {
                        return PictureDestination(photo.ArtistID, true, photo.Title, photo.ArtistPhotoId, filetype.jpg, folderOnly, innerQuery);
                    }
                }
                else
                {
                    return PictureDestination(photo.ArtistID, true, photo.Title, photo.ArtistPhotoId, filetype.jpg, folderOnly, innerQuery);
                }
                
            }
            catch
            {

            }
            return "";
        }
        public String GetMidsizeLink(StudioBContext.Entities.ArtistPhoto photo, bool innerQuery, bool createNew, bool folderOnly = false)
        {
            try
            {
                string filename = PictureDestination(photo.ArtistID, false, photo.Title, photo.ArtistPhotoId, filetype.jpg, false, true);
                //just return mid link if createnew
                if (createNew)
                {
                    return PictureDestination(photo.ArtistID, false, photo.Title, photo.ArtistPhotoId, filetype.jpg, folderOnly, innerQuery);
                }
                //check for mid and return thumb if not there
                if (System.IO.File.Exists(filename))
                {

                    return PictureDestination(photo.ArtistID, false, photo.Title, photo.ArtistPhotoId, filetype.jpg, folderOnly, innerQuery);
                }
                else
                {

                    return PictureDestination(photo.ArtistID, true, photo.Title, photo.ArtistPhotoId, filetype.jpg, folderOnly, innerQuery);
                }


            }
            catch
            {

            }
            return "";
        }



    }
}
