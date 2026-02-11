using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json; // Native .NET JSON
using System.Linq;
using System.Reflection;

namespace MyFirstApp.Core
{
    // The Data Structure for a Save File
    public class ProjectFile
    {
        public string Version { get; set; } = "1.0";
        public float VoxelSize { get; set; }
        
        // We store the Class Name so we know what to create
        public string RootObjectType { get; set; } = "";
        
        // We store the Slider Values
        public Dictionary<string, float> Parameters { get; set; } = new Dictionary<string, float>();
    }

    public static class ProjectIO
    {
        public static void SaveProject(EngineeringComponent root, string filePath)
        {
            if (root == null) return;

            ProjectFile file = new ProjectFile();
            file.VoxelSize = AppConfig.VoxelSize;
            
            // 1. Save Type (e.g. "MyFirstApp.Algorithms.Playground.CompressorGen")
            file.RootObjectType = root.GetType().FullName!;

            // 2. Save Parameters
            // We temporarily get the params to read their values
            var paramsList = root.GetParameters();
            foreach (var p in paramsList)
            {
                file.Parameters[p.Name] = p.Value;
            }

            // 3. Write to Disk
            string jsonString = JsonSerializer.Serialize(file, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, jsonString);
        }

        public static EngineeringComponent? LoadProject(string filePath)
        {
            try
            {
                string jsonString = File.ReadAllText(filePath);
                ProjectFile? file = JsonSerializer.Deserialize<ProjectFile>(jsonString);

                if (file == null) return null;

                // 1. Re-create the Object using Reflection
                // This searches your code for the class name string
                Type? type = Assembly.GetExecutingAssembly().GetType(file.RootObjectType);
                
                if (type == null) 
                {
                    System.Windows.Forms.MessageBox.Show($"Unknown Object Type: {file.RootObjectType}");
                    return null;
                }

                object? instance = Activator.CreateInstance(type);
                if (instance is EngineeringComponent comp)
                {
                    // 2. Restore Settings (Resolution)
                    if (Math.Abs(file.VoxelSize - AppConfig.VoxelSize) > 0.001f)
                    {
                        var res = System.Windows.Forms.MessageBox.Show(
                            $"Project uses {file.VoxelSize}mm resolution. Current is {AppConfig.VoxelSize}mm.\nUpdate and Restart?", 
                            "Resolution Mismatch", 
                            System.Windows.Forms.MessageBoxButtons.YesNo);
                        
                        if (res == System.Windows.Forms.DialogResult.Yes)
                        {
                            AppConfig.Save(file.VoxelSize);
                            System.Windows.Forms.Application.Restart();
                            Environment.Exit(0);
                        }
                    }

                    // 3. Restore Sliders
                    comp.ApplyParameterState(file.Parameters);
                    return comp;
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("Load Failed: " + ex.Message);
            }
            return null;
        }
    }
}