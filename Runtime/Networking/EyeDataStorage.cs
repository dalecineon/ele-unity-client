using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Cineon.ELE.Networking;
using static Cineon.ELE.Storage.EyeDataStorage;
using Cineon.ELE.DownloadHelper;

namespace Cineon.ELE.Storage
{
    /// <summary>
    /// This script will store all the eye data and also the response from the server.
    /// It gives an understand of how the data needs to be structured for the CineonRestAPI.
    /// We use Newtonsoft to give custom Json Property names as the CineonRestAPI requires specific naming conventions which are snake case.
    /// For the time being Device needs to be added in the inspector manually.
    /// </summary>
    public class EyeDataStorage : MonoBehaviour
    {
        public static EyeDataStorage Instance { get; private set; }

        public enum ConstructType
        {
            stress,
            workload,
            fatigue
        }

        public enum MetricsType
        {
            mean_fixation_duration,
            mean_fixation_rate,
            median_fixation_dispersion,
            mean_total_fixation_displacement,
            mean_saccade_velocity,
            mean_saccade_rate,
            peak_saccade_velocity,
            max_saccade_amplitude,
            persistent_saccade_ratio,
            gaze_efficiency_entropy,
            mean_head_speed,
            peak_head_speed,
            mean_head_acceleration,
            mean_pupil_diameter,
            variance_pupil_diameter,
            mean_blink_rate,
            mean_blink_duration,
            percent_eyes_closed
        }

        //private enum ModelsType { } //This is to be added later.


        #region Eye Data Collection
        [Serializable]
        public class ClientInfo
        {
            [JsonProperty("platform")]
            public string platform;
            [JsonProperty("version")]
            public string version;
        }

        [Serializable]
        public class EyeDataCollection
        {
            [JsonProperty("timestamp")]
            public string timestamp;
            [JsonProperty("left_eye")]
            public Eye leftEye = new Eye();
            [JsonProperty("right_eye")]
            public Eye rightEye = new Eye();
        }

        [Serializable]
        public class Eye
        {
            [SerializeField]
            [JsonProperty("gaze_validity")]
            private bool isGazeValid;
            [SerializeField]
            [JsonProperty("gaze_origin")]
            private GazePosition gazeOrigin = new GazePosition();
            [SerializeField]
            [JsonProperty("gaze_direction")]
            private GazePosition gazeDirection = new GazePosition();
            [SerializeField]
            [JsonProperty("object_gazed_at")]
            private string objectGazedAt = null;
            [SerializeField]
            [JsonProperty("pupil_diameter")]
            private float? pupilDiameter;
            [SerializeField]
            [JsonProperty("eye_openness")]
            private float? eyeOpenness;
            [JsonIgnore]
            public bool IsGazeValid { get => isGazeValid; set => isGazeValid = value; }
            [JsonIgnore]
            public GazePosition GazeOrigin { get => gazeOrigin; set => gazeOrigin = value; }
            [JsonIgnore]
            public GazePosition GazeForward { get => gazeDirection; set => gazeDirection = value; }
            [JsonIgnore]
            public string ObjectGazedAt { get => objectGazedAt; set => objectGazedAt = value; }
            [JsonIgnore]
            public float? PupilDiameter { get => pupilDiameter; set => pupilDiameter = value; }
            [JsonIgnore]
            public float? EyeOpenness { get => eyeOpenness; set => eyeOpenness = value; }
        }

        [Serializable]
        public class GazePosition
        {
            [SerializeField]
            private float x;
            [SerializeField]
            private float y;
            [SerializeField]
            private float z;

            public float X => x;
            public float Y => y;
            public float Z => z;

            /// <summary>
            /// This converts the position of the vector3 to individual x,y,z.
            /// </summary>
            /// <param name="position"></param>
            public void SetPosition(Vector3 position)
            {
                x = position.x;
                y = position.y;
                z = position.z;
            }

            /// <summary>
            /// This converts the x,y,z back to a vector3 if we ever need it.
            /// </summary>
            /// <returns></returns>
            public Vector3 ToVector3()
            {
                return new Vector3(x, y, z);
            }

            /// <summary>
            /// This converts the values to a readable string to be displayed in UI.
            /// </summary>
            /// <returns>Vector3</returns>
            public string ToCustomString()
            {
                return $"x:{x.ToString("F4")},y:{y.ToString("F4")},z:{z.ToString("F4")}";
            }
        }
        [Serializable]
        public class GazeRotation
        {
            [SerializeField]
            private float x;
            [SerializeField]
            private float y;
            [SerializeField]
            private float z;
            [SerializeField]
            private float w;

