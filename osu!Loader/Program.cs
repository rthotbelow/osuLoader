using Microsoft.Win32;
using System;
using System.IO;
using System.Reflection;
using System.Security.Permissions;

namespace osuLoader
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\" != getOsuPath())
            {
                Console.WriteLine($"Please place {Path.GetFileName(Assembly.GetExecutingAssembly().Location)} into {getOsuPath()}.");
                Console.ReadKey();
                return;
            }

            Assembly asm = Assembly.LoadFile(getOsuPath() + "osu!.exe");

            Console.WriteLine($"osu! Entry Point: {asm.EntryPoint.Name}");

            Type Color = asm.GetType("Microsoft.Xna.Framework.Graphics.Color");

            Type OsuMain             = asm.GetType(AsmEncrypt.class_OsuMain);
            Type Menu                = asm.GetType(AsmEncrypt.class_Menu);
            Type VoidDelegate        = asm.GetType(AsmEncrypt.delegate_VoidDelegate);
            Type BanchoClient        = asm.GetType(AsmEncrypt.class_BanchoClient);
            Type NotificationManager = asm.GetType(AsmEncrypt.class_NotificationManager);
            Type AuthenticodeTools   = asm.GetType(AsmEncrypt.class_AuthenticodeTools);

            MethodInfo FullPath         = OsuMain.GetMethod(AsmEncrypt.method_FullPath, BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo FullPath_patched = typeof(MthdPatch).GetMethod("FullPath");

            MethodInfo Filename         = OsuMain.GetMethod(AsmEncrypt.method_Filename, BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo Filename_patched = typeof(MthdPatch).GetMethod("Filename");

            MethodInfo IsTrusted         = AuthenticodeTools.GetMethod(AsmEncrypt.method_IsTrusted, BindingFlags.Static | BindingFlags.Public);
            MethodInfo IsTrusted_patched = typeof(MthdPatch).GetMethod("IsTrusted");

            MethodInfo ChangeOnlineImage = Menu.GetMethod(AsmEncrypt.method_ChangeOnlineImage, BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(string), typeof(string) }, null);
            MethodInfo SetServer         = BanchoClient.GetMethod(AsmEncrypt.method_SetServer, BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(string[]) }, null);
            MethodInfo ShowMessage       = NotificationManager.GetMethod(AsmEncrypt.method_ShowMessage, BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(string), Color, typeof(int), VoidDelegate }, null);

            Console.WriteLine($"[Unpatched] FullPath(): {FullPath.Invoke(null, null).ToString()}");
            Console.WriteLine($"[Unpatched] Filename(): {Filename.Invoke(null, null).ToString()}");
            Console.WriteLine($"[Unpatched] IsTrusted(string): {IsTrusted.Invoke(null, new object[] { "osu!Loader.exe" }).ToString()}");

            unsafe
            {
                // Patch out executable name/path checks
                int* p_FullPath         = (int*)FullPath.MethodHandle.Value.ToPointer()         + 2;
                int* p_FullPath_patched = (int*)FullPath_patched.MethodHandle.Value.ToPointer() + 2;

                int* p_Filename         = (int*)Filename.MethodHandle.Value.ToPointer()         + 2;
                int* p_Filename_patched = (int*)Filename_patched.MethodHandle.Value.ToPointer() + 2;

                *p_FullPath = *p_FullPath_patched;
                *p_Filename = *p_Filename_patched;

                // Set server endpoints
                SetServer.Invoke(null, new object[] { new string[] { "https://c.ripple.moe" } });

                // Patch out signature checks
                int* p_verifySigMethod      = (int*)IsTrusted.MethodHandle.Value.ToPointer()      + 2;
                int* p_verifySigReplacement = (int*)IsTrusted_patched.MethodHandle.Value.ToPointer() + 2;
                
                *p_verifySigMethod = *p_verifySigReplacement;
            }

            Console.WriteLine($"[Patched] FullPath(): {FullPath.Invoke(null, null).ToString()}");
            Console.WriteLine($"[Patched] Filename(): {Filename.Invoke(null, null).ToString()}");
            Console.WriteLine($"[Patched] IsTrusted(string): {IsTrusted.Invoke(null, new object[] { "osu!Loader.exe" }).ToString()}");

            ChangeOnlineImage.Invoke(null, new object[] { "https://i.imgur.com/hPBvqUq.png", "http://google.com" });
            ShowMessage.Invoke(null, new object[] { "osu!Loader is now running.", Color.GetMethod("get_Orange", BindingFlags.Static | BindingFlags.Public).Invoke(null, null), 20000, null });

            new ReflectionPermission(ReflectionPermissionFlag.RestrictedMemberAccess).Assert();
            asm.EntryPoint.Invoke(null, null);
        }

        public static string getOsuPath()
        {
            using (RegistryKey osuReg = Registry.ClassesRoot.OpenSubKey("osu\\DefaultIcon"))
            {
                if (osuReg != null)
                {
                    string osuKey = osuReg.GetValue(null).ToString();
                    string osuPath;
                    osuPath = osuKey.Remove(0, 1);
                    osuPath = osuPath.Remove(osuPath.Length - 11);

                    return osuPath;
                }

                return string.Empty;
            }
        }
    }
}
