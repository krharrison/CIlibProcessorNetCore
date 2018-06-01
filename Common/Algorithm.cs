using System.Collections.Generic;
using System.Linq;

namespace CIlibProcessor.Common
{
    public class Algorithm
    {
        public string Name { get; }

        private Dictionary<string, Measurement> Measurements { get; }

        public Algorithm(string name, Dictionary<string, Measurement> measurements)
        {
            Name = name;
            Measurements = measurements;
        }

        public override string ToString()
        {
            return $"[Algorithm: Name={Name}]";
        }

        public Measurement GetMeasurement(string measure)
        {
            return Measurements[measure];
        }

        public void AddMeasurement(Measurement measurement)
        {
            Measurements.Add(measurement.Name, measurement);
        }

        public Measurement GetFitness()
        {
            return Measurements["Fitness"];
        }

        public IEnumerable<string> GetMeasurementNames()
        {
            return Measurements.Keys;
        }
    }
}