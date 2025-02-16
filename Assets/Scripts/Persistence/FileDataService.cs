using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Persistence
{
    public class FileDataService : IDataService
    {
        private ISerializer _serializer;
        private string _dataPath;
        private string _fileExtension;

        public FileDataService(ISerializer serializer)
        {
            _serializer = serializer;
            _dataPath = Application.persistentDataPath;
            _fileExtension = "json";
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
            
            string jsonData = _serializer.Serialize<GameData, string>(data);
            File.WriteAllText(fileLocation, jsonData);
            
            Debug.Log($"[JsonDataService] Saved at {fileLocation}");
        }

        public GameData Load(string name)
        {
            var fileLocation = GetPathToFile(name);
            
            if (!File.Exists(fileLocation))
            {
                throw new IOException($"File {fileLocation} cannot be loaded as it does not exist");
            }

            string jsonData = File.ReadAllText(fileLocation);
            return _serializer.Deserialize<GameData, string>(jsonData);
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