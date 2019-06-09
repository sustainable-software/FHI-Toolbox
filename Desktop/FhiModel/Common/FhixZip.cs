using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization;
using System.Threading;
using System.Xml;

namespace FhiModel.Common
{
    public static class FhixZip
    {
        private const String VersionEntry = "version";
        private const String ModelEntry = "model";
        private const String Version = "1.0";

        public class Context
        {
            public ZipArchive Archive { get; }
            
            private Int32 TotalSteps { get; }
            private Int32 CurrentStep { get; set; }
            private CancellationToken CancellationToken { get; }
            private IProgress<Int32> Progress { get; }
            
            public Context(ZipArchive archive, Int32 totalSteps, CancellationToken cancellationToken, IProgress<Int32> progress)
            {
                Archive = archive;
                TotalSteps = totalSteps;
                CurrentStep = 0;
                CancellationToken = cancellationToken;
                Progress = progress;
            }

            public void Step()
            {
                CancellationToken.ThrowIfCancellationRequested();
                Progress?.Report(100 * ++CurrentStep / TotalSteps);
            }
        }

        #region Read
        public static Model ReadZip(String fileName, CancellationToken cancellationToken, IProgress<Int32> progress = null)
        {
            using (var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
                {
                    var steps = archive.Entries.Count;
                    var context = new Context(archive, steps, cancellationToken, progress);
                    ReadVersion(context);
                    var model = ReadModel(context);
                    return model;
                }
            }
        }

        private static void ReadVersion(Context context)
        {
            var entry = context.Archive.GetEntry(VersionEntry);
            if (entry == null)
                throw new ArgumentException("Bad format in file (version)");
            using (var reader = new StreamReader(entry.Open()))
            {
                var version = reader.ReadLine();
	            if (!String.IsNullOrWhiteSpace(version))
	            {
					var fileVersion = new Version(version);
					var fhixVersion = new Version(Version);
		            if (fileVersion > fhixVersion)	// can't read the future!
			            throw new ArgumentException($"Can't read file version {fileVersion} in FHI Tool version {fhixVersion}. They are incompatible.");
	            }
            }
            context.Step();
        }

        private static Model ReadModel(Context context)
        {
            var entry = context.Archive.GetEntry(ModelEntry);
            if (entry == null)
                throw new ArgumentException("Bad format in file (model)");
            Model model;
            using (var reader = XmlReader.Create(entry.Open(), new XmlReaderSettings { CloseInput = true }))
            {
                var serializer = new DataContractSerializer(typeof(Model));
                model = serializer.ReadObject(reader) as Model;
            }
            context.Step();
            return model;
        }
        #endregion


        #region Write
        public static void WriteZip(String fileName,Model model, CancellationToken cancellationToken, IProgress<Int32> progress = null)
        {
            var tempFileName = GetTemporaryFileName(fileName);
            using (var stream = new FileStream(tempFileName, FileMode.Create))
            {
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Create))
                {
                    var steps = 2;
                    var context = new Context(archive, steps, cancellationToken, progress);
                    WriteVersion(context);
                    WriteProblem(context, model);
                }
            }
            MoveFile(tempFileName, fileName);
        }

        private static void WriteVersion(Context context)
        {
            var entry = context.Archive.CreateEntry(VersionEntry);
            using (var writer = new StreamWriter(entry.Open()))
            {
                writer.Write(Version);
            }
            context.Step();
        }

        private static void WriteProblem(Context context, Model model)
        {
            var entry = context.Archive.CreateEntry(ModelEntry);
            using (var writer = XmlWriter.Create(entry.Open(), new XmlWriterSettings { Indent = true, CloseOutput = true }))
            {
                var serializer = new DataContractSerializer(typeof(Model));
                serializer.WriteObject(writer, model, _namespaces);
            }
            context.Step();
        }

        // override namespaces for data contract serializer to make their names compact
        private static readonly Dictionary<String, String> _namespaces = new Dictionary<String, String>
        {
            {"s", "http://www.w3.org/2001/XMLSchema"},
            {"z", "http://schemas.microsoft.com/2003/10/Serialization/"},
            {"a", "http://schemas.microsoft.com/2003/10/Serialization/Arrays"},
        };

        private static String GetTemporaryFileName(String fileName)
        {
            return fileName + "_" + Guid.NewGuid();
        }

        private static void MoveFile(String sourceFileName, String targetFileName)
        {
            if (File.Exists(targetFileName))
                File.Delete(targetFileName);
            if (File.Exists(sourceFileName))
                File.Move(sourceFileName, targetFileName);
        }
        #endregion
    }
    
    internal static class SerializerExtensions
    {
        public static void WriteObject(this DataContractSerializer serializer, XmlWriter stream, Object graph, Dictionary<String, String> namespaces)
        {
            serializer.WriteStartObject(stream, graph);
            foreach (var pair in namespaces)
                stream.WriteAttributeString("xmlns", pair.Key, String.Empty, pair.Value);
            serializer.WriteObjectContent(stream, graph);
            serializer.WriteEndObject(stream);
        }
    }
}

