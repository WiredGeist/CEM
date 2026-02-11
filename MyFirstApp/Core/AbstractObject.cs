using System;
using System.Collections.Generic;
using System.Numerics;
using Leap71.ShapeKernel;
using MyFirstApp.Core.Engine;

namespace MyFirstApp.Core
{
    public abstract class AbstractObject : IParametricObject
    {
        public abstract string Name { get; }
        protected Vector3 m_vecPosition = Vector3.Zero;

        protected abstract List<Parameter> GetObjectParameters();
        
        // Abstract method now takes Context
        public abstract void Construct(EngineeringContext context);

        public List<Parameter> GetParameters()
        {
            List<Parameter> parameters = GetObjectParameters();
            
            parameters.Add(new Parameter { Name = "--- TRANSFORMS ---", Value = 0, Min = 0, Max = 0, OnChange = v => {} });
            parameters.Add(new Parameter { Name = "Position X", Value = m_vecPosition.X, Min = -200, Max = 200, OnChange = v => m_vecPosition.X = v });
            parameters.Add(new Parameter { Name = "Position Y", Value = m_vecPosition.Y, Min = -200, Max = 200, OnChange = v => m_vecPosition.Y = v });
            parameters.Add(new Parameter { Name = "Position Z", Value = m_vecPosition.Z, Min = -200, Max = 200, OnChange = v => m_vecPosition.Z = v });

            return parameters;
        }

        public virtual List<SimResult> GetResults()
        {
            return new List<SimResult>();
        }

        protected LocalFrame GetFrame()
        {
            return new LocalFrame(m_vecPosition);
        }
    }
}