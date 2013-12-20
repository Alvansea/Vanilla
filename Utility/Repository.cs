using System;
using System.Collections.Generic;
using System.Text;

using System.IO;
using System.Drawing;
using Vanilla.Data;

namespace Vanilla.Utility
{
    public class Repository
    {
        public const int DEFAULT_JPEG_QUALITY = 95;

        private string rootPath = string.Empty;
        private string rootUrl = string.Empty;

        private int jpegQuality = 0;
        public int JpegQuality
        {
            set { this.jpegQuality = value; }
            get { return this.jpegQuality; }
        }

        public Repository(string rootPath, string rootUrl)
        {
            this.rootPath = rootPath;
            this.rootUrl = rootUrl;
            if (!Directory.Exists(rootPath))
            {
                Directory.CreateDirectory(rootPath);
            }
            this.jpegQuality = DEFAULT_JPEG_QUALITY;
        }

        #region Common Methods

        public Repository Sub(string folder)
        {
            if (!folder.EndsWith("/"))
            {
                folder += "/";
            }
            Repository rep = new Repository(this.rootPath + folder, this.rootUrl + folder);
            return rep;
        }

        public string GetFilePath(Guid fileID, string ext)
        {
            string path = string.Format(rootPath + "{0}.{1}", fileID, ext);
            return path;
        }

        public string GetFileUrl(Guid fileID, string ext)
        {
            string url = string.Format(rootUrl + "{0}.{1}", fileID, ext);
            return url;
        }

        public Guid Save(byte[] buffer, Guid fileID, string ext)
        {
            if (buffer.Length == 0)
            {
                return Guid.Empty;
            }
            string path = this.GetFilePath(fileID, ext);
            FileStream fs = File.Open(path, FileMode.Create);
            fs.Write(buffer, 0, buffer.Length);
            fs.Close();
            return fileID;
        }

        public Guid Create(byte[] buffer, string ext)
        {
            Guid id = Comb.NewComb();
            return this.Save(buffer, id, ext);
        }

        public Guid Create(string s, string ext)
        {
            Guid id = Comb.NewComb();
            return this.Save(s, id, ext);
        }

        public Guid Save(string s, Guid fileID, string ext)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(s.ToCharArray());
            return this.Save(buffer, fileID, ext);
        }

        public void Delete(Guid fileID, string ext)
        {
            string path = this.GetFilePath(fileID, ext);
            File.Delete(path);
        }

        public void Copy(Guid fileID, string ext, Repository target)
        {
            string srcPath = GetFilePath(fileID, ext);
            string targetPath = target.GetFilePath(fileID, ext);
            if (File.Exists(srcPath) && !File.Exists(targetPath))
            {
                File.Copy(srcPath, targetPath);
            }
        }

        public void Move(Guid fileID, string ext, Repository target)
        {
            string srcPath = GetFilePath(fileID, ext);
            string targetPath = target.GetFilePath(fileID, ext);
            if (File.Exists(srcPath) && !File.Exists(targetPath))
            {
                File.Move(srcPath, targetPath);
            }
        }

        public bool Exists(Guid fileID, string ext)
        {
            string path = this.GetFilePath(fileID, ext);
            return File.Exists(path);
        }

        public byte[] Get(Guid fileID, string ext)
        {
            string path = this.GetFilePath(fileID, ext);
            FileStream fs = File.Open(path, FileMode.Open);
            if (fs.Length >= 0)
            {
                byte[] buffer = new byte[fs.Length];
                fs.Read(buffer, 0, buffer.Length);
                fs.Close();
                return buffer;
            }
            else
            {
                byte[] buffer = new byte[0];
                fs.Close();
                return buffer;
            }
        }

        public string GetText(Guid fileID, string ext)
        {
            byte[] buffer = this.Get(fileID, ext);
            string s = Encoding.UTF8.GetString(buffer);
            return s;
        }

        public void Clear()
        {
            Directory.Delete(this.rootPath, true);
            Directory.CreateDirectory(this.rootPath);
        }

        #endregion

        #region Thumbnail Methods

        protected string GetThumbnailFolderName(int width, int height, bool cropped)
        {
            string folder = string.Format("{0}_{1}_{2}.thumbnail/", width, height, (cropped ? "c" : "o"));
            return folder;
        }

        public byte[] CreateThumbnail(Guid fileID, string ext, int width, int height, bool cropped)
        {
            string sourcePath = this.GetFilePath(fileID, ext);
            Bitmap source = new Bitmap(sourcePath);
            Bitmap thumb = ImageHelper.CreateThumbnail(source, width, height, !cropped);
            byte[] buffer = ImageHelper.SaveAsJpeg(thumb, this.JpegQuality);

            string folder = this.GetThumbnailFolderName(width, height, cropped);
            this.Sub(folder).Save(buffer, fileID, ext);

            source.Dispose();
            thumb.Dispose();

            return buffer;
        }

        public string GetThumbnailUrl(Guid fileID, string ext, int width, int height, bool cropped)
        {
            if (this.Exists(fileID, ext))
            {
                string folder = this.GetThumbnailFolderName(width, height, cropped);
                Repository sub = this.Sub(folder);
                if (!sub.Exists(fileID, ext))
                {
                    this.CreateThumbnail(fileID, ext, width, height, cropped);
                }
                return sub.GetFileUrl(fileID, ext);
            }
            else
            {
                return this.GetFileUrl(Guid.Empty, "jpg");
            }
        }

        public void ClearThumbnail()
        {
            string[] folders = Directory.GetDirectories(this.rootPath, "*.thumbnail");
            foreach (string folder in folders)
            {
                Directory.Delete(folder, true);
            }
        }

        #endregion
    }
}
