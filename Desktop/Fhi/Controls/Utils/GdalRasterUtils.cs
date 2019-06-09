using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using OSGeo.GDAL;
using OSGeo.OGR;
using OSGeo.OSR;

namespace Fhi.Controls.Utils
{
    public static class GdalRasterUtils
    {
        // TODO: raster files with pixel sizes other than a single byte, multiple bands
        public static void ReadRasterRows(Dataset valueRaster, Geometry polygon, out Dictionary<byte, int> tally, CancellationToken cancellation, IProgress<int> progress)
        {
            var bandValueRaster = valueRaster.GetRasterBand(1);
            var polygonSpatialReference = polygon.GetSpatialReference();
            var rasterSpatialReference = new SpatialReference(valueRaster.GetProjection());

            var rv = new Dictionary<byte, int>();

            var rasterTransform = new double[6]; // why 6?
            valueRaster.GetGeoTransform(rasterTransform);

            polygon.TransformTo(rasterSpatialReference);
            var polygonEnvelope = new Envelope();
            polygon.GetEnvelope(polygonEnvelope);
            polygon.TransformTo(polygonSpatialReference);

            var minRow = (int)Math.Round((polygonEnvelope.MinY - rasterTransform[3]) / rasterTransform[5], 0);
            var maxRow = (int)Math.Round((polygonEnvelope.MaxY - rasterTransform[3]) / rasterTransform[5], 0);
            var minColumn = (int)Math.Round((polygonEnvelope.MinX - rasterTransform[0]) / rasterTransform[1], 0);
            var maxColumn = (int)Math.Round((polygonEnvelope.MaxX - rasterTransform[0]) / rasterTransform[1], 0);

            var startRow = Math.Min(minRow, maxRow);
            var endRow = Math.Max(minRow, maxRow);
            var startColumn = Math.Min(minColumn, maxColumn);
            var endColumn = Math.Max(minColumn, maxColumn);
            var width = endColumn - startColumn + 1; // inclusive of the end column

            var transform = new CoordinateTransformation(rasterSpatialReference, polygonSpatialReference);
            var completed = new bool[endRow - startRow];

            for (var row = startRow; row < endRow; row++)
            {
                if (cancellation.IsCancellationRequested) break;

                var rasterValues = new byte[width];
                bandValueRaster.ReadRaster(startColumn, row, width, 1, rasterValues, width, 1, 0, 0);
                var line = new Geometry(wkbGeometryType.wkbLineString);

                var x0 = startColumn * rasterTransform[1] + rasterTransform[0];
                var y0 = row * rasterTransform[5] + rasterTransform[3];
                var x1 = endColumn * rasterTransform[1] + rasterTransform[0];
                var y1 = row * rasterTransform[5] + rasterTransform[3];

                line.AddPoint(x0, y0, 0);
                line.AddPoint(x1, y1, 0);
                line.Transform(transform);
                var match = polygon.Intersection(line);
                if (match?.IsEmpty() == false)
                {
                    match.TransformTo(rasterSpatialReference);

                    var ranges = GetRanges(match, rasterTransform);
                    foreach (var (start, end) in ranges)
                    {
                        for (var column = start; column < end; column++)
                        {
                            var value = rasterValues[column - startColumn];
                            if (!rv.ContainsKey(value))
                                rv[value] = 0;
                            rv[value]++;
                        }
                    }
                }

                completed[row - startRow] = true;
                progress.Report((int)(100.0 * completed.Count(x => x) / completed.Length));
            }
            tally = rv;
        }

        private static IEnumerable<(int start, int end)> GetRanges(Geometry match, double[] rasterTransform)
        {
            var rv = new List<(int start, int end)>();
            var pc = match.GetPointCount();
            if (pc == 2)
            {
                var p0 = new double[2];
                match.GetPoint_2D(0, p0);
                var p1 = new double[2];
                match.GetPoint_2D(1, p1);
                rv.Add(((int)Math.Round((p0[0] - rasterTransform[0]) / rasterTransform[1], 0),
                        (int)Math.Round((p1[0] - rasterTransform[0]) / rasterTransform[1], 0)));
            }
            else if (pc == 1)
            {
                var p0 = new double[2];
                match.GetPoint_2D(0, p0);
                var gt = match.GetGeometryType();
                Debug.Assert(gt == wkbGeometryType.wkbPoint25D);
                var start = (int) Math.Round((p0[0] - rasterTransform[0]) / rasterTransform[1], 0);
                rv.Add((start, start + 1));
            }
            else if (pc == 0)
            {
                var gc = match.GetGeometryCount();
                for (var i = 0; i < gc; i++)
                    rv.AddRange(GetRanges(match.GetGeometryRef(i), rasterTransform));
            }
            else
            {
                Trace.WriteLine("this case is not handled!");
            }

            return rv;
        }

