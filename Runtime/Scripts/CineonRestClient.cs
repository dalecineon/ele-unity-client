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

namespace Cineon.ELE.Networking
{
    public static class CineonRestClient
    {
        private static Coroutine pingCoroutine;
        public static Action<string> OnServerError; //This is an event which any script can subscribe to, to get error responses from the Cineon Rest Client.
        public static Action<bool> OnPingDetected; //This event is fired when a ping is detected, this maybe after a certain amount of time.
        public static string version = "1.0.0"; //This is the version of the Cineon Rest Client.
        public static string platform = Application.platform.ToString(); //This is the platform of the Cineon Rest Client.

        /// <summary>
        /// Sends Json Data to a server using a post request. 
        /// The Json Data must be sent in the correct format to get a response.
        /// Please check the api for this information https://ele-api-479937931673.europe-west2.run.app/docs
        /// </summary>
        /// <typeparam name="TRequest">A Generic Request class, but needs to be setup in the way the rest API needs it. Check the documents above.</typeparam>
        /// <typeparam name="TResponse">A Generic Response class, but needs to be setup in the way the rest API needs it. Check the documents above.</typeparam>
        /// <param name="_url">Chosen Server Url</param>
        /// <param name="_data">Json Data</param>
        /// <returns>A TResponse which can be used to populate a class.</returns>
        public static async Task<TResponse> Post<TRequest, TResponse>(string _url, TRequest _data)
        {
            string json = SerializeToJson(_data);
            Debug.Log($"Serialized JSON: {json}");
            using (UnityWebRequest request = new UnityWebRequest(_url, "POST"))
            {
                Debug.Log(_url);
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

        private static TResponse DeserializeFromJson<TResponse>(string data)
        {
            Debug.Log(data);
            return JsonConvert.DeserializeObject<TResponse>(data);
        }

        /// Starts a coroutine to periodically ping a server at the specified URL.
        /// </summary>
        /// <param name="context">The MonoBehaviour context used to start the coroutine.</param>
        /// <param name="_url">The URL of the server to ping.</param>
        /// <param name="_pingInterval">The interval in seconds between pings. If set to 0, the default interval is used.</param>
        /// This pings a server to see if it the server is active and then gives a response time back.
        /// You can also setup a pingInterval and it will ping the server after x amount of seconds.
        /// </summary>
        public static void Ping(MonoBehaviour context, string _url, int _pingInterval = 0)
        {
            pingCoroutine = context.StartCoroutine(PingServer(_url, _pingInterval));
        }

        /// <summary>
        /// This pings a server to see if it the server is active and then gives a response time back.
        /// You can also setup a pingInterval and it will ping the server after x amount of seconds.
        /// </summary>
        /// <param name="_url">This is the url of the server you want to check the ping on.</param>
        /// <param name="pingInterval">This is how often you wish to ping the server to check a connection.</param>
        public static IEnumerator PingServer(string _url, int _pingInterval = 0)
        {
            do
            {
                float startTime = Time.time;
                float timeout = 5f;
                using (UnityWebRequest request = UnityWebRequest.Get(_url))
                {
                    request.SetRequestHeader("x-api-key", EyeDataDistributor.Instance.apiKey);
                    yield return request.SendWebRequest();
                    while (!request.isDone && Time.time - startTime < timeout)
                    {
                        OnPingDetected?.Invoke(false);
                        yield return null;
                    }
                    switch (request.result)
                    {
                        case UnityWebRequest.Result.ConnectionError:
                        case UnityWebRequest.Result.DataProcessingError:
                            OnPingDetected?.Invoke(false);
                            Debug.LogError($"Error: {request.error}");
                            break;
                        case UnityWebRequest.Result.ProtocolError:
                            OnPingDetected?.Invoke(false);
                            Debug.LogError($"HTTP Error: {request.error}");
                            break;
                        case UnityWebRequest.Result.Success:
                            OnPingDetected?.Invoke(true);
                            Debug.Log($"Success {request.downloadHandler.text}");
                            break;
                    }
                    if (request.isDone)
                    {
                        Debug.Log($"Ping to {_url}");
                    }
                    if (_pingInterval > 0)
                    {
                        yield return new WaitForSeconds(_pingInterval);
                    }
                }
            } while (_pingInterval > 0);
        }

        /// <summary>
        /// This stops the repeating ping.
        /// </summary>
        /// <param name="context">The MonoBehaviour context used to start the coroutine.</param>
        public static void StopPing(MonoBehaviour context)
        {
            if (context != null)
            {
                OnPingDetected?.Invoke(false);
                context.StopCoroutine(pingCoroutine);
            }
        }
    }

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
