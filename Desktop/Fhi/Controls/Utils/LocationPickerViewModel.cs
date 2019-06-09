using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows;
using Esri.ArcGISRuntime.Geometry;
using Fhi.Controls.MVVM;
using Fhi.Controls.Network;
using FhiModel.Common;
using MaterialDesignThemes.Wpf;

namespace Fhi.Controls.Utils
{
    public class LocationPickerViewModel : ViewModelBase
    {
        private Location _selectedLocation;
        private Boolean _locationSelected;

        public LocationPickerViewModel(ILocated item)
        {
            BasinMapViewModel = new BasinMapViewModel(new List<BasinMapLayer>
            {
                // new BasinMapReaches() //marker isn't showing up when reaches are visible
            });

            BasinMapViewModel.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(BasinMapViewModel.SelectedPoint))
                    PointSelected(BasinMapViewModel.SelectedPoint);
            };
            
            if (item.Location.Latitude > 0 && item.Location.Longitude > 0)
                PointSelected(new MapPoint(item.Location.Longitude, item.Location.Latitude, new SpatialReference(item.Location.Wkid)));
        }

        public Location SelectedLocation
        {
            get => _selectedLocation;
            set => Set(ref _selectedLocation, value);
        }

        public Boolean LocationSelected
        {
            get => _locationSelected;
            set => Set(ref _locationSelected, value);
        }

        public BasinMapViewModel BasinMapViewModel { get; }

        private Int32? _selectedMarkerId;

        private void PointSelected(MapPoint point)
        {
            if (_selectedMarkerId != null)
                BasinMapViewModel.RemoveMarker(_selectedMarkerId.Value);
            
            var marker = new LocationMarker
            {
                Name = "Marker",
                Location = new Location
                {
                    Longitude = point.X,
                    Latitude = point.Y,
                    Color= Color.Red,
                    Symbol = Location.MapSymbol.X,
                    Wkid = point.SpatialReference.Wkid
                }
            };
            _selectedMarkerId = BasinMapViewModel.AddMarker(marker);
            SelectedLocation = marker.Location;
            LocationSelected = true;
        }

        private class LocationMarker : ILocated
        {
            public Location Location { get; set; }
            public String Name { get; set; }
        }

        public static void Picker(ILocated item)
        {
            var vm = new LocationPickerViewModel(item);
            var window = new LocationPickerWindow { DataContext = vm, Owner = Application.Current?.MainWindow };
            if (window.ShowDialog() != true) return;
            item.Location.Longitude = vm.SelectedLocation.Longitude;
            item.Location.Latitude = vm.SelectedLocation.Latitude;
            item.Location.Wkid = vm.SelectedLocation.Wkid;
        }
    }
}