            public float X => x;
            public float Y => y;
            public float Z => z;
            public float W => w;

            /// <summary>
            /// This converts the position of the vector3 to individual x,y,z.
            /// </summary>
            /// <param name="Quaternion">gaze rotation</param>
            public void SetRotation(Quaternion rotation)
            {
                x = rotation.x;
                y = rotation.y;
                z = rotation.z;
                w = rotation.w;
            }

            /// <summary>
            /// This converts the x,y,z back to a vector3 if we ever need it.
            /// </summary>
            /// <returns></returns>
            public Quaternion ToQuaternion()
            {
                return new Quaternion(x, y, z, w);
            }

            /// <summary>
            /// This converts the values to a readable string to be displayed in UI.
            /// </summary>
            /// <returns>Quaternion</returns>
            public string ToCustomString()
            {
                return $"x:{x.ToString("F4")},y:{y.ToString("F4")},z:{z.ToString("F4")},w:{w.ToString("F4")}";
            }

        }
        [Serializable]
        public class PupilPosition2D
        {
            [SerializeField]
            private float x;
            [SerializeField]
            private float y;
            public float X => x;
            public float Y => y;

            /// <summary>
            /// This converts the position of the vector3 to individual x,y,z.
            /// </summary>
            /// <param name="position">This is the 2D position of the pupil.</param>
            public void SetPosition(Vector2 position)
            {
                x = position.x;
                y = position.y;
            }

            /// <summary>
            /// This converts the x,y,z back to a vector3 if we ever need it.
            /// </summary>
            /// <returns></returns>
            public Vector2 ToVector2()
            {
                return new Vector2(x, y);
            }

            /// <summary>
            /// This converts the values to a readable string to be displayed in UI.
            /// </summary>
            /// <returns>vector2</returns>
            public string ToCustomString()
            {
                return $"x:{x.ToString("F4")},y:{y.ToString("F4")}";
            }
        }

        /// <summary>
        /// This is a wrapper to store the json Data
        /// </summary>
        [Serializable]
        public class EyeDataCollectionWrapper
        {
            [JsonProperty("constructs")]
            public List<ConstructType> constructs = new List<ConstructType>();
            [JsonProperty("metrics")]
            public List<MetricsType> metrics = new List<MetricsType>();
            //This is a feature that will be added at a later date.
            //[JsonProperty("models")]
            //public List<string> models = new List<string>();
            [JsonProperty("device")]
            public string device;
            [JsonProperty("client")]
            public ClientInfo clientInfo = new ClientInfo();
            [JsonProperty("eye_data")]
            public List<EyeDataCollection> eyeData = new List<EyeDataCollection>();
            //Create Temporary data for sending to the server.
            [JsonIgnore]
            public List<EyeDataCollection> temporaryEyeData = new List<EyeDataCollection>();
        }

        //The holding variable for the data set.
        public EyeDataCollectionWrapper eyeDataCollectionWrapper;
        #endregion

        #region ELE Response
        [Serializable]
        public class ResponseData
        {
            public string prediction;
            public double score;
        }

        [Serializable]
        public class ResponseContainer
        {
            public List<ResponseData> data;
        }
        //This is the response collection list and gets populate every time we get a response back from the server.
        //public List<ResponseContainer> responseCollection;
        /// <summary>
        /// We create a response set to deal with mulitple stop and start data collections.
        /// </summary>
        [Serializable]
        public class ResponseSet
        {
            public int setNumber;
            public List<ResponseContainer> responseCollection;
        }

        public List<ResponseSet> responseSets = new List<ResponseSet>();

        public ResponseSet currentResponseSet;

        #endregion

        [Header("User Defined Settings")]
        public bool debugMode = false;
        [Tooltip("The file will be saved to the StreamingAssets folder on the device.")]
        public bool saveDataToFile = false;

