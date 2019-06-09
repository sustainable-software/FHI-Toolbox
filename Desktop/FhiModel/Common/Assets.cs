using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace FhiModel.Common
{
    [DataContract(Namespace = "", IsReference = true)]
    public class ModelAssets : ModelBase
    {
        public ModelAssets()
        {
            OnDeserialized();
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context = default(StreamingContext))
        {
            Assets = Assets ?? new List<Asset>();
        }
        
        [DataMember]
        public List<Asset> Assets { get; set; }
        
        /// <summary>
        /// Create a new asset
        /// </summary>
        public Guid Create(String path, String name, String assetName)
        {
            if (!Directory.Exists(path))
                throw new ArgumentException($"{path} does not exist or is not a directory.");
            var id = Guid.NewGuid();
            var directory = GetCacheItemPath(id);
            var items = DirectoryCopy(path, name, directory); // make the local copy
            Assets.Add(new Asset
            {
                Name = assetName,
                CoreFile = name,
                Id = id,
                Items = items
            });
            RaisePropertyChanged(nameof(Assets));
            return id;
        }

        public void Delete(String assetName)
        {
            var asset = Assets.FirstOrDefault(x => x.Name == assetName);
            if (asset == null)
                return;
            RemoveLocalCopy(asset);
            Assets.Remove(asset);
            RaisePropertyChanged(nameof(Assets));
        }

        /// <summary>
        /// Get a local path to the requested asset.
        /// </summary>
        public String PathTo(Guid id)
        {
            var asset = Assets.FirstOrDefault(x => x.Id == id);
            if (asset == null)
                return null;
            var directory = GetCacheItemPath(asset.Id);
            if (!Directory.Exists(directory))
                MakeLocalCopy(asset);
            return directory;
        }
        
        /// <summary>
        /// Get a local path to the requested asset.
        /// </summary>
        public String PathTo(String assetName)
        {
            var asset = Assets.FirstOrDefault(x => x.Name == assetName);
            if (asset == null)
                return null;
            var directory = GetCacheItemPath(asset.Id);
            if (!Directory.Exists(directory))
                MakeLocalCopy(asset);
            return directory;
        }

        /// <summary>
        /// Test to see if the asset exists in the model.
        /// </summary>
        public Boolean Exists(String assetName)
        {
            return Assets.FirstOrDefault(x => x.Name == assetName) != null;
        }

        private static void MakeLocalCopy(Asset asset)
        {
            var directory = GetCacheItemPath(asset.Id);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            foreach (var item in asset.Items)
                File.WriteAllBytes(Path.Combine(directory, item.Name), item.Data);
        }
        
        private static void RemoveLocalCopy(Asset asset)
        {
            var directory = GetCacheItemPath(asset.Id);
            if (!Directory.Exists(directory))
                return;
            try
            {
                foreach (var file in Directory.EnumerateFiles(directory))
                    File.Delete(file);
                Directory.Delete(directory, false);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Failed to delete directory {ex.Message}");
            }
        }
        
        private static String GetCacheItemPath(Guid id)
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var assembly = Assembly.GetEntryAssembly();
            var companyName = assembly.GetCustomAttribute<AssemblyCompanyAttribute>().Company;
            var productName = assembly.GetCustomAttribute<AssemblyProductAttribute>().Product;
            return Path.Combine(path, companyName, productName, "Cache", id.ToString());
        }
        
        // from MSFT
        private static List<AssetItem> DirectoryCopy(string sourceDirName, string coreFile, string destDirName)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            var items = new List<AssetItem>();
            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles($"{coreFile}.*");
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
                items.Add(new AssetItem
                {
                    Name = file.Name,
                    Data = File.ReadAllBytes(temppath)
                });
            }

            return items;
        }
    }

    [DataContract(Namespace = "", IsReference = true)]
    public class Asset : ModelBase
    {
        [DataMember]
        public String Name { get; set; }
        [DataMember]
        public Guid Id { get; set; }
        [DataMember]
        public String CoreFile { get; set; }
        [DataMember]
        public List<AssetItem> Items { get; set; }
    }
    
    [DataContract(Namespace = "", IsReference = true)]
    public class AssetItem : ModelBase
    {
        [DataMember]
        public String Name { get; set; }
        [DataMember]
        public Byte[] Data { get; set; }
    }
}