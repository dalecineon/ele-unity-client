using System.Collections;
using System.Threading.Tasks;
using Cineon.ELE.Storage;
using UnityEngine;
using Cineon.ELE.Networking;
using System;
using UnityEngine.UIElements;

//TODO
// Implement the logic to distribute eye data to the server
// Need to add logic to create gaze windows after the first 10 seconds of data.
// So 0-10s data collection then 5-15s, 10-20s etc.
// This script will request data from the EyeDataStorage script and then distribute it to the server.

namespace Cineon.ELE.Networking
{
    [RequireComponent(typeof(EyeDataStorage))]
    public class EyeDataDistributor : MonoBehaviour
    {
        public static EyeDataDistributor Instance { get; private set; } //Singleton instance of the EyeDataDistributor.

        #region Server Settings
        public enum ServerType
        {
            dev,
            production
        }
        [Header("Server Settings")]
        public ServerType serverType;
        [SerializeField]
        public string apiKey;
        [SerializeField]
        public string devServerURL = "https://ele-api-gateway-64hani55.nw.gateway.dev";
        [SerializeField]
        private string productionServerURL = "https://ele-api-gateway-64hani55.nw.gateway.dev";
        public string ServerURL => serverType == ServerType.dev ? devServerURL : productionServerURL;
        private string pingPath = "/ping";
        private string inferencePath = "/inference";
        #endregion
        private EyeDataStorage eyeDataStorage; //Reference to the EyeDataStorage script to get the eye data collection.
        private float initialWindowLength = 10f;//This is the initial length of the first gaze window. This has to be 10 seconds because the models need 10 seconds of data to make predictions.
        [Tooltip("This is the overlap gaze window time in seconds.")]
        public float rollingWindow = 5f; //This is the overlap gaze window in seconds after the first 10 seconds, so if you put 5 it would use 5-15s.

        public bool useOnlyStaticWindows = false; //This is used if you don't want to use a rolling window and just send static 10 second windows.

        private bool isFirstCollection = true;
        [SerializeField]
        private bool startDataCollectionOnStart = false; //This is a bool to start the data collection at the start.
        [SerializeField]
        private bool startPingOnStart = false; //This is a bool to start the ping at the start.

        #region Event Listeners
        /// <summary>
        /// Enter a ping amount for a repeated ping, set to 0 if you only want one ping.
        /// </summary>
        public static Action<int> BeginPing; //Use this event to begin the ping request.
        public static Action EndPing; //Use this event to end the ping request.
        [Tooltip("This will start the capturing of eye data and send the data to the server.")]
        public static Action StartDataCapture;
        [Tooltip("This will stop the capturing of eye data.")]
        public static Action StopDataCapture;
        public static Action OnServerResponseSuccess;
        public static Action CollectData;
        /// <summary>
        /// This is an event that will fire off when the server has responded back ready to start eye data collection.
        /// </summary>
        public static Action OnServerReady;
        private bool isServerReady = false;
        #endregion

        #region IEnumerator Tracking
        private Coroutine rollingWindowRoutine;
        #endregion

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Debug.LogWarning("Multiple instances of EyeDataDistributor detected. Destroying duplicate.");
                Destroy(gameObject);
            }

