using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Fhi.Controls.Network;
using FhiModel;
using FhiModel.Common;
using FhiModel.EcosystemServices;
using FhiModel.EcosystemVitality;
using FhiModel.EcosystemVitality.DendreticConnectivity;
using FhiModel.EcosystemVitality.FlowDeviation;
using FhiModel.EcosystemVitality.WaterQuality;
using OfficeOpenXml.FormulaParsing.Excel.Functions;
using Xceed.Words.NET;

namespace Fhi.Controls.Utils
{
    public static class Report
    {
        public static async Task Overview(Model model, BasinMapViewModel basin, String filename, bool detail = true)
        {
            try
            {
                var imageFile = Path.GetTempFileName() + ".png";
                await basin.Snapshot(imageFile);
                
                using (var document = DocX.Create(filename))
                {

                    var tp = document.InsertParagraph("Freshwater Health Index Assessment").Color(_blue).FontSize(16);
                    tp.Alignment = Alignment.center;
                    tp = document.InsertParagraph($"{model.Attributes.Title} {model.Attributes.AssessmentYear}").FontSize(14);
                    tp.Alignment = Alignment.center;
                    
                    // the picture
                    var image = document.AddImage(imageFile);
                    var picture = image.CreatePicture();
                    var ip = document.InsertParagraph();
                    ip.Alignment = Alignment.center;
                    ip.AppendPicture(picture);

                    document.InsertParagraph($"{DateTime.Now.ToLongDateString()}");
                    document.InsertParagraph(model.Attributes.Notes);

                    var level1 = 2;
                    foreach (var pillar in new[] {model.EcosystemVitality, model.EcosystemServices, model.Governance})
                    {
                        AddSection(document, pillar, detail, level1);
                        var level2 = 1;
                        foreach (var indicator in pillar.Children)
                        {
                            AddSection(document, indicator, detail, level1, level2);

                            var level3 = 1;
                            if (indicator.Children == null) continue;
                            foreach (var sub in indicator.Children)
                            {
                                AddSection(document, sub,  detail, level1, level2, level3);
                                level3++;
                            }

                            level2++;
                        }
                        level1++;
                        
                    }

                    document.Save();
                    File.Delete(imageFile);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error producing document: {ex.Message}");
            }
        }

        private static readonly Color _blue = Color.FromArgb(0xff, 0x04, 0x94, 0xca);

        private static void AddSection(DocX document, IIndicator i, bool detail, int level1, int? level2 = null, int? level3 = null)
        {
            var sb = new StringBuilder();
            var color = Color.Black;
            var fontSize = 12;

            if (level3 != null)
            {
                sb.Append($"{level1}.{level2}.{level3} ");
            }
            else if (level2 != null)
            {
                sb.Append($"{level1}.{level2} ");
                color = _blue;
            }
            else
            {
                sb.Append($"{level1} ");
                color = _blue;
                fontSize = 14;
            }

            var value = i.Value?.ToString() ?? "N/A";
            sb.Append($"{i.Name}: {value}");
            if (i.Weight != null)
                sb.Append($" ({i.Weight:N2})");

            document.InsertParagraph(sb.ToString()).Color(color).FontSize(fontSize);
            if (i.UserOverride != null)
                document.InsertParagraph($"Value set by assessor" + (i.OverrideComment != null ? $": {i.OverrideComment}" : ".")).Alignment = Alignment.center;
            if (!detail || i.Value == null || i.UserOverride != null || !_detailStrategy.ContainsKey(i.GetType())) return;

            // detail
            _detailStrategy[i.GetType()](document, i);
            
        }


        private static readonly Dictionary<Type, Action<DocX, IIndicator>> _detailStrategy =
            new Dictionary<Type, Action<DocX, IIndicator>>
            {
                { typeof(ConnectivityIndicator), ConnectivityDetail },
                { typeof(BankModificationIndicator), LandCoverDetail },
                { typeof(LandCoverIndicator), LandCoverDetail },
                { typeof(WaterQualityIndicator), WaterQualityDetail },
                { typeof(EcosystemServicesIndicator), EcosystemServicesDetail },
                { typeof(FlowDeviationIndicator), FlowDeviationDetail }
            };

        private static void ConnectivityDetail(DocX doc, IIndicator i)
        {
            if (!(i is ConnectivityIndicator ci)) return;
            
            doc.InsertParagraph($"Potadromous results: {ci.DciP} ({ci.PotadromousWeight:N2})");
            doc.InsertParagraph($"Diadromous results: {ci.DciD} ({ci.DiadromousWeight:N2})");
        }

        private static void LandCoverDetail(DocX doc, IIndicator i)
        {
            if (!(i is ICoverage ci)) return;

            foreach (var item in ci.Coverage)
            {
                if (!item.Weight.HasValue || !item.Area.HasValue) continue;
                doc.InsertParagraph($"{item.Naturalness} {item.Area} ({item.Weight})");
            }
        }

        private static void WaterQualityDetail(DocX doc, IIndicator i)
        {
            if (!(i is WaterQualityIndicator wq)) return;
            foreach (var gauge in wq.Gauges)
            {
                doc.InsertParagraph($"Gauge {gauge.Name}: {gauge.Value:N2}");
                if (!String.IsNullOrWhiteSpace(gauge.Notes))
                    doc.InsertParagraph(gauge.Notes);
            }
        }

        private static void EcosystemServicesDetail(DocX doc, IIndicator i)
        {
            if (!(i is EcosystemServicesIndicator es)) return;
            doc.InsertParagraph($"Evidence Level: {es.EvidenceLevel}");
            foreach (var su in es.SpatialUnits)
            {
                doc.InsertParagraph($"Spatial unit: {su.Name} {su.Units}");
            }
        }

        private static void FlowDeviationDetail(DocX doc, IIndicator i)
        {
            if (!(i is FlowDeviationIndicator dv)) return;
            
            foreach (var s in dv.Stations)
            {
                if (!s.FlowDeviation.HasValue || !s.MeanDischarge.HasValue) continue;
                doc.InsertParagraph($"Station: {s.Name}, Flow Deviation {s.FlowDeviation}, Mean Discharge {s.MeanDischarge}");
            }
        }
    }
}
