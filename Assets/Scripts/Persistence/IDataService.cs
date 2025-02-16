using System.Collections.Generic;

namespace Persistence
{
    public interface IDataService
    {
        void Save(string fileName, GameData data, bool overwrite = true);
        GameData Load(string name);
        void Delete(string name);
        void DeleteAll();
        IEnumerable<string> ListSaves();
    }
}