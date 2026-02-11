using System;
using System.Collections.Generic;
using PicoGK;
using Leap71.ShapeKernel;

namespace MyFirstApp.Core.Engine
{
    public class EngineeringContext
    {
        // 1. GEOMETRY ACCUMULATOR
        public Voxels Assembly { get; set; }
        public Voxels PostProcessCuts { get; set; } // Section View Box

        // --- NEW: GLOBAL OPTIMIZATION LATTICES ---
        // Instead of voxelizing small things locally, add them here.
        // The SystemLoop will process them all at once (Batching).
        public Lattice GlobalSolids { get; private set; }
        public Lattice GlobalVoids { get; private set; }

        // 2. STATE
        public float CurrentZ { get; set; } = 0f;
        public float LastExitRadius { get; set; } = 0f;
        public float LastWallThickness { get; set; } = 5f;

        // 3. REGISTRIES
        public Dictionary<string, object> Data { get; private set; }
        public Dictionary<string, Func<float, float>> MathRegistry { get; private set; }
        public Dictionary<string, BaseShape> Shapes { get; private set; }

        public EngineeringContext()
        {
            Assembly = new Voxels();
            PostProcessCuts = new Voxels();
            
            // Initialize Global Lattices
            GlobalSolids = new Lattice();
            GlobalVoids = new Lattice();

            Data = new Dictionary<string, object>();
            MathRegistry = new Dictionary<string, Func<float, float>>();
            Shapes = new Dictionary<string, BaseShape>();
        }

        // --- HELPERS ---
        public void SetData(string key, object val) => Data[key] = val;
        
        public T GetData<T>(string key) 
        {
            if (Data.ContainsKey(key)) return (T)Data[key];
            return default!;
        }

        public void RegisterMath(string name, Func<float, float> function) => MathRegistry[name] = function;

        public Func<float, float>? GetMath(string name)
        {
            if (MathRegistry.ContainsKey(name)) return MathRegistry[name];
            return null;
        }

        public void RegisterShape(string name, BaseShape shape) => Shapes[name] = shape;
        
        public BaseShape? GetShape(string name)
        {
            if (Shapes.ContainsKey(name)) return Shapes[name];
            return null;
        }
    }
}