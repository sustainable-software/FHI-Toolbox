using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Xml;
using FhiModel.EcosystemVitality.Biodiversity;

namespace FhiModel.Services
{
    public class SpeciesService
    {
        private static readonly string _filename = @"foo.xml";
        public static List<Species> ReadSpeciesAsync()
        {
            // todo: temporary until we get library up and running

            using (var reader = XmlReader.Create(_filename, new XmlReaderSettings {CloseInput = true}))
             {
                 var serializer = new DataContractSerializer(typeof(List<Species>));
                 return serializer.ReadObject(reader) as List<Species>;
             }
        }

        public static void WriteSpecies(List<Species> species)
        {
            using (var writer = XmlWriter.Create(_filename, new XmlWriterSettings { Indent = true, CloseOutput = true }))
            {
                var serializer = new DataContractSerializer(typeof(List<Species>));
                serializer.WriteObject(writer, species);
            }
        }
    }
}