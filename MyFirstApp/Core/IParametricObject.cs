using System;
using System.Collections.Generic;
using MyFirstApp.Core.Engine;

namespace MyFirstApp.Core
{
    public class SimResult
    {
        public required string Label { get; set; }
        public required string Value { get; set; }
        public bool IsWarning { get; set; }
    }

    public class Parameter
    {
        public required string Name { get; set; }
        public float Value { get; set; }
        public float Min { get; set; }
        public float Max { get; set; }
        public required Action<float> OnChange { get; set; }
    }

    public interface IParametricObject
    {
        string Name { get; }
        
        // UPDATED: Now receives the EngineeringContext
        void Construct(EngineeringContext context);

        List<Parameter> GetParameters();
        List<SimResult> GetResults();
    }
}