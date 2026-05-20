using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Cineon.ELE.Networking;
using static Cineon.ELE.Storage.EyeDataStorage;
using Cineon.ELE.DownloadHelper;
using System.IO;
using Newtonsoft.Json.Serialization;

namespace Cineon.ELE.Storage
{
    /// <summary>
    /// This script will store all the eye data and also the response from the server.
    /// It gives an understand of how the data needs to be structured for the CineonRestAPI.
    /// We use Newtonsoft to give custom Json Property names as the CineonRestAPI requires specific naming conventions which are snake case.
    /// For the time being Device needs to be added in the inspector manually.
    /// </summary>
    public class EyeDataStorage : ELEMonoBehaviour
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
            fixation_duration_mean,
            fixation_dispersion_median,
            total_fixation_displacement_mean,
            saccade_speed_mean,
            saccade_speed_max,
            saccade_amplitude_max,
            persistent_saccade_ratio,
            saccade_rate,
            saccade_angular_amplitude_mean,
            saccade_angular_amplitude_std,
            saccade_angular_speed_mean,
            saccade_angular_speed_std,
            saccade_antipersistent_to_persistent_ratio,
            saccade_persistent_to_total_ratio,
            entropy_gaze,
            entropy_gaze_efficiency,
            pupil_diameter_mean,
            pupil_diameter_std,
            blink_rate,
            blink_duration_mean,
            blink_duration_std,
            percent_eyes_closed,
            head_acceleration_mean,
            head_acceleration_std,
            fixation_rate,
            fixation_duration_std,
            fixation_angular_distance_mean,
            fixation_angular_distance_std,
            fixation_yaw_dispersion_mean,
            fixation_yaw_dispersion_std,
            fixation_pitch_dispersion_mean,
            fixation_pitch_dispersion_std
        }

        public enum CustomModels
        {
            stress_heuristic,
            StressCrossFormer,
            stress_random,
            stress_heuristic_original,
            workload_heuristic,
            WorkloadCrossFormer,
            workload_random,
            fatigue_heuristic,
            FatigueCrossFormer,
            fatigue_random,
            fatigue_heuristic_original
        }

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
            public List<string> timestamp = new List<string>();
            [JsonProperty("eye")]
            public Eye eye = new Eye();
            [JsonProperty("head")]
            public Head head = new Head();

            /// <summary>
            /// Appends all list data from another EyeDataCollection into this one.
            /// </summary>
            public void AppendFrom(EyeDataCollection other)
            {
                timestamp.AddRange(other.timestamp);
                eye.GazeDirection.AddRange(other.eye.GazeDirection);
                eye.GazeObject.AddRange(other.eye.GazeObject);
                eye.PupilDiameter.AddRange(other.eye.PupilDiameter);
                eye.Openness.AddRange(other.eye.Openness);
                head.Direction.AddRange(other.head.Direction);
                head.Position.AddRange(other.head.Position);
            }

            /// <summary>
            /// Removes a range of entries from all lists at the given index.
            /// </summary>
            public void RemoveRange(int index, int count)
            {
                timestamp.RemoveRange(index, count);
                eye.GazeDirection.RemoveRange(index, count);
                eye.GazeObject.RemoveRange(index, count);
                eye.PupilDiameter.RemoveRange(index, count);
                eye.Openness.RemoveRange(index, count);
                head.Direction.RemoveRange(index, count);
                head.Position.RemoveRange(index, count);
            }

            /// <summary>
            /// Returns the number of data entries (based on timestamp count).
            /// </summary>
            [JsonIgnore]
            public int DataCount => timestamp.Count;
        }

        [Serializable]
        public class Eye
        {
            [SerializeField]
            [JsonProperty("gaze_direction")]
            private GazeVectorList gazeDirection = new GazeVectorList();
            //[JsonIgnore]
            //[JsonProperty("gaze_depth")]
            //private List<float> gazeDepth = new List<float>();
            [JsonIgnore]
            [JsonProperty("gaze_object")]
            private List<string> gazeObject = new List<string>();
            [JsonIgnore]
            [JsonProperty("pupil_diameter")]
            public List<float> pupilDiameter = new List<float>();
            [JsonIgnore]
            [JsonProperty("openness")]
            public List<float> openness = new List<float>();

            [JsonIgnore]
            public GazeVectorList GazeDirection { get => gazeDirection; set => gazeDirection = value; }
            //[JsonIgnore]
            //public List<float> GazeDepth { get => gazeDepth; set => gazeDepth = value; }
            [JsonIgnore]
            public List<string> GazeObject { get => gazeObject; set => gazeObject = value; }
            [JsonIgnore]
            public List<float> PupilDiameter { get => pupilDiameter; set => pupilDiameter = value; }
            [JsonIgnore]
            public List<float> Openness { get => openness; set => openness = value; }
        }

        [Serializable]
        public class Head
        {
            [SerializeField]
            [JsonProperty("direction")]
            private GazeVectorList direction = new GazeVectorList();
            [SerializeField]
            [JsonProperty("position")]
            private GazeVectorList position = new GazeVectorList();
            [SerializeField]
            [JsonProperty("acceleration")]
            [JsonIgnore]
            private GazeVectorList acceleration = new GazeVectorList();
            [JsonIgnore]
            public GazeVectorList Direction { get => direction; set => direction = value; }
            [JsonIgnore]
            public GazeVectorList Position { get => position; set => position = value; }
            [JsonIgnore]
            public GazeVectorList Acceleration { get => acceleration; set => acceleration = value; }
        }

        [Serializable]
        public class GazeVectorList
        {
            [JsonProperty("x")]
            public List<float> x = new List<float>();
            [JsonProperty("y")]
            public List<float> y = new List<float>();
            [JsonProperty("z")]
            public List<float> z = new List<float>();

            public void Add(Vector3 position)
            {
                x.Add(position.x);
                y.Add(position.y);
                z.Add(position.z);
            }

            public void AddRange(GazeVectorList other)
            {
                x.AddRange(other.x);
                y.AddRange(other.y);
                z.AddRange(other.z);
            }

            public void RemoveRange(int index, int count)
            {
                x.RemoveRange(index, count);
                y.RemoveRange(index, count);
                z.RemoveRange(index, count);
            }

            public void Clear()
            {
                x.Clear();
                y.Clear();
                z.Clear();
            }
            [JsonIgnore]
            public int Count => x.Count;
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
            [JsonProperty("models")]
            public List<CustomModels> models = new List<CustomModels>();
            [JsonProperty("device")]
            public string device;
            [JsonProperty("client")]
            public ClientInfo clientInfo = new ClientInfo();

            [JsonProperty("eye_data")]
            public EyeDataCollection eyeData = new EyeDataCollection();
            // [JsonProperty("head")]
            // public List<EyeDataCollection> headData = new List<EyeDataCollection>();
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

        [Serializable]
        public class ResponseConstructsAverages
        {
            public ConstructType construct;
            public float averageScore;
        }

        [Serializable]
        public class ResponseMetricsAverages
        {
            public MetricsType metric;
            public float averageScore;
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
            public List<ResponseConstructsAverages> responseConstructsAverages;
            public List<ResponseMetricsAverages> responseMetricsAverages;

            public enum Aggregates
            {
                Min,
                Max,
                Average
            }

            /// <summary>
            /// This is a helper function so you can easily get min, max and average data.
            /// </summary>
            /// <param name="aggregates"></param>
            /// <param name="predictionName">Optional filter to only aggregate scores for a specific prediction (e.g. "stress", "workload").</param>
            /// <param name="decimalPlaces">This is the number of decimal places you want (0-4Max).</param>
            public float GetAggregates(Aggregates aggregates, string predictionName = null, int decimalPlaces = 2)
            {
                decimalPlaces = Mathf.Clamp(decimalPlaces, 0, 4);
                if (responseCollection == null || responseCollection.Count == 0) return 0f;

                if (aggregates == Aggregates.Average)
                {
                    float sum = 0f;
                    int count = 0;
                    foreach (ResponseContainer container in responseCollection)
                    {
                        foreach (ResponseData data in container.data)
                        {
                            if (predictionName != null && data.prediction != predictionName) continue;
                            sum += (float)data.score;
                            count++;
                        }
                    }
                    if (count == 0) return 0f;
                    return (float)System.Math.Round(sum / count, decimalPlaces, MidpointRounding.AwayFromZero);
                }
                else
                {
                    float aggregatesValue = aggregates == Aggregates.Min ? float.MaxValue : float.MinValue;
                    bool found = false;
                    foreach (ResponseContainer container in responseCollection)
                    {
                        foreach (ResponseData data in container.data)
                        {
                            if (predictionName != null && data.prediction != predictionName) continue;
                            found = true;
                            if ((aggregates == Aggregates.Min && data.score < aggregatesValue) || (aggregates == Aggregates.Max && data.score > aggregatesValue))
                            {
                                aggregatesValue = (float)data.score;
                            }
                        }
                    }
                    if (!found) return 0f;
                    return (float)System.Math.Round(aggregatesValue, decimalPlaces, MidpointRounding.AwayFromZero);
                }
            }

        }

        public List<ResponseSet> responseSets = new List<ResponseSet>();

        public ResponseSet currentResponseSet;
        /// <summary>
        /// This action is used if you want scripts to subscribe to when we get new data from the server and update accordingly.
        /// </summary>
        public static Action OnDataUpdated;
        #endregion

        [Header("User Defined Settings")]
        public bool debugMode = false;
        public static string gazeDirectionDebug;
        [Tooltip("The file will be saved to the StreamingAssets folder if using in editor, otherwise it will be saved to the persistent data path on the device.")]
        public bool saveRawEyeData = false;

        /// <summary>
        /// This is a helper function to get the save path for the eye data json file depending on the platform. On Android it will be saved to persistent data path and on other platforms it will be saved to streaming assets path.
        /// </summary> 
        /// <returns>The file path where eye data should be saved.</returns>
        private static string GetEyeDataSavePath()
        {
            return Application.platform == RuntimePlatform.Android ? Application.persistentDataPath : Application.streamingAssetsPath;
        }

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

            if (saveRawEyeData)
            {
                DataStorageManager.SaveToJson<EyeDataCollectionWrapper>(eyeDataCollectionWrapper, GetEyeDataSavePath(), "EyeDataCollection");
            }
        }

        /// <summary>
        /// This clears all data in the storage.
        /// </summary>
        public void ClearAllData()
        {
            eyeDataCollectionWrapper.eyeData = new EyeDataCollection();
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
            eyeDataCollectionWrapper.eyeData = new EyeDataCollection();
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
                Debug.Log($"Gaze Direction : {eyeData.eye.GazeDirection}");
                if (eyeData.eye.GazeDirection.x.Count > 0)
                {
                    int lastDataEntry = eyeData.eye.GazeDirection.x.Count - 1;
                    gazeDirectionDebug = $"{eyeData.eye.GazeDirection.x[lastDataEntry]}, {eyeData.eye.GazeDirection.y[lastDataEntry]}, {eyeData.eye.GazeDirection.z[lastDataEntry]}";
                }
                else
                {
                    gazeDirectionDebug = "No gaze data";
                }
                //Debug.Log($"Gaze Depth : {eyeData.eye.GazeDepth}");
                Debug.Log($"Gaze Object : {eyeData.eye.GazeObject}");
                Debug.Log($"Pupil Diameter : {eyeData.eye.PupilDiameter}");
                Debug.Log($"Openness : {eyeData.eye.Openness}");
                Debug.Log($"Head Direction : {eyeData.head.Direction}");
                Debug.Log($"Head Position : {eyeData.head.Position}");
            }

            // Ensure there is exactly one EyeDataCollection instance, then append into it
            if (eyeDataCollectionWrapper.temporaryEyeData.Count == 0)
            {
                eyeDataCollectionWrapper.temporaryEyeData.Add(new EyeDataCollection());
            }
            eyeDataCollectionWrapper.temporaryEyeData[0].AppendFrom(eyeData);

            // If the first two timestamps are more than 0.5s apart, discard them both
            EyeDataCollection temp = eyeDataCollectionWrapper.temporaryEyeData[0];
            if (temp.DataCount == 2
                && DateTime.TryParse(temp.timestamp[0], out DateTime t0)
                && DateTime.TryParse(temp.timestamp[1], out DateTime t1)
                && (t1 - t0).TotalSeconds > 0.5)
            {
                temp.RemoveRange(0, 2);
            }
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

        public float GetLatestPredictionScore(string predictionName)
        {
            ResponseSet currentResponseSet = responseSets[responseSets.Count - 1];
            foreach (ResponseContainer container in currentResponseSet.responseCollection)
            {
                foreach (ResponseData data in container.data)
                {
                    if (data.prediction == predictionName)
                    {
                        return (float)data.score;
                    }
                }
            }
            Debug.LogWarning($"Prediction {predictionName} not found in the latest response set.");
            return 0f; // or throw an exception, or return a nullable float
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
            eyeDataCollectionWrapper.eyeData = new EyeDataCollection();
            if (eyeDataCollectionWrapper.temporaryEyeData.Count > 0)
            {
                eyeDataCollectionWrapper.eyeData.AppendFrom(eyeDataCollectionWrapper.temporaryEyeData[0]);
            }
            eyeDataCollectionWrapper.temporaryEyeData.Clear();
            Debug.Log("Cleared Temp Eye Data");
            Debug.Log($"Temp count: {eyeDataCollectionWrapper.temporaryEyeData.Count}");
            Debug.Log($"Eye data entries: {eyeDataCollectionWrapper.eyeData.DataCount}");
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
                    EyeDataCollection tempData = eyeDataCollectionWrapper.temporaryEyeData[0];
                    if (tempData.DataCount > 0 && DateTime.TryParse(tempData.timestamp[0], out DateTime startTime))
                    {
                        int removeCount = 0;
                        for (int i = 0; i < tempData.DataCount; i++)
                        {
                            if (DateTime.TryParse(tempData.timestamp[i], out DateTime entryTime))
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
                            tempData.RemoveRange(0, removeCount);
                        }
                    }
                    else if (tempData.DataCount > 0)
                    {
                        Debug.LogError("Failed to parse timestamp from eye data.");
                    }
                }
            }
            eyeDataCollectionWrapper.eyeData = new EyeDataCollection();
            if (eyeDataCollectionWrapper.temporaryEyeData.Count > 0)
            {
                eyeDataCollectionWrapper.eyeData.AppendFrom(eyeDataCollectionWrapper.temporaryEyeData[0]);
                if (saveRawEyeData)
                {
                    DataStorageManager.SaveToJson<EyeDataCollectionWrapper>(eyeDataCollectionWrapper, Application.streamingAssetsPath, "EyeDataCollection", null, true);
                }
            }
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
                EyeDataCollection tempData = eyeDataCollectionWrapper.temporaryEyeData[0];
                if (tempData.DataCount > 0 && DateTime.TryParse(tempData.timestamp[^1], out DateTime latestTime))
                {
                    int removeCount = 0;
                    for (int i = 0; i < tempData.DataCount; i++)
                    {
                        if (DateTime.TryParse(tempData.timestamp[i], out DateTime entryTime))
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
                        tempData.RemoveRange(0, removeCount);
                    }
                }
                else if (tempData.DataCount > 0)
                {
                    Debug.LogError("Failed to parse timestamp from eye data.");
                }
            }
            eyeDataCollectionWrapper.eyeData = new EyeDataCollection();
            if (eyeDataCollectionWrapper.temporaryEyeData.Count > 0)
            {
                eyeDataCollectionWrapper.eyeData.AppendFrom(eyeDataCollectionWrapper.temporaryEyeData[0]);
                if (saveRawEyeData)
                {
                    DataStorageManager.SaveToJson<EyeDataCollectionWrapper>(eyeDataCollectionWrapper, Application.streamingAssetsPath, "EyeDataCollection", null, true);
                }
            }
        }

        /// <summary>
        /// This is a function to calculate the averages of the response data as it is collected.
        /// </summary>
        public void GetResponseAverages()
        {
            if (currentResponseSet == null || currentResponseSet.responseCollection == null || currentResponseSet.responseCollection.Count == 0)
            {
                Debug.LogWarning("No responses available to calculate averages.");
                return;
            }

            Dictionary<string, (float totalScore, int count)> scores = new Dictionary<string, (float totalScore, int count)>();

            foreach (ResponseContainer responseCol in currentResponseSet.responseCollection)
            {
                foreach (ResponseData data in responseCol.data)
                {
                    if (scores.ContainsKey(data.prediction))
                    {
                        var current = scores[data.prediction];
                        current.totalScore += (float)data.score;
                        current.count += 1;
                        scores[data.prediction] = current;
                    }
                    else
                    {
                        scores[data.prediction] = ((float)data.score, 1);
                    }
                }
            }

            List<ResponseConstructsAverages> constructAveragesList = new List<ResponseConstructsAverages>();
            List<ResponseMetricsAverages> metricAveragesList = new List<ResponseMetricsAverages>();

            foreach (var kvp in scores)
            {
                if (Enum.TryParse(kvp.Key, out ConstructType construct))
                {
                    constructAveragesList.Add(new ResponseConstructsAverages
                    {
                        construct = construct,
                        averageScore = kvp.Value.totalScore / kvp.Value.count
                    });
                }
                else if (Enum.TryParse(kvp.Key, out MetricsType metric))
                {
                    metricAveragesList.Add(new ResponseMetricsAverages
                    {
                        metric = metric,
                        averageScore = kvp.Value.totalScore / kvp.Value.count
                    });
                }
                else
                {
                    Debug.LogError($"[EyeDataStorage] No construct or metric type found for prediction: {kvp.Key}");
                }
            }

            currentResponseSet.responseConstructsAverages = constructAveragesList;
            currentResponseSet.responseMetricsAverages = metricAveragesList;

            OnDataUpdated?.Invoke();
        }

        public static float GetMetricAverage(EyeDataStorage.MetricsType metricType = MetricsType.fixation_duration_mean)
        {
            var metricsAverages = EyeDataStorage.Instance.currentResponseSet?.responseMetricsAverages;
            if (metricsAverages == null || metricsAverages.Count == 0)
            {
                Debug.LogWarning("No metric averages available in the current response set.");
                return 0f;
            }
            foreach (var metricAvg in metricsAverages)
            {
                if (metricAvg.metric == metricType)
                {
                    return metricAvg.averageScore;
                }
            }
            Debug.LogWarning($"Metric {metricType} not found in the current response set averages.");
            return 0f;
        }

        public static float GetConstructAverage(EyeDataStorage.ConstructType constructType = ConstructType.stress)
        {
            var constructsAverages = EyeDataStorage.Instance.currentResponseSet?.responseConstructsAverages;
            if (constructsAverages == null || constructsAverages.Count == 0)
            {
                Debug.LogWarning("No metric averages available in the current response set");
                return 0f;
            }
            foreach (var constructAvg in constructsAverages)
            {
                if (constructAvg.construct == constructType)
                {
                    return constructAvg.averageScore;
                }
            }
            Debug.LogWarning($"Metric {constructType} not found in the current response set averages.");
            return 0f;
        }


        [ContextMenu("Save JSON")]
        public void SaveJson()
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                },
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented
            };
            string json = JsonConvert.SerializeObject(eyeDataCollectionWrapper, settings);

            // File path with timestamp
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string filename = $"eye_tracking_{timestamp}.json";
            string path = Path.Combine(Application.streamingAssetsPath, filename);

            // Save file
            File.WriteAllText(path, json);

            Debug.Log($"JSON saved to: {path}");
            Debug.Log(json);
        }

    }
}