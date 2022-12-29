using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;

namespace SDL2
{
    public static class DllMap
    {
        private static bool _registered = false;

        public static void RegisterDllMap()
        {
            if (_registered) return;
            var currentAssembly = Assembly.GetExecutingAssembly();
            NativeLibrary.SetDllImportResolver(currentAssembly, MapAndLoad);
            _registered = true;
        }

        private static IntPtr MapAndLoad(string libraryName, Assembly assembly,
            DllImportSearchPath? dllImportSearchPath)
        {
            string mappedName;
            mappedName = MapLibraryName(assembly.Location, libraryName, out mappedName)
                ? mappedName
                : libraryName;
            return NativeLibrary.Load(mappedName, assembly, dllImportSearchPath);
        }

        // Parse the assembly.xml file, and map the old name to the new name of a library.
        private static bool MapLibraryName(string assemblyLocation, string originalLibName, out string mappedLibName)
        {
            var xmlPath = Path.Combine(Path.GetDirectoryName(assemblyLocation) ?? string.Empty, "DllMap.xml");
            mappedLibName = null;
            var os = "windows";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                os = "osx";
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                os = "linux";

            var arch = RuntimeInformation.OSArchitecture switch
            {
                Architecture.X64 => "x64",
                Architecture.X86 => "x86",
                _ => "ignore"
            };

            if (!File.Exists(xmlPath))
                return false;

            var root = XElement.Load(xmlPath);
            var map =
                (from el in root.Elements("dllmap")
                    where (string)el.Attribute("dll") == originalLibName
                    where (string)el.Attribute("os") == os
                    where arch == (string)el.Attribute("arch") || arch == "ignore"
                    select el).SingleOrDefault();

            if (map != null)
                mappedLibName = map.Attribute("target")?.Value;

            return (mappedLibName != null);
        }
    }
}
