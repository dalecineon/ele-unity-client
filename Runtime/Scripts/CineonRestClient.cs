using System;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using Newtonsoft.Json.Converters;
using System.Threading;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Cineon.ELE.Networking
{
    public static class CineonRestClient
    {
        public static Action<string> OnServerError; //This is an event which any script can subscribe to, to get error responses from the Cineon Rest Client.
        public static Action<bool, float> OnPingUpdated; //This event is fired when a ping is detected, this maybe after a certain amount of time. It also gives the ping time in milliseconds.
        private static CancellationTokenSource pingCancellationTokenSource; //This is used to cancel the ping coroutine when needed.
        public static string version = "1.0.0"; //This is the version of the Cineon Rest Client.
        public static string platform = Application.platform.ToString(); //This is the platform of the Cineon Rest Client.
#if UNITY_EDITOR
        static CineonRestClient()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.EnteredEditMode)
                    StopPingLoop();
            };
        }
#endif
        /// <summary>
        /// Store the url endpoints for the server.
        /// </summary>
        public static class ServerURL
        {
            public static string BaseURL;
            public static string pingURL => $"{BaseURL}/ping";
            public static string inferencePath => $"{BaseURL}/inference";
        }
        #region POST Request Functionality
        /// <summary>
        /// Sends Json Data to a server using a post request. 
        /// The Json Data must be sent in the correct format to get a response.
        /// Please check the api for this information https://ele-api-479937931673.europe-west2.run.app/docs
        /// </summary>
        /// <typeparam name="TRequest">A Generic Request class, but needs to be setup in the way the rest API needs it. Check the documents above.</typeparam>
        /// <typeparam name="TResponse">A Generic Response class, but needs to be setup in the way the rest API needs it. Check the documents above.</typeparam>
        /// <param name="_data">Json Data</param>
        /// <returns>A TResponse which can be used to populate a class.</returns>
        public static async Task<TResponse> Post<TRequest, TResponse>(TRequest _data)
        {
            string json = SerializeToJson(_data);
            Debug.Log($"Serialized JSON: {json}");
            using (UnityWebRequest request = new UnityWebRequest(ServerURL.inferencePath, "POST"))
            {
                Debug.Log(ServerURL.inferencePath);
                byte[] rawBody = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(rawBody);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("x-api-key", EyeDataDistributor.Instance.apiKey);
                request.SetRequestHeader("Content-Type", "application/json");
                UnityWebRequestAsyncOperation webAsyncOperation;
                try
                {
                    webAsyncOperation = request.SendWebRequest();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to Send Web Request {ex.Message}");
                    OnServerError?.Invoke(ex.Message);
                    throw;
                }
                while (!webAsyncOperation.isDone)
                {
                    await Task.Yield();
                }
                if (request.result == UnityWebRequest.Result.ConnectionError)
                {
                    Debug.LogError($"Connection Error: {request.error}");
                    OnServerError?.Invoke($"Connection Error: {request.error}");
                    throw new Exception($"Connection Error: {request.error}");
                }
                else if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"POST Error: {request.error} Response : {request.downloadHandler.text} ");
                    OnServerError?.Invoke($"POST Error: {request.error} Response : {request.downloadHandler.text} ");
                    throw new Exception($"POST Error: {request.error}");
                }
                else if (request.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"POST Error: {request.error} Response : {request.downloadHandler.text} ");
                    OnServerError?.Invoke($"POST Error: {request.error} Response : {request.downloadHandler.text} ");
                    throw new Exception($"POST Error: {request.error}");
                }
                else if (request.result == UnityWebRequest.Result.DataProcessingError)
                {
                    Debug.LogError($"POST Error: {request.error} Response : {request.downloadHandler.text} ");
                    OnServerError?.Invoke($"POST Error: {request.error} Response : {request.downloadHandler.text} ");
                    throw new Exception($"POST Error: {request.error}");
                }
                try
                {
                    Debug.Log($"T RESPONSE : {request.downloadHandler.text}");
                    return DeserializeFromJson<TResponse>(request.downloadHandler.text);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Deserialization Error {ex.Message} Response : {request.downloadHandler.text}");
                    OnServerError?.Invoke($"Deserialization Error {ex.Message} Response : {request.downloadHandler.text}");
                    throw;
                }
            }
        }

        #endregion

        #region Serialization and Deserialization
        /// <summary>
        /// This serializes the data to a JSON string.
        /// </summary>
        private static string SerializeToJson<TRequest>(TRequest data)
        {
            try
            {
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
                return json;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Serialization Error: {ex.Message}");
                OnServerError?.Invoke($"Serialization Error: {ex.Message}");
                throw;
            }
        }
        /// <summary>
        /// Deserializes a JSON string into an object of type TResponse. It also logs the JSON data for debugging purposes. If the JSON is not in the correct format or if there is an error during deserialization, it will log an error message and invoke the OnServerError event with the error details.
        /// </summary>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        private static TResponse DeserializeFromJson<TResponse>(string data)
        {
            Debug.Log(data);
            return JsonConvert.DeserializeObject<TResponse>(data);
        }

        #endregion

        #region Ping Functionality
        /// <summary>
        /// This method checks if a server is live by sending a GET request to the specified URL.
        /// If attempts is 0, it loops continuously with a delay of pingCheckDelayMs between each check until the server responds.
        /// If attempts is greater than 0, it tries that many times before returning failure.
        /// </summary>
        /// <param name="attempts">Number of ping attempts. 0 = continuous loop until success.</param>
        /// <param name="pingCheckDelayMs">Delay in milliseconds between ping checks (used when attempts is 0).</param>
        public static async Task<(bool isLive, float pingMs)> PingRequest(int attempts = 0, int pingCheckDelayMs = 2000, CancellationToken cancellationToken = default)
        {
            Debug.Log($"Server URL : {ServerURL.pingURL}");

            bool continuous = attempts == 0;
            if (!continuous)
                attempts = Mathf.Max(1, attempts);

            int i = 0;
            while ((continuous || i < attempts) && Application.isPlaying && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    float startTime = Time.realtimeSinceStartup;
                    using UnityWebRequest request = UnityWebRequest.Get(ServerURL.pingURL);
                    request.timeout = 5;
                    var operation = request.SendWebRequest();
                    while (!operation.isDone)
                    {
                        if (!Application.isPlaying || cancellationToken.IsCancellationRequested)
                        {
                            request.Abort();
                            cancellationToken.ThrowIfCancellationRequested();
                            return (false, -1f);
                        }
                        await Task.Yield();
                    }
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        float pingMs = (Time.realtimeSinceStartup - startTime) * 1000f;
                        return (true, pingMs);
                    }
                }
                catch (OperationCanceledException)
                {
                    return (false, -1f);
                }
                catch
                {
                    if (Application.isPlaying && !cancellationToken.IsCancellationRequested)
                        Debug.LogWarning($"Attempt {i + 1} failed to ping server.");
                }
                if (continuous)
                    await Task.Delay(pingCheckDelayMs, cancellationToken);
                else if (i < attempts - 1)
                    await Task.Delay(100, cancellationToken);

                i++;
            }
            return (false, -1f);
        }

        /// <summary>
        /// This method pings the server once to check the server is live and awake.
        /// </summary>
        public static void PingOnce()
        {
            Task.Run(async () =>
            {
                var (isLive, pingMs) = await PingRequest(1, cancellationToken: CancellationToken.None);
                OnPingUpdated?.Invoke(isLive, pingMs);
            });
        }
        ///<summary>
        /// This method starts a loop that continuously pings the server at regular intervals (default is every 2 seconds). It uses a CancellationTokenSource to allow stopping the loop when needed. The ping results are invoked through the OnPingUpdated event, which provides both the server status (live or not) and the ping time in milliseconds. This can be useful for keeping track of the server's availability and response time over time.
        /// </summary>
        public static void StartPingLoop(int attempts = 0, int pingCheckDelayMs = 2000)
        {
            if (pingCancellationTokenSource != null)
            {
                Debug.LogWarning("Ping loop is already running.");
                return;
            }
            pingCancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = pingCancellationTokenSource.Token;
            Task.Run(async () =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        var (isLive, pingMs) = await PingRequest(attempts, pingCheckDelayMs, token);
                        if (token.IsCancellationRequested)
                            break;

                        OnPingUpdated?.Invoke(isLive, pingMs);
                        await Task.Delay(pingCheckDelayMs, token);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when the ping loop is stopped.
                }
            }, token);
        }
        /// <summary>
        /// This method starts a loop that continuously pings the server at the specified URL at regular intervals.
        /// </summary>
        public static void StopPingLoop()
        {
            if (pingCancellationTokenSource == null)
                return;

            pingCancellationTokenSource.Cancel();
            pingCancellationTokenSource.Dispose();
            pingCancellationTokenSource = null;
        }
        #endregion
    }
    /// <summary>
    /// This is a custom JsonConverter that converts enum values to lowercase strings when serializing and parses them back to enum values when deserializing. This is useful for ensuring that enum values are consistently formatted in JSON, especially when the API expects lowercase strings.
    /// </summary>
    public class LowercaseEnumConverter : StringEnumConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString().ToLower());
        }
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.Value == null) return null;
            return Enum.Parse(objectType, reader.Value.ToString(), ignoreCase: true);
        }
    }
}
