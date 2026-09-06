using System;
using System.IO;
using UnityEngine;

namespace Persistence
{
    /// <summary>
    /// JSON save files in <see cref="Application.persistentDataPath"/>.
    /// <para>
    /// Writes are atomic: the payload lands in a temp file first, the previous save becomes the
    /// backup, and only then does the temp file take its place. A crash or a kill at any point
    /// leaves either the old save or the new one intact — never a half-written file.
    /// </para>
    /// </summary>
    public class FileDataService : IDataService
    {
        private readonly ISerializer _serializer;
        private readonly string _dataPath;

        private const string Extension = "json";
        private const string BackupExtension = "bak";
        private const string TempExtension = "tmp";

        /// <param name="dataPath">
        /// Where save files live. Defaults to <see cref="Application.persistentDataPath"/>; tests
        /// pass a temp directory, which is the only way to exercise the rotate-and-replace for
        /// real rather than against a fake.
        /// </param>
        public FileDataService(ISerializer serializer, string dataPath = null)
        {
            _serializer = serializer;
            _dataPath = string.IsNullOrEmpty(dataPath) ? Application.persistentDataPath : dataPath;
        }

        private string PathFor(string fileName) => Path.Combine(_dataPath, $"{fileName}.{Extension}");
        private string BackupPathFor(string fileName) => Path.Combine(_dataPath, $"{fileName}.{BackupExtension}");
        private string TempPathFor(string fileName) => Path.Combine(_dataPath, $"{fileName}.{TempExtension}");

        public void Save(string fileName, GameData data)
        {
            string json = _serializer.Serialize<GameData, string>(data);
            string target = PathFor(fileName);
            string temp = TempPathFor(fileName);
            string backup = BackupPathFor(fileName);

            // Write the new payload somewhere harmless first.
            File.WriteAllText(temp, json);

            // Rotate the current save out to the backup slot before overwriting it.
            if (File.Exists(target))
            {
                if (File.Exists(backup)) File.Delete(backup);
                File.Move(target, backup);
            }

            File.Move(temp, target);
        }

        public bool Exists(string fileName) => File.Exists(PathFor(fileName));

        public GameData Load(string fileName)
        {
            string path = PathFor(fileName);
            if (!File.Exists(path)) throw new FileNotFoundException($"No save at {path}", path);

            GameData data = _serializer.Deserialize<GameData, string>(File.ReadAllText(path));
            if (data == null) throw new InvalidDataException($"Save at {path} deserialized to null.");

            return data;
        }

        public bool TryLoadBackup(string fileName, out GameData data)
        {
            data = null;
            string backup = BackupPathFor(fileName);
            if (!File.Exists(backup)) return false;

            try
            {
                data = _serializer.Deserialize<GameData, string>(File.ReadAllText(backup));
                return data != null;
            }
            catch (Exception exception)
            {
                Debug.LogError($"Backup save at {backup} is also unreadable.\n{exception}");
                data = null;
                return false;
            }
        }

        public void QuarantineCorrupt(string fileName)
        {
            string path = PathFor(fileName);
            if (!File.Exists(path)) return;

            string quarantined = Path.Combine(
                _dataPath, $"{fileName}.corrupt.{DateTime.UtcNow:yyyyMMdd-HHmmss}.{Extension}");

            try
            {
                File.Move(path, quarantined);
                Debug.LogError($"Unreadable save preserved at {quarantined} rather than deleted.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"Could not quarantine the unreadable save at {path}.\n{exception}");
            }
        }

        public void Delete(string fileName)
        {
            foreach (string path in new[] { PathFor(fileName), BackupPathFor(fileName), TempPathFor(fileName) })
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }
}
