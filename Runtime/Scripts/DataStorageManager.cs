using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.IO;
using System;

namespace Cineon.ELE.DownloadHelper
{
    public static class DataStorageManager
    {
        public static float jsonFileNum = 0;
        /// <summary>
        /// This is a helper function to save an object to a json file on either desktop or android.
        /// We are also converting the json parameter names to snakecase for the ELE API.
        /// </summary>
        /// <param name="location">This would be either dataPath, PersistentDataPath or StreamingAssets.</param>
        /// <param name="folderName">A custom name for the folder you wish the data to be stored in.</param>
        /// <param name="fileName">A custom filename for the data file.</param>
        /// <param name="timeStampedFileName">If you want the filename to be timestamped.</param>
        public static void SaveToJson<T>(T data, string path, string fileName, string folderName = null, bool timeStampedFileName = false)
        {
            if (string.IsNullOrEmpty(path))
            {
                path = Application.persistentDataPath;
            }
            string targetFolder;
            if (string.IsNullOrEmpty(folderName))
            {
                targetFolder = path;
            }
            else
            {
                targetFolder = Path.Combine(path, folderName);
            }
            if (!Directory.Exists(targetFolder))
            {
                Directory.CreateDirectory(targetFolder);
            }
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                },
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented
            };
            string json = JsonConvert.SerializeObject(data, settings);
            string _fileName = !string.IsNullOrEmpty(fileName) ? $"{fileName}.json" : "data.json";
            string fullFilePath = Path.Combine(targetFolder, _fileName);
            List<T> jsonDataList = new List<T>();
            if (File.Exists(fullFilePath))
            {
                try
                {
                    string existingJson = File.ReadAllText(fullFilePath);
                    if (!string.IsNullOrWhiteSpace(existingJson))
                    {
                        jsonDataList = JsonConvert.DeserializeObject<List<T>>(existingJson) ?? new List<T>();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to read or deserialize existing JSON: {ex.Message}");
                }
            }
            jsonDataList.Add(data);
            string updatedJson = JsonConvert.SerializeObject(jsonDataList, settings);
            File.WriteAllText(fullFilePath, updatedJson);
        }
    }
}