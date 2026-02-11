using System.Collections.Generic;
using PicoGK;
using Leap71.ShapeKernel;

namespace MyFirstApp.Core.Generative
{
    // 1. WHAT YOU WANT (User Inputs)
    // Instead of hardcoded variables, we use a dynamic dictionary.
    public class DesignRequirements
    {
        public Dictionary<string, float> NumericParams = new Dictionary<string, float>();
        public Dictionary<string, string> TextParams = new Dictionary<string, string>();

        public void Set(string key, float val) => NumericParams[key] = val;
        public void Set(string key, string val) => TextParams[key] = val;
        
        public float GetNum(string key, float defaultVal = 0) => NumericParams.ContainsKey(key) ? NumericParams[key] : defaultVal;
        public string GetText(string key, string defaultVal = "") => TextParams.ContainsKey(key) ? TextParams[key] : defaultVal;
    }

    // 2. THE BLUEPRINT (What the Solver decides to build)
    // The Solver will fill this list with "Steps".
    public class Blueprint
    {
        public List<IBldStep> Steps = new List<IBldStep>();
    }

    // 3. THE BUILDING BLOCK INTERFACE
    public interface IBldStep
    {
        void Execute(Voxels assembly);
    }
}