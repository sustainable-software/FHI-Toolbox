using System.Collections.Generic;
using System.Collections.ObjectModel;
using FhiModel.Common;
using FhiModel.EcosystemVitality.Biodiversity;
using FhiModel.EcosystemVitality.DendreticConnectivity;
using FhiModel.EcosystemVitality.FlowDeviation;
using FhiModel.EcosystemVitality.WaterQuality;

namespace FhiModel.EcosystemVitality
{
    public static class InitializeEcosystemVitality
    {
        public static IIndicator Create()
        {
            var rv = new Indicator
            {
                Name = "Ecosystem Vitality",
                Children = new ObservableCollection<IIndicator>
                {
                    new Indicator
                    {
                        Name = "Water Quantity",
                        Children = new ObservableCollection<IIndicator>
                        {
                            new FlowDeviationIndicator
                            {
                                Name = "Flow Deviation"
                            },
                            new GroundwaterStorageIndicator
                            {
                                Name = "Groundwater Storage"
                            },

                        }
                    },
                    new Indicator
                    {
                        Name = "Biodiversity",
                        Children = new ObservableCollection<IIndicator>
                        {
                            new SpeciesOfConcernIndicator
                            {
                                Name = "Species of Concern"
                            },
                            new InvasiveSpeciesIndicator
                            {
                                Name = "Invasive Species"
                            }
                        }
                    },
                    new WaterQualityIndicator
                    {
                        Name = "Water Quality",
                    },
                    new Indicator
                    {
                        Name = "Basin Condition",
                        Children = new ObservableCollection<IIndicator>
                        {
                            new ConnectivityIndicator()
                            {
                                Name = "Connectivity"
                            },
                            new BankModificationIndicator
                            {
                                Name = "Bank Modification"
                            },
                            new LandCoverIndicator
                            {
                                Name = "Land Cover Naturalness"
                            }
                        }
                    },
                }
            };
            rv.NormalizeWeights();
            return rv;
        }
    }
}