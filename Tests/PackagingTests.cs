using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using Minesweeper.Editor;

static class PackagingTests
{
    public static void Run()
    {
        string root = Path.Combine(Path.GetTempPath(), "Minesweeper-PackagingTests-" + Guid.NewGuid().ToString("N"));
        string player = Path.Combine(root, "Player");
        Directory.CreateDirectory(player);
        // No deletion: the unique, small fixture also provides failure diagnostics.
        foreach (string name in new[] { "Minesweeper.exe", "UnityPlayer.dll", "Minesweeper_Data/globalgamemanagers", "Minesweeper_Data/Managed/Assembly-CSharp.dll", "MonoBleedingEdge/EmbedRuntime/mono-2.0-bdwgc.dll", "D3D12/D3D12Core.dll", "game.pdb", "game.mdb", "Game_BackUpThisFolder_ButDontShipIt/private.txt", "Game_BurstDebugInformation_DoNotShip/debug.txt" })
        {
            string path = Path.Combine(player, name);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, "fixture: " + name);
        }
        string id = ReleasePackage.NewId("1.2.3-rc.1");
        string zip = ReleasePackage.Create(player, Path.Combine(root, "Stage"), Path.Combine(root, "Releases"), id, "1.2.3-rc.1", "test-editor", "遊び方");
        using (var archive = ZipFile.OpenRead(zip))
        {
            var entries = archive.Entries.ToDictionary(e => e.FullName.Replace('\\', '/'));
            Check(entries.Keys.All(n => n.StartsWith(id + "/")), "Single root directory");
            Check(entries.ContainsKey(id + "/D3D12/D3D12Core.dll"), "Retain supplemental runtime DLLs");
            Check(entries.Keys.All(n => !n.Contains("DontShip") && !n.Contains("DoNotShip") && !n.EndsWith(".pdb") && !n.EndsWith(".mdb")), "Do not distribute debug artifacts");
            using (var reader = new StreamReader(entries[id + "/SHA256SUMS.txt"].Open()))
            {
                string[] sums = reader.ReadToEnd().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                Check(sums.Length == entries.Count - 1, "Manifest covers every payload file");
                foreach (string line in sums)
                {
                    using (var stream = entries[id + "/" + line.Substring(66)].Open())
                        Check(Hash(stream) == line.Substring(0, 64), "ZIP contents match manifest");
                }
            }
        }
        using (var stream = File.OpenRead(zip))
            Check(File.ReadAllText(zip + ".sha256").StartsWith(Hash(stream) + "  "), "ZIP checksum matches");
        Expect<IOException>(() => ReleasePackage.Create(player, Path.Combine(root, "OtherStage"), Path.Combine(root, "Releases"), id, "1.2.3", "test", ""));
        Expect<ArgumentException>(() => ReleasePackage.ValidateVersion("../../escape"));
        Expect<ArgumentException>(() => ReleasePackage.ValidateVersion("1.0.0\n"));
        Expect<ArgumentException>(() => ReleasePackage.Create(player, Path.Combine(player, "Nested"), Path.Combine(root, "Releases"), "nested", "1.0.0", "test", ""));
        string missing = Path.Combine(root, "Missing");
        Directory.CreateDirectory(missing);
        Expect<IOException>(() => ReleasePackage.ValidatePlayer(missing));
        File.WriteAllText(Path.Combine(player, "UnityPlayer.dll"), "");
        Expect<IOException>(() => ReleasePackage.ValidatePlayer(player));
        Console.WriteLine("PASS: packaging, exclusions, all payload hashes, archive checksum, overwrite protection, invalid versions, missing runtime and recursive output guards.");
    }

    static string Hash(Stream stream) { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant(); }
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static void Expect<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new Exception("Expected " + typeof(T).Name); }
}
