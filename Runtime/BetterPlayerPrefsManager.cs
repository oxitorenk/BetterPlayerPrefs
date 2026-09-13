using System;
using UnityEngine;

namespace BetterPlayerPrefs.Runtime
{
    public interface IPersistent
    {
        public string SaveKey { get; }
        public int SaveVersion { get; }
    }

    public static class BetterPlayerPrefsManager
    {
        [Serializable]
        private class SaveRecord<T>
        {
            public int version;
            public bool hasData;
            public T data;
        }
        
        public static void Save<T>(this IPersistent saveObject, T data, bool writeToDisk)
        {
            var key = GetSaveKey(saveObject);
            if (data is null)
            {
                var exceptionMessage = $"[BetterPlayerPrefs]: Cannot save null data for key '{key}'.";
                throw new ArgumentNullException(nameof(data), exceptionMessage);
            }

            var version = saveObject.SaveVersion;
            if (version < 1)
            {
                var exceptionMessage = $"[BetterPlayerPrefs]: Save version must be at least 1 for key '{key}'.";
                throw new ArgumentOutOfRangeException(nameof(IPersistent.SaveVersion), version, exceptionMessage);
            }

            var saveRecord = new SaveRecord<T>
            {
                version = version,
                hasData = true,
                data = data
            };
            var json = JsonUtility.ToJson(saveRecord);
            
            PlayerPrefs.SetString(key, json);
            if (!writeToDisk) return;
            
            WriteToDisk();
        }

        public static bool TryLoad<T>(this IPersistent target, out T loadedData, out int savedVersion)
        {
            loadedData = default;
            savedVersion = -1;

            var key = GetSaveKey(target);
            if (!PlayerPrefs.HasKey(key)) return false;

            var json = PlayerPrefs.GetString(key);
            
            SaveRecord<T> saveRecord;
            try
            {
                saveRecord = JsonUtility.FromJson<SaveRecord<T>>(json);
            }
            catch (ArgumentException exception)
            {
                var wrapperMessage = $"[BetterPlayerPrefs]: Failed to deserialize record '{key}'";
                throw new InvalidOperationException(wrapperMessage, exception);
            }
            
            if (saveRecord is null)
            {
                throw new InvalidOperationException($"[BetterPlayerPrefs]: Record '{key}' deserialized to null.");
            }

            if (saveRecord.version < 1)
            {
                throw new InvalidOperationException($"[BetterPlayerPrefs]: Version missing or invalid for key '{key}'.");
            }

            if (!saveRecord.hasData)
            {
                throw new InvalidOperationException($"[BetterPlayerPrefs]: Data field missing for key '{key}'.");
            }
            
            loadedData = saveRecord.data;
            savedVersion = saveRecord.version;
            return true;
        }

        public static void Delete(this IPersistent target, bool writeToDisk)
        {
            var key = GetSaveKey(target);
            PlayerPrefs.DeleteKey(key);
            
            if (!writeToDisk) return;
            
            WriteToDisk();
        }

        public static void DeleteAll()
        {
            PlayerPrefs.DeleteAll();
            WriteToDisk();
        }
        
        public static void WriteToDisk()
        {
            PlayerPrefs.Save();
        }

        private static string GetSaveKey(IPersistent target)
        {
            if (target is null)
            {
                throw new ArgumentNullException(nameof(target), "[BetterPlayerPrefs]: Persistence target cannot be null.");
            }

            var key = target.SaveKey;
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("[BetterPlayerPrefs]: Save key cannot be empty.", nameof(target));
            }

            return key;
        }
    }
}
