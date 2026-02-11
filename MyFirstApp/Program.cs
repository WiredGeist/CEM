using System;
using System.IO;
using PicoGK;
using MyFirstApp.Projects;
using MyFirstApp.Core;

try
{
    // 1. Load Settings
    AppConfig.Load();
    float voxelSize = AppConfig.VoxelSize;

    // 2. Setup Unique Log Folder (FIX FOR CRASH)
    // We append a timestamp so every run gets its own folder.
    // This prevents "File in Use" errors during restart.
    string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
    string strLogFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log", timestamp);
    
    Directory.CreateDirectory(strLogFolder);

    Console.WriteLine($"Initializing PicoGK Library at {voxelSize}mm resolution...");
    Console.WriteLine($"Log Folder: {strLogFolder}");

    // 3. Start Engine
    Library.Go(
        voxelSize, 
        SystemLoop.Run, 
        strLogFolder
    );
}
catch (Exception e)
{
    Console.WriteLine("\n--- CRITICAL ERROR ---");
    Console.WriteLine(e.ToString());
    Console.WriteLine("\nPress ENTER to exit...");
    Console.ReadLine();
}