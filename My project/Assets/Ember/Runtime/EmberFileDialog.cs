using System;
using System.Runtime.InteropServices;

namespace Ember
{
    public static class EmberFileDialog
    {
        public static void Reveal(string path)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe","/select,\""+System.IO.Path.GetFullPath(path)+"\""){UseShellExecute=true});
#endif
        }
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] sealed class OpenFileName
        {
            public int size=Marshal.SizeOf(typeof(OpenFileName));public IntPtr owner,instance;
            public string filter="Ember backup\0*.ember\0\0",customFilter;public int maxCustomFilter,filterIndex=1;
            public string file=new string('\0',1024);public int maxFile=1024;public string fileTitle;public int maxFileTitle;
            public string initialDir=Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),title="Ember backup";
            public int flags;public short fileOffset,fileExtension;public string defaultExtension="ember";public IntPtr customData,hook;public string templateName;public IntPtr reserved;public int reservedInt,flagsEx;
        }
        [DllImport("comdlg32.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern bool GetSaveFileName([In,Out]OpenFileName data);
        [DllImport("comdlg32.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern bool GetOpenFileName([In,Out]OpenFileName data);
        public static string Choose(bool save){var data=new OpenFileName{flags=0x00080000|0x00000008|(save?0x00000002:0x00001000)};return (save?GetSaveFileName(data):GetOpenFileName(data))?data.file.Split('\0')[0]:null;}
#else
        public static string Choose(bool save)=>throw new PlatformNotSupportedException("Native backup picker is implemented for Windows. Mobile document provider is pending.");
#endif
    }
}
