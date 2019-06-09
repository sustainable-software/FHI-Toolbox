using System.Collections.Generic;
using System.Collections.ObjectModel;
using FhiModel.Common;

namespace FhiModel.Governance
{
    public static class InitializeGovernance
    {
        public static IIndicator Create()
        {
            var rv = new Indicator
            {
                // NOTICE: THESE NAMES ARE HARD-WIRED INTO Step2ViewModel.cs for auto-assignment.
                Name = "Governance",
                Children = new ObservableCollection<IIndicator>
                {
                    new Indicator
                    {
                        Name = "Effectiveness",
                        Children = new ObservableCollection<IIndicator>
                        {
                            new GovernanceIndicator
                            {
                                Name = "Enforcement & compliance"
                            },
                            new GovernanceIndicator
                            {
                                Name = "Distribution of benefits"
                            },
                            new GovernanceIndicator
                            {
                                Name = "Water-related conflict"
                            }
                        }
                    },
                    new Indicator
                    {
                        Name = "Stakeholder Engagement",
                        Children = new ObservableCollection<IIndicator>
                        {
                            new GovernanceIndicator
                            {
                                Name = "Information access"
                            },
                            new GovernanceIndicator
                            {
                                Name = "Engagement in decision-making"
                            }
                        }
                    },
                    new Indicator
                    {
                        Name = "Vision & Adaptive Governance",
                        Children = new ObservableCollection<IIndicator>
                        {
                            new GovernanceIndicator
                            {
                                Name = "Monitoring mechanisms"
                            },
                            new GovernanceIndicator
                            {
                                Name = "Strategic planning"
                            }
                        }
                    },
                    new Indicator
                    {
                        Name = "Enabling Environment",
                        Children = new ObservableCollection<IIndicator>
                        {
                            new GovernanceIndicator
                            {
                                Name = "Water resources management"
                            },
                            new GovernanceIndicator
                            {
                                Name = "Right to resource use"
                            },
                            new GovernanceIndicator
                            {
                                Name = "Incentives & regulations"
                            },
                            new GovernanceIndicator
                            {
                                Name = "Financial capacity"
                            },
                            new GovernanceIndicator
                            {
                                Name = "Technical capacity"
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