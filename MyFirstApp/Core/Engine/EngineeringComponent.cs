using System;
using System.Collections.Generic;
using PicoGK;
using MyFirstApp.Core.Engine;

namespace MyFirstApp.Core
{
    // --- 1. ADD THIS STRUCT (Required for UI Calculations) ---
    public struct SimulationData
    {
        public string Label;
        public string Value;
        public bool IsWarning;
    }

    public abstract class EngineeringComponent
    {
        // ==========================================
        // 1. PROPERTIES
        // ==========================================
        public string Name { get; set; } = "Unnamed";
        
        // Master toggle to show/hide this part
        public bool Enabled { get; set; } = true;
        
        // Tree Structure
        public EngineeringComponent? Parent { get; set; }
        public List<EngineeringComponent> Children { get; private set; } = new List<EngineeringComponent>();

        // ==========================================
        // 2. CACHING STATE
        // ==========================================
        protected Voxels? m_CachedVoxels = null;
        protected float m_CachedInputRadius = -1f; 
        public bool IsDirty { get; set; } = true;
        protected float m_MyStartZ = 0f;

        // ==========================================
        // 3. PARAMETER HANDLING
        // ==========================================
        public List<Parameter> GetParameters()
        {
            var list = new List<Parameter>();
            
            // Auto-add the Header/Enabled switch
            list.Add(new Parameter { 
                Name = $"[ {Name.ToUpper()} ]", 
                Value = Enabled ? 1 : 0, 
                Min = 0, Max = 1, 
                OnChange = v => { Enabled = v > 0.5f; IsDirty = true; } 
            });

            // Add component-specific parameters
            foreach(var p in GetComponentParameters())
            {
                var originalAction = p.OnChange;
                p.OnChange = (val) => {
                    originalAction(val);
                    this.IsDirty = true; 
                };
                list.Add(p);
            }
            return list;
        }

        public void ApplyParameterState(Dictionary<string, float> savedState)
        {
            var currentParams = GetParameters();
            foreach (var p in currentParams)
            {
                if (savedState.ContainsKey(p.Name))
                {
                    float savedValue = savedState[p.Name];
                    p.Value = savedValue;
                    p.OnChange(savedValue);
                }
            }
        }

        protected abstract List<Parameter> GetComponentParameters();

        // --- 2. ADD THIS METHOD (Fixes CS1061 Error) ---
        public virtual List<SimulationData> GetResults() 
        { 
            return new List<SimulationData>(); 
        }

        // ==========================================
        // 4. LIFECYCLE METHODS
        // ==========================================

        public virtual void CalculatePhysics(EngineeringContext ctx) { }

        public virtual void OnSetup(EngineeringContext ctx) 
        {
            m_MyStartZ = ctx.CurrentZ;
        }

        public virtual void OnPreview(EngineeringContext ctx) { }

        protected abstract void OnConstruct(EngineeringContext ctx);

        // ==========================================
        // 5. THE MASTER BUILD FUNCTION
        // ==========================================
        public void Build(EngineeringContext ctx)
        {
            if (!Enabled) return;

            // A. Run Physics
            CalculatePhysics(ctx);

            // B. Run Logic
            float inputRadius = ctx.LastExitRadius;
            OnSetup(ctx); 

            // C. Run Preview
            OnPreview(ctx);

            // D. Run Construction (Cached)
            bool upstreamChanged = Math.Abs(inputRadius - m_CachedInputRadius) > 0.1f;

            if (m_CachedVoxels == null || IsDirty || upstreamChanged)
            {
                m_CachedInputRadius = inputRadius;

                Voxels tempContainer = new Voxels();
                Voxels mainAssembly = ctx.Assembly;
                ctx.Assembly = tempContainer;

                float actualCurrentZ = ctx.CurrentZ; 
                ctx.CurrentZ = m_MyStartZ; 

                try 
                {
                    OnConstruct(ctx);
                }
                finally 
                {
                    ctx.Assembly = mainAssembly;
                    ctx.CurrentZ = actualCurrentZ; 
                }

                m_CachedVoxels = tempContainer;
                IsDirty = false;
            }

            // E. Composite
            if (m_CachedVoxels != null)
            {
                ctx.Assembly.BoolAdd(m_CachedVoxels);
            }
        }

        // ==========================================
        // 6. HELPER METHODS
        // ==========================================
        public void AddChild(EngineeringComponent child)
        {
            child.Parent = this;
            Children.Add(child);
        }
    }
}