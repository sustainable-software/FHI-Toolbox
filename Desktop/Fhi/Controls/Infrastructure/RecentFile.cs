using System;
using System.Collections.Generic;
using System.Linq;
using Fhi.Properties;
using FhiModel;
using Newtonsoft.Json;

namespace Fhi.Controls.Infrastructure
{
    public class RecentFile : IComparable<RecentFile>
    {
        public RecentFile() { }
        public RecentFile(String path, Model model)
        {
            Path = path;
            Update(model);
        }

        public String Name
        {
            get
            {
                if (String.IsNullOrWhiteSpace(Path)) return null;
                return System.IO.Path.GetFileNameWithoutExtension(Path);
            }
        }

        public String Path { get; set; }
        public String Author { get; set; }
        public String Title { get; set; }
        public String Notes { get; set; }
        public Int32 Year { get; set; }
        public DateTime LastAccess { get; set; }
        
        public void Update(Model model)
        {
            Author = model.Attributes.Author;
            Title = model.Attributes.Title;
            Notes = model.Attributes.Notes;
            Year = model.Attributes.AssessmentYear;
            LastAccess = DateTime.Now;
        }

        public int CompareTo(RecentFile other)
        {
            var access = other.LastAccess.CompareTo(LastAccess);
            if (access == 0)
                return String.Compare(Name, other.Name, StringComparison.Ordinal);
            return access;
        }

        public static List<RecentFile> ReadRecentFiles()
        {
            var json = Settings.Default.RecentFiles;
            if (String.IsNullOrWhiteSpace(json)) return new List<RecentFile>();
            var rv = JsonConvert.DeserializeObject<List<RecentFile>>(json);
            rv.Sort();
            return rv;
        }

        public static void WriteRecentFiles(IList<RecentFile> files)
        {
            var list = new List<RecentFile>(files);
            list.Sort();
            Settings.Default.RecentFiles = JsonConvert.SerializeObject(files.Take(15));
            Settings.Default.Save();
        }
    }
}