            if (serverType == ServerType.dev)
            {
                Debug.Log("Using Development Server: " + devServerURL);
            }
            else
            {
                Debug.Log("Using Production Server: " + productionServerURL);
            }
            eyeDataStorage = GetComponent<EyeDataStorage>();
        }

        /// <summary>
        /// We are checking to see if the eyeDataStorage is available and if not we disable the script.
        /// </summary>
        void Start()
        {
            if (eyeDataStorage == null)
            {
                Debug.LogError("EyeDataStorage reference is missing.");
                enabled = false;
                return;
            }
            if (startDataCollectionOnStart)
            {
                //Start the coroutine.
                rollingWindowRoutine = StartCoroutine(WaitForEyeDataCollection(initialWindowLength, RetrieveEyeData));
            }
            if (startPingOnStart)
            {
                StartPing();
            }
        }

        /// <summary>
        /// Here we setup the event listeners, ready to start the data capture.
        /// </summary>
        private void OnEnable()
        {
            CineonRestClient.OnServerError += ServerErrorResponse;
            CineonRestClient.OnPingDetected += PingResponse;
            BeginPing += StartPing;
            EndPing += StopPing;
            StartDataCapture += StartEyeDataCollection;
            StopDataCapture += StopEyeDataCollection;
        }

        /// <summary>
        /// Here we Destroy the event listeners on disable.
        /// </summary>
        private void OnDisable()
        {
            CineonRestClient.OnServerError -= ServerErrorResponse;
            CineonRestClient.OnPingDetected -= PingResponse;
            BeginPing -= StartPing;
            EndPing -= StopPing;
            StartDataCapture -= StartEyeDataCollection;
            StopDataCapture -= StopEyeDataCollection;
        }

        /// <summary>
        /// This logs out an error if any problems with the send request.
        /// </summary>
        /// <param name="msg"></param>
        private void ServerErrorResponse(string msg) => Debug.Log($"Server Response Message : {msg}");

        /// <summary>
        /// Waits for the initial Eye Data to then send to the server.
        /// </summary>
        /// <returns></returns>
        private IEnumerator WaitForEyeDataCollection(float delaySeconds, System.Action callback)
        {
            float repeatDelay = useOnlyStaticWindows ? delaySeconds : rollingWindow;
            yield return new WaitForSeconds(delaySeconds);
            callback?.Invoke();
            while (true)
            {
                yield return new WaitForSeconds(repeatDelay);
                callback?.Invoke();
            }
        }

        /// <summary>
        /// This checks if eye data is available and prepares the eye data ready to be sent to the server.
        /// </summary>
        private void RetrieveEyeData()
        {
            if (eyeDataStorage.eyeDataCollectionWrapper.temporaryEyeData.Count == 0)
            {
                Debug.LogWarning("No eye data available for retrieval.");
                return;
            }
            else
            {
                CollectData?.Invoke();
                if (useOnlyStaticWindows)
                {
                    eyeDataStorage.SetStaticWindow();
                    PostData().ContinueWith(task =>
                    {
                        if (task.IsFaulted)
                        {
                            Debug.LogError($"Error posting data: {task.Exception?.Message}");
                        }
                        else
                        {
                            Debug.Log("Eye Data Sent Successfully.");
                        }
                    });
                }
                else
                {
                    if (isFirstCollection)
                    {
                        eyeDataStorage.SetSampleWindow(isFirstCollection, initialWindowLength);
                        isFirstCollection = false;
                    }
                    else
                    {
                        eyeDataStorage.SetSampleWindow(isFirstCollection, initialWindowLength);
                    }
                    PostData().ContinueWith(task =>
                    {
                        if (task.IsFaulted)
                        {
                            Debug.LogError($"Error posting data: {task.Exception?.Message}");
                        }
                        else
                        {
                            Debug.Log("Eye Data Sent Successfully.");
                        }
                    });
                }
            }
        }

        /// <summary>
        /// This Posts data to the CineonRestClient Script which will send the eye data to the server.
        /// </summary>
        private async Task PostData()
        {
            EyeDataStorage.ResponseContainer response = await CineonRestClient.Post<EyeDataStorage.EyeDataCollectionWrapper, EyeDataStorage.ResponseContainer>($"{ServerURL}{inferencePath}", eyeDataStorage.eyeDataCollectionWrapper);
            if (response != null)
            {
                eyeDataStorage.AddResponseToCurrentSet(response);
                Debug.Log($"Response received: {response}");
                OnServerResponseSuccess?.Invoke();
            }
            else
            {
                Debug.LogError("Failed to receive response from server.");
            }
        }

        /// <summary>
        /// This starts the Eye Data Collection and waits to be ready to send to the Rest Api.
        /// </summary>
        private void StartEyeDataCollection()
        {
            Debug.Log($"Starting the eye data collection.");
            isFirstCollection = true;
            eyeDataStorage.ClearAllData();
            eyeDataStorage.CreateResponseSet();
            rollingWindowRoutine = StartCoroutine(WaitForEyeDataCollection(initialWindowLength, RetrieveEyeData));
        }

        /// <summary>
        /// This stops the Eye Data Collection.
        /// </summary>
        private void StopEyeDataCollection()
        {
            Debug.Log($"Stopping the eye data collection.");
            StopCoroutine(rollingWindowRoutine);
        }

        /// <summary>
        /// This is a function that gets fired when the ping is detected.
        /// </summary>
        /// <param name="isActive">The CineonRestAPI will respond with true or false on whether a ping to the server worked.</param>
        private void PingResponse(bool isActive)
        {
            Debug.Log($"Is Ping active : {isActive}");
            if (isActive && isServerReady == false)
            {
                isServerReady = true;
                OnServerReady?.Invoke();
            }
        }

        /// <summary>
        /// This starts a ping to the server to check the connection.
        /// </summary>
        /// <param name="pingIntervalTime">This is how often you want the ping to happen</param>
        private void StartPing(int pingIntervalTime = 0)
        {
            Debug.Log("Start Ping.");
            CineonRestClient.Ping(this, $"{ServerURL}{pingPath}", pingIntervalTime);
        }

        /// <summary>
        /// This is used to stop the ping to the server.
        /// </summary>
        private void StopPing()
        {
            CineonRestClient.StopPing(this);
        }

    }

}