        /// <summary>
        /// In the awake we setup the instance for the script
        /// Also we clear any data that may of been accidentally added in the inspector.
        /// </summary>
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(Instance);
            }
            ClearAllData();
            responseSets.Clear();
            eyeDataCollectionWrapper.clientInfo.platform = Application.platform.ToString();
            eyeDataCollectionWrapper.clientInfo.version = CineonRestClient.version;
        }

        public void CreateResponseSet()
        {
            ResponseSet newSet = new ResponseSet
            {
                setNumber = responseSets.Count + 1,
                responseCollection = new List<ResponseContainer>()
            };
            responseSets.Add(newSet);
            currentResponseSet = newSet;
        }

        public void AddResponseToCurrentSet(ResponseContainer response)
        {
            if (currentResponseSet == null)
            {
                CreateResponseSet();
            }
            currentResponseSet.responseCollection.Add(response);

            if (saveDataToFile)
            {
                DataStorageManager.SaveToJson<EyeDataCollectionWrapper>(eyeDataCollectionWrapper, Application.streamingAssetsPath, "EyeDataCollection");
            }
        }

        /// <summary>
        /// This clears all data in the storage.
        /// </summary>
        public void ClearAllData()
        {
            eyeDataCollectionWrapper.eyeData.Clear();
            eyeDataCollectionWrapper.temporaryEyeData.Clear();
            //responseCollection.Clear();
        }

        /// <summary>
        /// This clears a specific response set or all response sets.
        /// </summary>
        /// <param name="index">index of set to clear.</param>
        /// <param name="clearAll">if set to true this will clear all.</param>
        public void ClearResponseSet(int index, bool clearAll = false)
        {
            if (clearAll)
            {
                responseSets.Clear();
            }
            else
            {
                responseSets.RemoveAt(index);
            }
        }

        /// <summary>
        /// This clears the eye data collection.
        /// </summary>
        public void ClearEyeData()
        {
            eyeDataCollectionWrapper.eyeData.Clear();
            eyeDataCollectionWrapper.temporaryEyeData.Clear();
            Debug.Log("Eye data cleared.");
        }
        /// <summary>
        /// We can easily Update data that we receive from different headsets.
        /// </summary>
        public void UpdateEyeData(EyeDataCollection eyeData)
        {
            if (debugMode)
            {
                Debug.Log($"Eye Data Added at time : {eyeData.timestamp}");
                Debug.Log($"Eye Data Left Valid : {eyeData.leftEye.IsGazeValid}");
                Debug.Log($"Eye Data Right Valid : {eyeData.rightEye.IsGazeValid}");
                Debug.Log($"Left Eye Gaze Origin : {eyeData.leftEye.GazeOrigin.ToCustomString()}");
                Debug.Log($"Left Eye Gaze Direction : {eyeData.leftEye.GazeForward.ToCustomString()}");
                Debug.Log($"Right Eye Gaze Origin : {eyeData.rightEye.GazeOrigin.ToCustomString()}");
                Debug.Log($"Right Eye Gaze Direction : {eyeData.rightEye.GazeForward.ToCustomString()}");
                Debug.Log($"Left Gaze Transform : {eyeData.leftEye.GazeOrigin.ToCustomString()}");
                Debug.Log($"Right Gaze Transform : {eyeData.rightEye.GazeOrigin.ToCustomString()}");
                Debug.Log($"Left Eye Object Gazed At: {eyeData.leftEye.ObjectGazedAt}");
                Debug.Log($"Left Eye Pupil Diameter: {eyeData.leftEye.PupilDiameter}");
                Debug.Log($"Left Eye Openness: {eyeData.leftEye.EyeOpenness}");
                Debug.Log($"Right Eye Object Gazed At: {eyeData.rightEye.ObjectGazedAt}");
                Debug.Log($"Right Eye Pupil Diameter: {eyeData.rightEye.PupilDiameter}");
                Debug.Log($"Right Eye Openness: {eyeData.rightEye.EyeOpenness}");
            }
            eyeDataCollectionWrapper.temporaryEyeData.Add(eyeData);
        }


        // /// <summary>
        // /// This returns the response collection list. 
        // /// We store responses we get back from the server after a POST.
        // /// </summary>
        // public List<ResponseContainer> GetResponseCollection
        // {
        //     get { return responseCollection; }
        // }

        /// <summary>
        /// This will get the newest response list from the response collection.
        /// </summary>
        /// <returns></returns>
        public ResponseSet GetNewestResponseSet()
        {
            // ResponseSet currentResponseSet = responseSets[responseSets.Count - 1];
            // if (debugMode)
            // {
            //     Debug.Log(currentResponseSet.responseCollection[currentResponseSet.responseCollection.Count - 1]);
            //     foreach (ResponseData data in currentResponseSet.responseCollection[currentResponseSet.responseCollection.Count - 1].data)
            //     {
            //         Debug.Log($"Prediction : {data.prediction} - Score : {data.score}");
            //     }
            // }
            return currentResponseSet;
        }

        // /// <summary>
        // /// This will get the newest response list from the response collection.
        // /// </summary>
        // /// <returns></returns>
        // public List<ResponseContainer> GetNewestResponseCollection()
        // {
        //     ResponseSet currentResponseSet = responseSets[responseSets.Count - 1];
        //     if (debugMode)
        //     {
        //         Debug.Log(currentResponseSet.responseCollection[currentResponseSet.responseCollection.Count - 1]);
        //         foreach (ResponseData data in currentResponseSet.responseCollection[currentResponseSet.responseCollection.Count - 1].data)
        //         {
        //             Debug.Log($"Prediction : {data.prediction} - Score : {data.score}");
        //         }
        //     }
        //     return currentResponseSet.responseCollection;
        // }

        /// <summary>
        /// This will get the newest response collection from a chosen construct type and return the average score from all models.
        /// </summary>
        /// <returns>you will get an average score.</returns>
        public float GetNewestResponseConstructAverage(ConstructType? constructType)
        {
            ResponseSet currentResponseSet = responseSets[responseSets.Count - 1];
            float averageValue = 0f;
            int count = 0;
            string constructValue = constructType.Value.ToString();
            foreach (ResponseContainer data in currentResponseSet.responseCollection)
            {
                foreach (ResponseData responseData in data.data)
                {
                    if (responseData.prediction == constructValue)
                    {
                        if (debugMode)
                        {
                            Debug.Log($"Count is : {count}, Adding Score : {responseData.score} for Prediction : {responseData.prediction}");
                        }
                        count++;
                        averageValue += (System.Convert.ToSingle(responseData.score) - averageValue) / count;
                    }
                }
            }
            return averageValue;
        }

        /// <summary>
        /// This will get the newest response collection from a chosen metric type and return the average score from all models.
        /// </summary>
        /// <returns>you will get an average score.</returns>
        public float GetNewestResponseMetricAverage(MetricsType? metricType = null)
        {
            ResponseSet currentResponseSet = responseSets[responseSets.Count - 1];
            float averageValue = 0f;
            int count = 0;
            string constructValue = metricType.Value.ToString();
            foreach (ResponseContainer data in currentResponseSet.responseCollection)
            {
                foreach (ResponseData responseData in data.data)
                {
                    if (responseData.prediction == constructValue)
                    {
                        if (debugMode)
                        {
                            Debug.Log($"Count is : {count}, Adding Score : {responseData.score} for Prediction : {responseData.prediction}");
                        }
                        count++;
                        averageValue += (System.Convert.ToSingle(responseData.score) - averageValue) / count;
                    }
                }
            }
            return averageValue;
        }

        /// <summary>
        /// This will get a response from the selected response set and search for only a construct type and return the average score from all models for a specific response collection value.
        /// </summary>
        /// <param name="responseCollectionValue"></param>
        /// <param name="constructType"></param>
        /// <returns></returns>
        public float GetSelectedResponseConstructAverage(int responseSetValue, ConstructType? constructType)
        {
            ResponseSet currentResponseSet = responseSets[responseSetValue];
            float averageValue = 0f;
            int count = 0;
            string constructValue = constructType.Value.ToString();
            foreach (ResponseContainer responseData in currentResponseSet.responseCollection)
            {
                foreach (ResponseData data in responseData.data)
                {
                    if (data.prediction == constructValue)
                    {
                        if (debugMode)
                        {
                            Debug.Log($"Count is : {count}, Adding Score : {data.score} for Prediction : {data.prediction}");
                        }
                        count++;
                        averageValue += (System.Convert.ToSingle(data.score) - averageValue) / count;
                    }
                }
            }
            return averageValue;
        }

        /// <summary>
        /// This will get a response from the selected response set and search for only a Metric type and return the average score from all models for a specific response collection value.
        /// </summary>
        /// <param name="responseSetValue"></param>
        /// <param name="metricType"></param>
        /// <returns></returns>
        public float GetSelectedResponseMetricAverage(int responseSetValue, MetricsType? metricType)
        {
            ResponseSet currentResponseSet = responseSets[responseSetValue];
            float averageValue = 0f;
            int count = 0;
            string metricValue = metricType.Value.ToString();
            foreach (ResponseContainer responseData in currentResponseSet.responseCollection)
            {
                foreach (ResponseData data in responseData.data)
                {
                    if (data.prediction == metricValue)
                    {
                        if (debugMode)
                        {
                            Debug.Log($"Count is : {count}, Adding Score : {data.score} for Prediction : {data.prediction}");
                        }
                        count++;
                        averageValue += (System.Convert.ToSingle(data.score) - averageValue) / count;
                    }
                }
            }
            return averageValue;
        }

        /// <summary>
        /// This will get the overall average from all response sets for either a metric type or construct type.
        /// </summary>
        /// <param name="responseSetValue"></param>
        /// <param name="metricType"></param>
        /// <param name="constructType"></param>
        /// <returns>The average score for the specified metric or construct type across all response sets.</returns>
        /// <exception cref="ArgumentException"></exception>
        public float GetOverallAverageFromAllResponseSets(MetricsType? metricType = null, ConstructType? constructType = null)
        {
            if (!metricType.HasValue && !constructType.HasValue)
                throw new ArgumentException("metric type or construct type must be provided.");

            if (metricType.HasValue && constructType.HasValue)
                throw new ArgumentException("Only one of metric or construct type can be provided.");

            string modelName = metricType?.ToString() ?? constructType!.ToString();
            float averageValue = 0f;
            int count = 0;
            foreach (ResponseSet responseSet in responseSets)
            {
                foreach (ResponseContainer responseContainer in responseSet.responseCollection)
                {
                    foreach (ResponseData data in responseContainer.data)
                    {
                        if (data.prediction == modelName)
                        {
                            if (debugMode)
                            {
                                Debug.Log($"Count is : {count}, Adding Score : {data.score} for Prediction : {data.prediction}");
                            }
                            count++;
                            averageValue += (System.Convert.ToSingle(data.score) - averageValue) / count;
                        }
                    }
                }
            }
            return averageValue;
        }

        /// <summary>
        /// This saves the current static Window
        /// </summary>
        public void SetStaticWindow()
        {
            eyeDataCollectionWrapper.eyeData.Clear();
            eyeDataCollectionWrapper.eyeData = new List<EyeDataCollection>(eyeDataCollectionWrapper.temporaryEyeData);
            eyeDataCollectionWrapper.temporaryEyeData.Clear();
            Debug.Log("Cleared Temp Eye Data");
            Debug.Log($"{eyeDataCollectionWrapper.temporaryEyeData.Count}");
            Debug.Log($"{eyeDataCollectionWrapper.eyeData.Count}");
        }

        /// <summary>
        /// This will store the sample window into the eyeData collection list.
        /// </summary>
        public void SetSampleWindow(bool isFirstCollection, float rollingWindowTime)
        {
            //If First collection is true the first 10 seconds worth of data will be stored.
            //If false we need to look at the data to find out the 10 seconds for example 5-15s timestamps.
            if (!isFirstCollection)
            {
                if (eyeDataCollectionWrapper.temporaryEyeData.Count > 0)
                {
                    DateTime startTime;
                    if (DateTime.TryParse(eyeDataCollectionWrapper.temporaryEyeData[0].timestamp, out startTime))
                    {
                        int removeCount = 0;
                        foreach (EyeDataCollection data in eyeDataCollectionWrapper.temporaryEyeData)
                        {
                            DateTime entryTime;
                            if (DateTime.TryParse(data.timestamp, out entryTime))
                            {
                                if ((entryTime - startTime).TotalSeconds < rollingWindowTime)
                                {
                                    removeCount++;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                        if (removeCount > 0)
                        {
                            eyeDataCollectionWrapper.temporaryEyeData.RemoveRange(0, removeCount);
                        }
                    }
                    else
                    {
                        Debug.LogError("Failed to parse timestamp from eye data.");
                    }
                }
            }
            eyeDataCollectionWrapper.eyeData = new List<EyeDataCollection>(eyeDataCollectionWrapper.temporaryEyeData);
        }

        /// <summary>
        /// This sets the rolling window data for the eye data collection.
        /// </summary>
        /// <param name="isFirstCollection">This is if it is the first collection of data</param>
        /// <param name="rollingWindowTime">This is the rolling window time in seconds.</param>
        public void SetRollingWindowData(bool isFirstCollection, float rollingWindowTime)
        {
            if (eyeDataCollectionWrapper.temporaryEyeData.Count > 0)
            {
                if (DateTime.TryParse(eyeDataCollectionWrapper.temporaryEyeData[^1].timestamp, out DateTime latestTime))
                {
                    int removeCount = 0;
                    foreach (EyeDataCollection data in eyeDataCollectionWrapper.temporaryEyeData)
                    {
                        if (DateTime.TryParse(data.timestamp, out DateTime entryTime))
                        {
                            if ((latestTime - entryTime).TotalSeconds > rollingWindowTime)
                            {
                                removeCount++;
                            }
                            else
                            {
                                break;
                            }
                        }
                        else
                        {
                            Debug.LogError("Failed to parse timestamp from eye data.");
                            break;
                        }
                    }
                    if (removeCount > 0)
                    {
                        eyeDataCollectionWrapper.temporaryEyeData.RemoveRange(0, removeCount);
                    }
                }
                else
                {
                    Debug.LogError("Failed to parse timestamp from eye data.");
                }
            }
            eyeDataCollectionWrapper.eyeData = new List<EyeDataCollection>(eyeDataCollectionWrapper.temporaryEyeData);
        }

    }
}