using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Esri.ArcGISRuntime.Mapping;
using Fhi.Controls.MVVM;
using Fhi.Controls.Network;
using Fhi.Properties;
using Newtonsoft.Json;

namespace Fhi.Controls.Infrastructure
{
    public class OptionsViewModel : ViewModelBase
    {
        private BasemapSelection _selectedMap;

        public OptionsViewModel()
        {
            MapChoices = BasemapSelection.Create();
            BasemapSelection setting = null;
            if (!String.IsNullOrWhiteSpace(Settings.Default.Basemap))
            {
                try
                {
                    setting = JsonConvert.DeserializeObject<BasemapSelection>(Settings.Default.Basemap);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Basemap setting parsing: {ex.Message} {Settings.Default.Basemap}");
                }
            }

            
            _selectedMap = MapChoices.FirstOrDefault(x => (setting == null ? x.Default : x.Name == setting.Name));
            if (_selectedMap == null)
               _selectedMap = MapChoices.FirstOrDefault(x => x.Default);
        }

        public IList<BasemapSelection> MapChoices { get; }

        public BasemapSelection SelectedMap
        {
            get => _selectedMap;
            set
            {
                if (!Set(ref _selectedMap, value)) return;
                BasinMapViewModel.SelectedBasemap = _selectedMap;
                Settings.Default.Basemap = JsonConvert.SerializeObject(_selectedMap);
                Settings.Default.Save();
            }
        }
    }

    public class BasemapSelection
    {
        public String Name { get; set; }
        public BasemapType Basemap { get; set; }
        public String Id { get; set; }
        public Boolean Default { get; set; }
        public Boolean Builtin { get; set; }
        
        public override string ToString()
        {
            return Name;
        }

        public static IList<BasemapSelection> Create()
        {
            return new List<BasemapSelection>
            {
                new BasemapSelection
                {
                    Basemap = BasemapType.Imagery,
                    Name = "Imagery",
                    Builtin = true
                },
                new BasemapSelection
                {
                    Basemap = BasemapType.ImageryWithLabels,
                    Name = "Imagery with labels",
                    Builtin = true
                },
                new BasemapSelection
                {
                    Basemap = BasemapType.Streets,
                    Name = "Streets",
                    Builtin = true

                },
                new BasemapSelection
                {
                    Basemap = BasemapType.Topographic,
                    Name = "Topographic",
                    Builtin = true

                },
                new BasemapSelection
                {
                    Basemap = BasemapType.TerrainWithLabels,
                    Name = "Terrain with labels",
                    Builtin = true

                },
                new BasemapSelection
                {
                    Basemap = BasemapType.LightGrayCanvas,
                    Name = "Light gray canvas",
                    Builtin = true

                },
                new BasemapSelection
                {
                    Basemap = BasemapType.NationalGeographic,
                    Name = "National Geographic Map",
                    Builtin = true

                },
                new BasemapSelection
                {
                    Basemap = BasemapType.Oceans,
                    Name = "Ocean basemap",
                    Builtin = true

                },
                new BasemapSelection
                {
                    Basemap = BasemapType.OpenStreetMap,
                    Name = "Open street map basemap",
                    Builtin = true

                },
                new BasemapSelection
                {
                    Basemap = BasemapType.ImageryWithLabelsVector,
                    Name = "Imagery with labels vector basemap",
                    Builtin = true

                },
                new BasemapSelection
                {
                    Basemap = BasemapType.StreetsVector,
                    Name = "Streets vector basemap",
                    Builtin = true

                },
                new BasemapSelection
                {
                    Basemap = BasemapType.TopographicVector,
                    Name = "Topographic vector basemap",
                    Builtin = true

                },
                new BasemapSelection
                {
                    Basemap = BasemapType.TerrainWithLabelsVector,
                    Name = "Terrain with labels vector basemap",
                    Builtin = true

                },
                new BasemapSelection
                {
                    Basemap = BasemapType.LightGrayCanvasVector,
                    Name = "Light gray canvas vector basemap",
                    Builtin = true

                },
                new BasemapSelection
                {
                    Basemap = BasemapType.NavigationVector,
                    Name = "Navigation vector basemap",
                    Builtin = true

                },
                new BasemapSelection
                {
                    Basemap = BasemapType.StreetsNightVector,
                    Name = "Streets night vector basemap",
                    Builtin = true

                },
                new BasemapSelection
                {
                    Basemap = BasemapType.StreetsWithReliefVector,
                    Name = "Streets with relief vector basemap",
                    Default = true,
                    Builtin = true
                },
                new BasemapSelection
                {
                    Basemap = BasemapType.DarkGrayCanvasVector,
                    Name = "Dark gray canvas vector basemap",
                    Builtin = true

                },
                /*
                new BasemapSelection
                {
                    Name = "FHI custom",
                    Id = "88f339245400469e88d784524ae80fca"
                }
                */
            };
        }
    }
}
