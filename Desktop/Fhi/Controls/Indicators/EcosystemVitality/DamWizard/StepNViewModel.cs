using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Fhi.Controls.MVVM;
using Fhi.Controls.Network;
using Fhi.Controls.Wizard;
using FhiModel.Common;
using FhiModel.EcosystemVitality.DendreticConnectivity;

namespace Fhi.Controls.Indicators.EcosystemVitality.DamWizard
{
    public class StepNViewModel : WizardStepViewModel
    {
        private readonly IList<DamTableRow> _dams;
        private Int32 _currentIndex;
        private DamTableRow _dam;
        private Boolean _actionComplete;
        private Node _assignedNode;

        public StepNViewModel(IList<DamTableRow> dams, IList<Reach> reaches, int wkid)
        {
            _dams = dams;

            AcceptCommand = new RelayCommand(Accept);
            DeleteCommand = new RelayCommand(Delete);

            BasinMapViewModel = new BasinMapViewModel(reaches, wkid, true);
            BasinMapViewModel.SelectionOverlays.Add("Nodes");    // users only select nodes

            BasinMapViewModel.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName != nameof(BasinMapViewModel.SelectedId)) return;

                if (!(BasinMapViewModel.GetSelectedModel() is Node node)) return;
                if (AssignedNode != null)
                {
                    AssignedNode.Dam = null;
                    AssignedNode.Location.Color = Color.Black;
                    BasinMapViewModel.UpdateModelToMap();
                }
                node.Dam = new Dam(Dam);
                node.Location.Color = Color.Red;
                BasinMapViewModel.UpdateModelToMap();
                AssignedNode = node;
                ActionComplete = true;
            };

            Dam = _dams[_currentIndex];
            var id = BasinMapViewModel.AddMarker(new DamMarker(Dam));
            BasinMapViewModel.ZoomToMarker(id);
        }

        public ICommand AcceptCommand { get; }
        public ICommand DeleteCommand { get; }

        public BasinMapViewModel BasinMapViewModel { get; }

        public override Boolean ReadyForNext => _dams.Count == _currentIndex;

        public Int32 Remaining => _dams.Count - _currentIndex - 1;

        public Boolean ActionComplete
        {
            get => _actionComplete;
            set => Set(ref _actionComplete, value);
        }

        public Node AssignedNode
        {
            get => _assignedNode;
            set => Set(ref _assignedNode, value);
        }

        public DamTableRow Dam
        {
            get => _dam;
            set => Set(ref _dam, value);
        }

        private void Advance()
        {
            BasinMapViewModel.ClearMarkers();
            _currentIndex++;
            if (_currentIndex == _dams.Count)
            {
                RaisePropertyChanged(nameof(ReadyForNext));
                return;
            }
            Dam = _dams[_currentIndex];
            var id = BasinMapViewModel.AddMarker(new DamMarker(Dam));
            BasinMapViewModel.ZoomToMarker(id);
            ActionComplete = false;
            AssignedNode = null;
            RaisePropertyChanged(nameof(Remaining));
        }

        private void Accept()
        {
            Advance();
        }

        private void Delete()
        {
            if (AssignedNode != null)
                AssignedNode.Dam = null;
            Advance();
        }
    }

    public class DamMarker : ILocated
    {
        public DamMarker(DamTableRow dam)
        {
            Name = dam.Name;
            Location = new Location
            {
                Latitude = dam.Location.Latitude,
                Longitude = dam.Location.Longitude,
                Color = Color.Purple,
                Symbol = Location.MapSymbol.X
            };
        }
        public String Name { get; }
        public Location Location { get; }
    }
}

