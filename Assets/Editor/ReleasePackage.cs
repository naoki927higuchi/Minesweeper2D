using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Minesweeper.Editor
{
    // Engine-independent packaging, exercised by the standalone test runner.
    public static class ReleasePackage
    {
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);

        public static void ValidateVersion(string version)
        {
            if (version == null || !Regex.IsMatch(version, @"\A\d+\.\d+\.\d+(?:-[A-Za-z0-9]+(?:\.[A-Za-z0-9]+)*)?\z"))
                throw new ArgumentException("Version must be MAJOR.MINOR.PATCH, optionally with a suffix such as -rc.1.");
        }

        public static string NewId(string version)
        {
            ValidateVersion(version);
            return "Minesweeper-" + version + "-win-x64-" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        }

        public static string Create(string player, string staging, string releases, string id,
            string version, string unityVersion, string readme)
        {
            ValidateVersion(version);
            if (string.IsNullOrEmpty(id) || !Regex.IsMatch(id, @"\A[A-Za-z0-9][A-Za-z0-9._-]*\z"))
                throw new ArgumentException("Invalid package name.");
            ValidatePlayer(player);
            string package = Path.Combine(staging, id);
            if (Directory.Exists(package)) throw new IOException("Package staging already exists: " + package);
            string sourceRoot = Path.GetFullPath(player).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (Path.GetFullPath(package).StartsWith(sourceRoot, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Package staging must be outside the player directory.");
            Directory.CreateDirectory(package);
            CopyTree(player, package);
            ValidatePlayer(package);
            File.WriteAllText(Path.Combine(package, "README-ja.txt"), readme, Utf8);
            File.WriteAllText(Path.Combine(package, "build-info.txt"),
                "Product: Minesweeper\nVersion: " + version + "\nUnity: " + unityVersion +
                "\nPlatform: Windows x64\nBackend: Mono\nConfiguration: Release\nBuilt UTC: " +
                DateTime.UtcNow.ToString("O") + "\n", Utf8);
            string prefix = Path.GetFullPath(package).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var files = Directory.GetFiles(package, "*", SearchOption.AllDirectories).OrderBy(p => p, StringComparer.Ordinal).ToArray();
            File.WriteAllLines(Path.Combine(package, "SHA256SUMS.txt"), files.Select(path =>
                Hash(path) + "  " + Path.GetFullPath(path).Substring(prefix.Length).Replace('\\', '/')), Utf8);
            Directory.CreateDirectory(releases);
            string zip = Path.Combine(releases, id + ".zip");
            string checksum = zip + ".sha256";
            if (File.Exists(zip) || File.Exists(checksum)) throw new IOException("Release already exists: " + zip);
            // Failed runs leave staging for diagnosis; publish only a completed archive.
            string temporaryZip = Path.Combine(staging, id + ".partial.zip");
            ZipFile.CreateFromDirectory(package, temporaryZip, CompressionLevel.Optimal, true);
            using (var archive = ZipFile.OpenRead(temporaryZip))
            {
                var names = archive.Entries.Select(e => e.FullName.Replace('\\', '/')).ToArray();
                if (!names.Contains(id + "/Minesweeper.exe") || !names.Contains(id + "/SHA256SUMS.txt"))
                    throw new IOException("Invalid distribution archive.");
            }
            string zipHash = Hash(temporaryZip);
            File.Move(temporaryZip, zip);
            using (var stream = new FileStream(checksum, FileMode.CreateNew, FileAccess.Write))
            using (var writer = new StreamWriter(stream, Utf8)) writer.WriteLine(zipHash + "  " + Path.GetFileName(zip));
            return zip;
        }

        public static void ValidatePlayer(string root)
        {
            foreach (string relative in new[] { "Minesweeper.exe", "UnityPlayer.dll", "Minesweeper_Data/globalgamemanagers", "Minesweeper_Data/Managed/Assembly-CSharp.dll" })
            {
                string path = Path.Combine(root, relative);
                if (!File.Exists(path) || new FileInfo(path).Length == 0)
                    throw new IOException("Required player file missing or empty: " + relative);
            }
            string runtime = Path.Combine(root, "MonoBleedingEdge");
            if (!Directory.Exists(runtime) || Directory.GetFiles(runtime, "*.dll", SearchOption.AllDirectories).Length == 0)
                throw new IOException("Mono runtime missing.");
        }

        private static bool Excluded(string name)
        {
            return name.EndsWith("_BackUpThisFolder_ButDontShipIt", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith("_BurstDebugInformation_DoNotShip", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith(".mdb", StringComparison.OrdinalIgnoreCase);
        }

        private static void CopyTree(string source, string target)
        {
            if ((File.GetAttributes(source) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Symbolic links are not allowed in player output: " + source);
            Directory.CreateDirectory(target);
            foreach (string file in Directory.GetFiles(source))
            {
                if (Excluded(Path.GetFileName(file))) continue;
                if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("Symbolic links are not allowed: " + file);
                File.Copy(file, Path.Combine(target, Path.GetFileName(file)), false);
            }
            foreach (string directory in Directory.GetDirectories(source))
                if (!Excluded(Path.GetFileName(directory))) CopyTree(directory, Path.Combine(target, Path.GetFileName(directory)));
        }

        private static string Hash(string path)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(path))
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
    }
}
