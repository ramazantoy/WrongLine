using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using AF.Core;
using UnityEngine;

namespace AF.Platform.Epic
{
    /// <summary>
    /// Achievement unlocks waiting for Epic's confirmation. The EOS SDK keeps nothing while offline or logged out, so every
    /// unlock goes into this file first and leaves it only when Epic confirms it. One file per deployment and Epic account
    /// (<c>&lt;deployment&gt;_&lt;account&gt;.json</c>), outside the saves folder: a cloud-synced queue would send the same
    /// unlock from two PCs. Without a deployment or account id (not started by the launcher) the queue is inactive.
    /// </summary>
    internal sealed class EpicUnlockQueue
    {
        private const string FILE_EXTENSION = ".json";
        private const string TEMP_SUFFIX = ".tmp";
        private const int FileVersion = 1;

        private readonly string _filePath;
        private readonly QueueFile _data;

        public EpicUnlockQueue(string directory, string deploymentId, string accountId)
        {
            _data = new QueueFile { version = FileVersion, deploymentId = FileSafe(deploymentId), accountId = FileSafe(accountId) };
            if (string.IsNullOrEmpty(directory) || _data.deploymentId.Length == 0 || _data.accountId.Length == 0)
                return;
            _filePath = Path.Combine(directory, _data.deploymentId + "_" + _data.accountId + FILE_EXTENSION);
            Load();
        }

        public bool IsActive => _filePath != null;

        /// <summary>The Epic account the queue belongs to; unlocks are sent only while that account is logged in.</summary>
        public string AccountId => _data.accountId;

        public IReadOnlyList<string> Pending => _data.unlocks;

        public void Add(string id)
        {
            if (!IsActive || string.IsNullOrEmpty(id) || _data.unlocks.Contains(id))
                return;
            _data.unlocks.Add(id);
            Save();
        }

        public void Remove(string id)
        {
            if (_data.unlocks.Remove(id))
                Save();
        }

        private void Load()
        {
            if (!File.Exists(_filePath))
                return;
            try
            {
                var loaded = JsonUtility.FromJson<QueueFile>(File.ReadAllText(_filePath));
                if (loaded?.unlocks == null)
                    return;
                foreach (var id in loaded.unlocks)
                {
                    if (!string.IsNullOrEmpty(id) && !_data.unlocks.Contains(id))
                        _data.unlocks.Add(id);
                }
            }
            catch (Exception exception)
            {
                // Kept as it is: the next successful write replaces it, and the game unlocks again after loading.
                Log.Error($"Epic: the achievement queue {_filePath} could not be read ({exception.Message}).");
            }
        }

        private void Save()
        {
            try
            {
                if (_data.unlocks.Count == 0)
                {
                    // Everything confirmed: the queue's own file, nothing of the player's.
                    if (File.Exists(_filePath))
                        File.Delete(_filePath);
                    return;
                }
                Directory.CreateDirectory(Path.GetDirectoryName(_filePath));
                var temp = _filePath + TEMP_SUFFIX;
                File.WriteAllText(temp, JsonUtility.ToJson(_data));
                if (File.Exists(_filePath))
                    File.Replace(temp, _filePath, null, ignoreMetadataErrors: true);
                else
                    File.Move(temp, _filePath);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Log.Error($"Epic: the achievement queue {_filePath} could not be written ({exception.Message}).");
            }
        }

        private static string FileSafe(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            var safe = new StringBuilder(value.Length);
            foreach (var character in value)
            {
                if ((character >= 'a' && character <= 'z') || (character >= 'A' && character <= 'Z') || (character >= '0' && character <= '9'))
                    safe.Append(character);
            }
            return safe.ToString();
        }

        [Serializable]
        private sealed class QueueFile
        {
            public int version;
            public string deploymentId;
            public string accountId;
            public List<string> unlocks = new List<string>();
        }
    }
}
