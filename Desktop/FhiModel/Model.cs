using System;
using System.IO;
using System.Runtime.Serialization;
using System.Threading;
using FhiModel.Common;
using FhiModel.EcosystemServices;
using FhiModel.EcosystemVitality;
using FhiModel.EcosystemVitality.Biodiversity;
using FhiModel.EcosystemVitality.DendreticConnectivity;
using FhiModel.EcosystemVitality.FlowDeviation;
using FhiModel.EcosystemVitality.WaterQuality;
using FhiModel.Governance;

namespace FhiModel
{
    [DataContract(Namespace = "", IsReference = true)]
    [KnownType(typeof(Indicator))]
    [KnownType(typeof(GovernanceIndicator))]
    [KnownType(typeof(SpeciesOfConcernIndicator))]
    [KnownType(typeof(InvasiveSpeciesIndicator))]
    [KnownType(typeof(ConnectivityIndicator))]
    [KnownType(typeof(EcosystemServicesIndicator))]
    [KnownType(typeof(GroundwaterStorageIndicator))]
    [KnownType(typeof(WaterQualityIndicator))]
    [KnownType(typeof(FlowDeviationIndicator))]
    [KnownType(typeof(ManualIndicator))]
    [KnownType(typeof(BankModificationIndicator))]
    [KnownType(typeof(LandCoverIndicator))]
    [KnownType(typeof(ConservationAreaIndicator))]
#pragma warning disable 612
    [KnownType(typeof(PotadromousIndicator))]
    [KnownType(typeof(DiadromousIndicator))]
#pragma warning restore 612
    public class Model : ModelBase
    {
        private Attributes _attributes;
        private IIndicator _governance;
        private IIndicator _ecosystemVitality;
        private IIndicator _ecosystemServices;
        private ModelAssets _assets;

        public Model(Guid userId)
        {
            OnDeserialized();

            Attributes.UserId = userId;
            Attributes.Created = DateTime.Now;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context = default(StreamingContext))
        {
            Attributes = Attributes ?? new Attributes();
            Assets = Assets ?? new ModelAssets();
            Governance = Governance ?? InitializeGovernance.Create();
            EcosystemServices = EcosystemServices ?? InitializeEcosystemServices.Create();
            EcosystemVitality = EcosystemVitality ?? InitializeEcosystemVitality.Create();

#region Backward Compatibility
#pragma warning disable 612
            var bc = EcosystemVitality.FetchIndicator<Indicator>("Basin Condition");
            PotadromousIndicator p = null;
            DiadromousIndicator d = null;
            foreach (var indicator in bc.Children)
            {
                switch (indicator)
                {
                    case PotadromousIndicator potadromousIndicator:
                        p = potadromousIndicator;
                        break;
                    case DiadromousIndicator diadromousIndicator:
                        d = diadromousIndicator;
                        break;
                }
            }
#pragma warning restore 612
            if (p != null || d != null)
            {
                bc.Children.Remove(p);
                bc.Children.Remove(d);
                var ci = new ConnectivityIndicator {Reaches = p?.Reaches};
                bc.Children.Add(ci);
                foreach (var child in bc.Children)
                    child.Weight = 1.0 / bc.Children.Count;
            }

            // bank modification and land cover indicators
            var cm = bc.FetchIndicator<BankModificationIndicator>();
            if (cm == null)
                bc.Children.Add(new BankModificationIndicator());
            var lc = bc.FetchIndicator<LandCoverIndicator>();
            if (lc == null)
                bc.Children.Add(new LandCoverIndicator());

            // old conservation areas manual indicator
            var mca = EcosystemServices.FetchIndicator<ManualIndicator>("Conservation Areas");
            if (mca != null)
            {
                var cai = new ConservationAreaIndicator();
                // preserve old data
                if (mca.Value != null || mca.UserOverride != null)
                {
                    cai.UserOverride = mca.Value ?? mca.UserOverride;
                    cai.OverrideComment = mca.OverrideComment ?? "User's manual input.";
                }
                cai.Weight = mca.Weight ?? 0.5;
                var culture = EcosystemServices.FetchIndicator<Indicator>("Cultural");
                culture.Children.Remove(mca);
                culture.Children.Add(cai);
            }
#endregion Backward Compatibility

        }

        [DataMember]
        public Attributes Attributes
        {
            get => _attributes;
            set => Set(ref _attributes, value);
        }

        [DataMember]
        public ModelAssets Assets
        {
            get => _assets;
            set => Set(ref _assets, value);
        }

        [DataMember]
        public IIndicator Governance
        {
            get => _governance;
            set => Set(ref _governance, value);
        }

        [DataMember]
        public IIndicator EcosystemVitality
        {
            get => _ecosystemVitality;
            set => Set(ref _ecosystemVitality, value);
        }

        [DataMember]
        public IIndicator EcosystemServices
        {
            get => _ecosystemServices;
            set => Set(ref _ecosystemServices, value);
        }

        public void Write(string filename, CancellationToken cancellationToken, IProgress<Int32> progress = null)
        {
            if (Path.GetExtension(filename) != ".fhix") throw new ArgumentException("We only write fhix files.");
            FhixZip.WriteZip(filename, this, cancellationToken, progress);
        }
        
        public static Model Read(string filename, CancellationToken cancellationToken, IProgress<Int32> progress = null)
        {
            if (Path.GetExtension(filename) != ".fhix") throw new ArgumentException("We only read fhix files.");
            return FhixZip.ReadZip(filename, cancellationToken, progress);
        }
    }
}