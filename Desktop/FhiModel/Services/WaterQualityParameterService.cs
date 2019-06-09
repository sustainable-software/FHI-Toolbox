using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using FhiModel.Common.Timeseries;
using FhiModel.EcosystemVitality.WaterQuality;

namespace FhiModel.Services
{
    public static class WaterQualityParameterService
    {
        public static IEnumerable<WaterQualityParameter> Parameters { get; } = new[]
        {
            new WaterQualityParameter
            {
                Name = "Ag (Silver)",
                Units = "ug/L",
                Objective = new Objective
                {
                    Function = new ObjectiveFunctionGreaterThan(),
                    Metrics = new ObservableCollection<ObjectiveMetric>
                    {
                        new ObjectiveMetricSingleValue
                        {
                            Start = new DateTime(2019, 1, 1),
                            End = new DateTime(2019, 12, 31),
                            Value = 0.1
                        }
                    }
                }
            },
            new WaterQualityParameter
            {
                Name = "As (Arsenic)",
                Units = "ug/L",
                Objective = new Objective
                {
                    Function = new ObjectiveFunctionGreaterThan(),
                    Metrics = new ObservableCollection<ObjectiveMetric>
                    {
                        new ObjectiveMetricSingleValue
                        {
                            Start = new DateTime(2019, 1, 1),
                            End = new DateTime(2019, 12, 31),
                            Value = 5
                        }
                    }
                }
            },
            new WaterQualityParameter
            {
                Name = "Chloride",
                Units = "mg/L",
                Objective = new Objective
                {
                    Function = new ObjectiveFunctionGreaterThan(),
                    Metrics = new ObservableCollection<ObjectiveMetric>
                    {
                        new ObjectiveMetricSingleValue
                        {
                            Start = new DateTime(2019, 1, 1),
                            End = new DateTime(2019, 12, 31),
                            Value = 150
                        }
                    }
                }
            },
            new WaterQualityParameter
            {
                Name = "Chlorophyll",
                Units = "mg/L",
                Objective = new Objective
                {
                    Function = new ObjectiveFunctionGreaterThan(),
                    Metrics = new ObservableCollection<ObjectiveMetric>
                    {
                        new ObjectiveMetricSingleValue
                        {
                            Start = new DateTime(2019, 1, 1),
                            End = new DateTime(2019, 12, 31),
                        }
                    }
                }
            },
            new WaterQualityParameter
            {
                Name = "Cr-III (Chromium III)",
                Units = "ug/L",
                Objective = new Objective
                {
                    Function = new ObjectiveFunctionGreaterThan(),
                    Metrics = new ObservableCollection<ObjectiveMetric>
                    {
                        new ObjectiveMetricSingleValue
                        {
                            Start = new DateTime(2019, 1, 1),
                            End = new DateTime(2019, 12, 31),
                            Value = 8.9
                        }
                    }
                }
            },
            new WaterQualityParameter
            {
                Name = "Cr-VI (Chromium VI)",
                Units = "ug/L",
                Objective = new Objective
                {
                    Function = new ObjectiveFunctionGreaterThan(),
                    Metrics = new ObservableCollection<ObjectiveMetric>
                    {
                        new ObjectiveMetricSingleValue
                        {
                            Start = new DateTime(2019, 1, 1),
                            End = new DateTime(2019, 12, 31),
                            Value = 1
                        }
                    }
                }
            },
            new WaterQualityParameter
            {
                Name = "DO (Dissolved Oxygen)",
                Units = "mg/L",
                Objective = new Objective
                {
                    Function = new ObjectiveFunctionRange(),
                    Metrics = new ObservableCollection<ObjectiveMetric>
                    {
                        new ObjectiveMetricRange
                        {
                            Start = new DateTime(2019, 1, 1),
                            End = new DateTime(2019, 12, 31),
                            Minimum = 5.5,
                            Maximum = 9.5
                        }
                    }
                }
            },
            new WaterQualityParameter
            {
                Name = "Fe (Iron)",
                Units = "ug/L",
                Objective = new Objective
                {
                    Function = new ObjectiveFunctionGreaterThan(),
                    Metrics = new ObservableCollection<ObjectiveMetric>
                    {
                        new ObjectiveMetricSingleValue
                        {
                            Start = new DateTime(2019, 1, 1),
                            End = new DateTime(2019, 12, 31),
                            Value = 300
                        }
                    }
                }
            },
            new WaterQualityParameter
            {
                Name = "Hg (Inorganic Mercury)",
                Units = "ug/L",
                Objective = new Objective
                {
                    Function = new ObjectiveFunctionGreaterThan(),
                    Metrics = new ObservableCollection<ObjectiveMetric>
                    {
                        new ObjectiveMetricSingleValue
                        {
                            Start = new DateTime(2019, 1, 1),
                            End = new DateTime(2019, 12, 31),
                            Value = 0.026
                        }
                    }
                }
            },
            new WaterQualityParameter
            {
                Name = "MeHg (Methyl Mercury)",
                Units = "ug/L",
                Objective = new Objective
                {
                    Function = new ObjectiveFunctionGreaterThan(),
                    Metrics = new ObservableCollection<ObjectiveMetric>
                    {
                        new ObjectiveMetricSingleValue
                        {
                            Start = new DateTime(2019, 1, 1),
                            End = new DateTime(2019, 12, 31),
                            Value = 0.004
                        }
                    }
                }
            },
            new WaterQualityParameter
            {
                Name = "Mo (Molybdenum)",
                Units = "ug/L",
                Objective = new Objective
                {
                    Function = new ObjectiveFunctionGreaterThan(),
                    Metrics = new ObservableCollection<ObjectiveMetric>
                    {
                        new ObjectiveMetricSingleValue
                        {
                            Start = new DateTime(2019, 1, 1),
                            End = new DateTime(2019, 12, 31),
                            Value = 73
                        }
                    }
                }
            },
            new WaterQualityParameter
            {
                Name = "N (Nitrogen)",
                Units = "mg/L",
                Objective = new Objective
                {
                    Function = new ObjectiveFunctionGreaterThan(),
                    Metrics = new ObservableCollection<ObjectiveMetric>
                    {
                        new ObjectiveMetricSingleValue
                        {
                            Start = new DateTime(2019, 1, 1),
                            End = new DateTime(2019, 12, 31),
                        }
                    }
                }
            },
            new WaterQualityParameter
            {
                Name = "NO3 (Nitrate)",
                Units = "mg/L",
                Objective = new Objective
                {
                    Function = new ObjectiveFunctionGreaterThan(),
                    Metrics = new ObservableCollection<ObjectiveMetric>
                    {
                        new ObjectiveMetricSingleValue
                        {
                            Start = new DateTime(2019, 1, 1),
                            End = new DateTime(2019, 12, 31),
                            Value = 13
                        }
                    }
                }
            },
            new WaterQualityParameter
            {
                Name = "Ph",
                Objective = new Objective
                {
                    Function = new ObjectiveFunctionRange(),
                    Metrics = new ObservableCollection<ObjectiveMetric>
                    {
                        new ObjectiveMetricRange
                        {
                            Start = new DateTime(2019, 1, 1),
                            End = new DateTime(2019, 12, 31),
                            Minimum = 6.5,
                            Maximum = 9
                        }
                    }
                }
            },
            new WaterQualityParameter
            {
                Name = "P (Phosphorous)",
                Units = "mg/L",
                Objective = new Objective
                {
                    Function = new ObjectiveFunctionGreaterThan(),
                    Metrics = new ObservableCollection<ObjectiveMetric>
                    {
                        new ObjectiveMetricSingleValue
                        {
                            Start = new DateTime(2019, 1, 1),
                            End = new DateTime(2019, 4, 30),
                            Value = 0.01
                        },
                        new ObjectiveMetricSingleValue
                        {
                            Start = new DateTime(2019, 5, 1),
                            End = new DateTime(2019, 10, 31),
                            Value = 0.02
                        },
                        new ObjectiveMetricSingleValue
                        {
                            Start = new DateTime(2019, 11, 1),
                            End = new DateTime(2019, 12, 31),
                            Value = 0.01
                        }
                    }
                }
            },
            new WaterQualityParameter
            {
                Name = "Se (Selenium)",
                Units = "ug/L",
                Objective = new Objective
                {
                    Function = new ObjectiveFunctionGreaterThan(),
                    Metrics = new ObservableCollection<ObjectiveMetric>
                    {
                        new ObjectiveMetricSingleValue
                        {
                            Start = new DateTime(2019, 1, 1),
                            End = new DateTime(2019, 12, 31),
                            Value = 1
                        }
                    }
                }
            },
            new WaterQualityParameter
            {
                Name = "Th (Thallium)",
                Units = "ug/L",
                Objective = new Objective
                {
                    Function = new ObjectiveFunctionGreaterThan(),
                    Metrics = new ObservableCollection<ObjectiveMetric>
                    {
                        new ObjectiveMetricSingleValue
                        {
                            Start = new DateTime(2019, 1, 1),
                            End = new DateTime(2019, 12, 31),
                            Value = 0.8
                        }
                    }
                }
            },
            new WaterQualityParameter
            {
                Name = "Zn (Zinc)",
                Units = "ug/L",
                Objective = new Objective
                {
                    Function = new ObjectiveFunctionGreaterThan(),
                    Metrics = new ObservableCollection<ObjectiveMetric>
                    {
                        new ObjectiveMetricSingleValue
                        {
                            Start = new DateTime(2019, 1, 1),
                            End = new DateTime(2019, 12, 31),
                            Value = 30
                        }
                    }
                }
            },
        };
    }
}