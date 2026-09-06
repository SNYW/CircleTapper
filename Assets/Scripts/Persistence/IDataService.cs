namespace Persistence
{
    public interface IDataService
    {
        /// <summary>Writes atomically, keeping the previous save as a backup.</summary>
        void Save(string fileName, GameData data);

        /// <summary>True if a primary save file exists. Check before <see cref="Load"/>.</summary>
        bool Exists(string fileName);

        /// <summary>Throws if the file is missing or cannot be parsed.</summary>
        GameData Load(string fileName);

        /// <summary>Reads the backup written by the previous <see cref="Save"/>.</summary>
        bool TryLoadBackup(string fileName, out GameData data);

        /// <summary>
        /// Renames an unreadable save aside instead of deleting it, so a player's progress can
        /// still be recovered by hand. Never destroys data.
        /// </summary>
        void QuarantineCorrupt(string fileName);

        void Delete(string fileName);
    }
}
