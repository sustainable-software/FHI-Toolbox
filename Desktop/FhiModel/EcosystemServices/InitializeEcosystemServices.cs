using System.Collections.Generic;
using System.Collections.ObjectModel;
using FhiModel.Common;

namespace FhiModel.EcosystemServices
{
    public static class InitializeEcosystemServices
    {
        public static IIndicator Create()
        {
            var rv = new Indicator
            {
                Name = "Ecosystem Services",
                Children = new ObservableCollection<IIndicator>
                {
                    new Indicator
                    {
                        Name = "Provisioning",
                        Children = new ObservableCollection<IIndicator>
                        {
                            new EcosystemServicesIndicator
                            {
                                Name = "Water Supply Reliability"
                            },
                            new EcosystemServicesIndicator
                            {
                                Name = "Biomass"
                            },

                        }
                    },
                    new Indicator
                    {
                        Name = "Cultural",
                        Children = new ObservableCollection<IIndicator>
                        {
                            new ManualIndicator
                            {
                                Name = "Recreation"
                            },
                            new ConservationAreaIndicator
                            {
                                Name = "Conservation Areas"
                            },
                        }
                    },
                    new Indicator
                    {
                        Name = "Regulation",
                        Children = new ObservableCollection<IIndicator>()
                        {
                            new EcosystemServicesIndicator
                            {
                                Name = "Sediment Regulation"
                            },
                            new EcosystemServicesIndicator
                            {
                                Name = "Water Quality Regulation"
                            },
                            new EcosystemServicesIndicator
                            {
                                Name = "Disease Regulation"
                            },
                            new EcosystemServicesIndicator
                            {
                                Name = "Flood Regulation"
                            }
                        }
                    }
                }
            };
            rv.NormalizeWeights();
            return rv;
        }
    }
}