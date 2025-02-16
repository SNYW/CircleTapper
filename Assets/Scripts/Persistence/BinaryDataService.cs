using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Persistence
{
    public class BinaryDataService : IDataService
    {
        private ISerializer _serializer;
        private string _dataPath;
        private string _fileExtension;

        public BinaryDataService(ISerializer serializer)
        {
            _serializer = serializer;
            _dataPath = Application.persistentDataPath;
            _fileExtension = "dat";
        }

        private string GetPathToFile(string fileName)
        {
            return Path.Combine(_dataPath, $"{fileName}.{_fileExtension}");
        }
        
        public void Save(string fileName, GameData data, bool overwrite = true)
        {
            var fileLocation = GetPathToFile(fileName);

            if (!overwrite && File.Exists(fileLocation))
            {
                throw new IOException($"Cannot overwrite {fileLocation}");
            }
            
            byte[] binaryData = _serializer.Serialize<GameData, byte[]>(data);
            File.WriteAllBytes(fileLocation, binaryData);
        }

        public GameData Load(string name)
        {
            var fileLocation = GetPathToFile(name);
            
            if (!File.Exists(fileLocation))
            {
                return new GameData{ currentPoints = 0 };
            }

            byte[] binaryData = File.ReadAllBytes(fileLocation);
            return _serializer.Deserialize<GameData, byte[]>(binaryData);
        }

        public void Delete(string name)
        {
            var fileLocation = GetPathToFile(name);
            
            if (!File.Exists(fileLocation))
            {
                throw new IOException($"File {fileLocation} cannot be deleted as it does not exist");
            }
            
            File.Delete(fileLocation);
        }

        public void DeleteAll()
        {
            foreach (var file in Directory.EnumerateFiles(_dataPath, $"*.{_fileExtension}"))
            {
                File.Delete(file);
            }
        }

        public IEnumerable<string> ListSaves()
        {
            return Directory.EnumerateFiles(_dataPath, $"*.{_fileExtension}")
                .Select(Path.GetFileNameWithoutExtension);
        }
    }
}
