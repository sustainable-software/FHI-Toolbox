using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

namespace FhiModel.Common
{
    public static class ObjectExtensions
    {
        public static T Clone<T>(this T self)
        {
            try
            {
                using (var stream = new MemoryStream())
                {
                    var formatter = new DataContractSerializer(typeof(T));
                    formatter.WriteObject(stream, self);
                    stream.Position = 0;
                    return (T) formatter.ReadObject(stream);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Clone<T>: " + ex);
                throw;
            }
        }

        public static void NormalizeWeights(this IIndicator self)
        {
            if (self.Children == null) return;
            foreach (var child in self.Children)
            {
                child.Weight = 1.0 / self.Children.Count;
                NormalizeWeights(child);
            }
        }

        public static T FetchIndicator<T>(this IIndicator self) where T: Indicator
        {
            if (self is T item)
                return item;

            if (self.Children == null) return null;
            
            foreach (var child in self.Children)
            {
                var childItem = child.FetchIndicator<T>();
                if (childItem != null)
                    return childItem;
            }
            
            return null;
        }
        
        public static T FetchIndicator<T>(this IIndicator self, String name) where T: Indicator
        {
            if (self is T item && self.Name == name)
                return item;

            if (self.Children == null) return null;
            
            foreach (var child in self.Children)
            {
                var childItem = child.FetchIndicator<T>(name);
                if (childItem != null)
                    return childItem;
            }
            
            return null;
        }
        
        public static IEnumerable<T> FetchIndicators<T>(this IIndicator self) where T: Indicator
        {
            var rv = new List<T>();
            if (self is T item)
                rv.Add(item);
            if (self.Children != null) 
            {    foreach (var child in self.Children)
                    rv.AddRange(child.FetchIndicators<T>());
            }
            return rv;
        }
        
    }
}