        public static String DumpResult(Dictionary<byte, int> result)
        {
            var sb = new StringBuilder();
            var keySpace = new List<byte>(result.Keys);
            keySpace.Sort();
            foreach (var key in keySpace)
                sb.AppendLine($"{key} : {result[key]}");
            return sb.ToString();
        }

        public static String GeoTiffDescription(this Dataset self)
        {
            var metadata = self.GetMetadata("");
            if (metadata == null) return null;
            foreach (var item in metadata)
            {
                if (item.StartsWith("Dataset"))
                    return item.Replace("Dataset", "").Replace("=", "").Trim();
            }

            return null;
        }

        public static String DumpDatasetInfo(Dataset ds)
        {
            var sb = new StringBuilder();

            sb.AppendLine("Raster dataset parameters:");
            sb.AppendLine("  Projection: " + ds.GetProjectionRef());
            sb.AppendLine("  RasterCount: " + ds.RasterCount);
            sb.AppendLine("  RasterSize (" + ds.RasterXSize + "," + ds.RasterYSize + ")");

            /* -------------------------------------------------------------------- */
            /*      Get driver                                                      */
            /* -------------------------------------------------------------------- */
            var drv = ds.GetDriver();

            if (drv == null)
            {
                sb.AppendLine("Can't get driver.");
                return sb.ToString();
            }

            sb.AppendLine("Using driver " + drv.LongName);

            /* -------------------------------------------------------------------- */
            /*      Get metadata                                                    */
            /* -------------------------------------------------------------------- */
            string[] metadata = ds.GetMetadata("");
            if (metadata.Length > 0)
            {
                sb.AppendLine("  Metadata:");
                for (int iMeta = 0; iMeta < metadata.Length; iMeta++)
                {
                    sb.AppendLine("    " + iMeta + ":  " + metadata[iMeta]);
                }
                sb.AppendLine("");
            }

            /* -------------------------------------------------------------------- */
            /*      Report "IMAGE_STRUCTURE" metadata.                              */
            /* -------------------------------------------------------------------- */
            metadata = ds.GetMetadata("IMAGE_STRUCTURE");
            if (metadata.Length > 0)
            {
                sb.AppendLine("  Image Structure Metadata:");
                for (int iMeta = 0; iMeta < metadata.Length; iMeta++)
                {
                    sb.AppendLine("    " + iMeta + ":  " + metadata[iMeta]);
                }
                sb.AppendLine("");
            }

            /* -------------------------------------------------------------------- */
            /*      Report subdatasets.                                             */
            /* -------------------------------------------------------------------- */
            metadata = ds.GetMetadata("SUBDATASETS");
            if (metadata.Length > 0)
            {
                sb.AppendLine("  Subdatasets:");
                for (int iMeta = 0; iMeta < metadata.Length; iMeta++)
                {
                    sb.AppendLine("    " + iMeta + ":  " + metadata[iMeta]);
                }
                sb.AppendLine("");
            }

            /* -------------------------------------------------------------------- */
            /*      Report geolocation.                                             */
            /* -------------------------------------------------------------------- */
            metadata = ds.GetMetadata("GEOLOCATION");
            if (metadata.Length > 0)
            {
                sb.AppendLine("  Geolocation:");
                for (int iMeta = 0; iMeta < metadata.Length; iMeta++)
                {
                    sb.AppendLine("    " + iMeta + ":  " + metadata[iMeta]);
                }
                sb.AppendLine("");
            }

            /* -------------------------------------------------------------------- */
            /*      Report corners.                                                 */
            /* -------------------------------------------------------------------- */
            sb.AppendLine("Corner Coordinates:");
            sb.AppendLine("  Upper Left (" + GdalInfoGetPosition(ds, 0.0, 0.0) + ")");
            sb.AppendLine("  Lower Left (" + GdalInfoGetPosition(ds, 0.0, ds.RasterYSize) + ")");
            sb.AppendLine("  Upper Right (" + GdalInfoGetPosition(ds, ds.RasterXSize, 0.0) + ")");
            sb.AppendLine("  Lower Right (" + GdalInfoGetPosition(ds, ds.RasterXSize, ds.RasterYSize) + ")");
            sb.AppendLine("  Center (" + GdalInfoGetPosition(ds, ds.RasterXSize / 2.0, ds.RasterYSize / 2.0) + ")");
            sb.AppendLine("");

            /* -------------------------------------------------------------------- */
            /*      Report projection.                                              */
            /* -------------------------------------------------------------------- */
            string projection = ds.GetProjectionRef();
            if (projection != null)
            {
                SpatialReference srs = new SpatialReference(null);
                if (srs.ImportFromWkt(ref projection) == 0)
                {
                    srs.ExportToPrettyWkt(out var wkt, 0);
                    sb.AppendLine("Coordinate System is:");
                    sb.AppendLine(wkt);
                }
                else
                {
                    sb.AppendLine("Coordinate System is:");
                    sb.AppendLine(projection);
                }
            }

            /* -------------------------------------------------------------------- */
            /*      Report GCPs.                                                    */
            /* -------------------------------------------------------------------- */
            if (ds.GetGCPCount() > 0)
            {
                sb.AppendLine("GCP Projection: " + ds.GetGCPProjection());
                GCP[] gcps = ds.GetGCPs();
                for (int i = 0; i < ds.GetGCPCount(); i++)
                {
                    sb.AppendLine("GCP[" + i + "]: Id=" + gcps[i].Id + ", Info=" + gcps[i].Info);
                    sb.AppendLine("          (" + gcps[i].GCPPixel + "," + gcps[i].GCPLine + ") -> ("
                                + gcps[i].GCPX + "," + gcps[i].GCPY + "," + gcps[i].GCPZ + ")");
                    sb.AppendLine("");
                }
                sb.AppendLine("");

                double[] transform = new double[6];
                Gdal.GCPsToGeoTransform(gcps, transform, 0);
                sb.AppendLine("GCP Equivalent geotransformation parameters: " + ds.GetGCPProjection());
                for (int i = 0; i < 6; i++)
                    sb.AppendLine("t[" + i + "] = " + transform[i]);
                sb.AppendLine("");
            }

            /* -------------------------------------------------------------------- */
            /*      Get raster band                                                 */
            /* -------------------------------------------------------------------- */
            for (int iBand = 1; iBand <= ds.RasterCount; iBand++)
            {
                Band band = ds.GetRasterBand(iBand);
                sb.AppendLine("Band " + iBand + " :");
                sb.AppendLine("   DataType: " + Gdal.GetDataTypeName(band.DataType));
                sb.AppendLine("   ColorInterpretation: " + Gdal.GetColorInterpretationName(band.GetRasterColorInterpretation()));
                ColorTable ct = band.GetRasterColorTable();
                if (ct != null)
                    sb.AppendLine("   Band has a color table with " + ct.GetCount() + " entries.");

                sb.AppendLine("   Description: " + band.GetDescription());
                sb.AppendLine("   Size (" + band.XSize + "," + band.YSize + ")");
                band.GetBlockSize(out var blockXSize, out var blockYSize);
                sb.AppendLine("   BlockSize (" + blockXSize + "," + blockYSize + ")");
                band.GetMinimum(out var val, out var hasVal);
                if (hasVal != 0) sb.AppendLine("   Minimum: " + val);
                band.GetMaximum(out val, out hasVal);
                if (hasVal != 0) sb.AppendLine("   Maximum: " + val);
                band.GetNoDataValue(out val, out hasVal);
                if (hasVal != 0) sb.AppendLine("   NoDataValue: " + val);
                band.GetOffset(out val, out hasVal);
                if (hasVal != 0) sb.AppendLine("   Offset: " + val);
                band.GetScale(out val, out hasVal);
                if (hasVal != 0) sb.AppendLine("   Scale: " + val);

                for (int iOver = 0; iOver < band.GetOverviewCount(); iOver++)
                {
                    Band over = band.GetOverview(iOver);
                    sb.AppendLine("      OverView " + iOver + " :");
                    sb.AppendLine("         DataType: " + over.DataType);
                    sb.AppendLine("         Size (" + over.XSize + "," + over.YSize + ")");
                    sb.AppendLine("         PaletteInterpretation: " + over.GetRasterColorInterpretation());
                }
            }

            return sb.ToString();
        }

        private static string GdalInfoGetPosition(Dataset ds, double x, double y)
        {
            var adfGeoTransform = new double[6];
            ds.GetGeoTransform(adfGeoTransform);

            var dfGeoX = adfGeoTransform[0] + adfGeoTransform[1] * x + adfGeoTransform[2] * y;
            var dfGeoY = adfGeoTransform[3] + adfGeoTransform[4] * x + adfGeoTransform[5] * y;

            return dfGeoX + ", " + dfGeoY;
        }
    }
}
