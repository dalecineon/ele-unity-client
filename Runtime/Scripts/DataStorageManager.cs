using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.IO;
using System;
using Cineon.ELE.Networking;

namespace Cineon.ELE.DownloadHelper
{
    public static class DataStorageManager
    {
        public static float jsonFileNum = 0;
        /// <summary>
        /// This is a helper function to save an object to a json file on either desktop or android.
        /// We are also converting the json parameter names to snakecase for the ELE API.
        /// </summary>
        /// <param name="path">This would be either dataPath, PersistentDataPath or StreamingAssets.</param>
        /// <param name="folderName">A custom name for the folder you wish the data to be stored in.</param>
        /// <param name="fileName">A custom filename for the data file.</param>
        /// <param name="timeStampedFileName">If you want the filename to be timestamped.</param>
        public static void SaveToJson<T>(T data, string path, string fileName, string folderName = "CineonELE", bool timeStampedFileName = false)
        {
            if (string.IsNullOrEmpty(path))
            {
                path = Application.persistentDataPath;
            }
            if (string.IsNullOrWhiteSpace(folderName))
            {
                folderName = "CineonELE";
            }
            string targetFolder = Path.Combine(path, folderName);
            try
            {
                if (!Directory.Exists(targetFolder))
                {
                    Directory.CreateDirectory(targetFolder);
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is NotSupportedException)
            {
                // Some locations (for example StreamingAssets on device builds) are read-only.
                string fallbackPath = Application.persistentDataPath;
                targetFolder = Path.Combine(fallbackPath, folderName);
                Debug.LogWarning($"Failed to create directory at '{path}'. Falling back to persistentDataPath: {targetFolder}. Error: {ex.Message}");
                if (!Directory.Exists(targetFolder))
                {
                    Directory.CreateDirectory(targetFolder);
                }
            }
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                },
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented,
                Converters = new List<JsonConverter>
                    {
                        new LowercaseEnumConverter()
                    }
            };
            string json = JsonConvert.SerializeObject(data, settings);
            string baseFileName = !string.IsNullOrEmpty(fileName) ? fileName : "data";
            if (timeStampedFileName)
            {
                baseFileName = $"{baseFileName}_{DateTime.Now:yyyyMMdd_HHmmssfff}";
            }
            string _fileName = $"{baseFileName}.json";
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
            try
            {
                File.WriteAllText(fullFilePath, updatedJson);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                string fallbackFolder = Path.Combine(Application.persistentDataPath, folderName);
                if (!Directory.Exists(fallbackFolder))
                {
                    Directory.CreateDirectory(fallbackFolder);
                }
                string fallbackFilePath = Path.Combine(fallbackFolder, _fileName);
                Debug.LogWarning($"Failed to write JSON to '{fullFilePath}'. Falling back to '{fallbackFilePath}'. Error: {ex.Message}");
                File.WriteAllText(fallbackFilePath, updatedJson);
            }
        }
    }
}