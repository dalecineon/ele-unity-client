using System;
using Cineon.ELE.Storage;
using Cineon.ELE.Networking;
using UnityEngine;

#if VIVE_OPENXR
using VIVE.OpenXR;
using VIVE.OpenXR.EyeTracker;

namespace Cineon.ELE.Utils
{
    public class ELEViveEyeTrackingBridge : ELEMonoBehaviour
    {
        //Enum for the recording state.
        public enum RecordingState
        {
            Start,
            Stop
        }

        //Enum for the eye tracking mode, either using dummy data or real device data.
        public enum EyeTrackingMode
        {
            DummyData,
            DeviceData
        };

        public Transform leftGazeTransform = null;
        public Transform rightGazeTransform = null;
        public bool isRecording = false; //This is the recording state of the headset data.

        [Header("VIVE only Settings")]
        public XrSingleEyeGazeDataHTC leftGaze;
        public XrSingleEyeGazeDataHTC rightGaze;
        public XrSingleEyePupilDataHTC leftPupil;
        public XrSingleEyePupilDataHTC rightPupil;
        public XrSingleEyeGeometricDataHTC leftGeometricData;
        public XrSingleEyeGeometricDataHTC rightGeometricData;

        [Header("Eye Tracking Event Listeners")]
        public static Action<EyeDataStorage.EyeDataCollection> EyeTrackingDataChanged;
        public static Action<RecordingState> RecordingStateChanged; //This event is fired when the recording state changes.
        public static Action<string, bool> EyeColliderChanged; //This event is fired when the eye collider changes.

        public string currentGazedAtObject = "null";

        [Header("Debug Settings")]
        [Tooltip("Choose whether to use dummy data or real device data for eye tracking.")]
        public EyeTrackingMode eyeTrackingMode = EyeTrackingMode.DeviceData;
        public bool debugRaycast = false;
        public bool debugData = false;
        public float raycastDistance = 10f;
        public Color rayColour = Color.red;
        public LineRenderer lineRenderer;

        /// <summary>
        /// Listening to the recording state changes.
        /// </summary>
        void OnEnable()
        {
            RecordingStateChanged += OnRecordingStateChanged;
            EyeColliderChanged += OnEyeColliderChanged;
        }

        private void OnEyeColliderChanged(string obj, bool isGazedAt)
        {
            if (isGazedAt == false)
            {
                currentGazedAtObject = "null";
            }
            else
            {
                currentGazedAtObject = obj;
            }
        }

        /// <summary>
        /// Stop listening to the recording state changes.
        /// </summary>
        void OnDisable()
        {
            RecordingStateChanged -= OnRecordingStateChanged;
            EyeColliderChanged -= OnEyeColliderChanged;
        }

        /// <summary>
        /// This is called when the recording state changes.
        /// </summary>
        /// <param name="state"></param>
        public void OnRecordingStateChanged(RecordingState state)
        {
            if (state == RecordingState.Start)
            {
                Debug.Log("Recording started");
                EyeDataStorage.Instance.ClearAllData(); // Clear previous data before starting a new recording
                EyeDataDistributor.StartDataCapture?.Invoke();
                isRecording = true;
            }
            else if (state == RecordingState.Stop)
            {
                Debug.Log("Recording stopped");
                EyeDataDistributor.StopDataCapture?.Invoke();
                EyeDataStorage.Instance.ClearEyeData();
                //EyeData.Instance.SaveToJson(); // Save the data to JSON when recording stops
                isRecording = false;
            }
        }

        ///<summary>
        /// This is a dummy data processor that generates random eye tracking data for testing purposes when the eye tracking mode is set to DummyData.
        /// </summary>
        void DummyDataProcessor()
        {
            EyeDataStorage.EyeDataCollection data = new EyeDataStorage.EyeDataCollection();
            data.timestamp.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffffK"));
            EyeDataStorage.Eye eye = data.eye;

            // Set dummy values for all properties
            Vector3 gazeDir = CreateRandomUnitVector(true);
            eye.GazeDirection.Add(gazeDir);
            eye.GazeDepth.Add(UnityEngine.Random.Range(0.5f, 2f));
            eye.GazeObject.Add(currentGazedAtObject);
            eye.PupilDiameter.Add(UnityEngine.Random.Range(2f, 8f));
            eye.Openness.Add(UnityEngine.Random.Range(0f, 1f));
            
            if(debugData){
                Debug.Log($"Dummy EyeData: GazeDirection=({gazeDir.x:F4},{gazeDir.y:F4},{gazeDir.z:F4}), GazeDepth={eye.GazeDepth[0]}, GazeObject={eye.GazeObject[0]}, PupilDiameter={eye.PupilDiameter[0]}, Openness={eye.Openness[0]}");
            }
            
            EyeDataStorage.Instance.UpdateEyeData(data);
            EyeTrackingDataChanged?.Invoke(data);
        }

        public Vector3 CreateRandomUnitVector(bool forward = false)
        {
            float azimin = forward ? -Mathf.PI / 2f : -Mathf.PI;
            float azimax = forward ? Mathf.PI / 2f : Mathf.PI;
            float azimuth = UnityEngine.Random.Range(azimin, azimax);
            float sinElevation = UnityEngine.Random.Range(-1f, 1f);

            float cosElevation = Mathf.Sqrt(1f - sinElevation * sinElevation);
            float x = cosElevation * Mathf.Sin(azimuth);
            float y = sinElevation;
            float z = cosElevation * Mathf.Cos(azimuth);
            return new Vector3(x, y, z);
        }

        /// <summary>
        /// In the update we are getting the Vive Eye Tracking D-+ata.
        /// </summary>
        void Update()
        {
            if (eyeTrackingMode == EyeTrackingMode.DummyData)
            {
                DummyDataProcessor();
            }
            // else
            // {
            //     //Check eye tracking is enabled and if true start recording the data to the EyeData class.
            //     bool isEyeTrackingEnabled = XR_HTC_eye_tracker.Interop.GetEyeGazeData(out XrSingleEyeGazeDataHTC[] gazes);
            //     if (isEyeTrackingEnabled)
            //     {
            //         //This is for the Eye Gaze Data.
            //         XR_HTC_eye_tracker.Interop.GetEyeGazeData(out XrSingleEyeGazeDataHTC[] out_gazes);
            //         leftGaze = out_gazes[(int)XrEyePositionHTC.XR_EYE_POSITION_LEFT_HTC];
            //         rightGaze = out_gazes[(int)XrEyePositionHTC.XR_EYE_POSITION_RIGHT_HTC];

            //         EyeDataStorage.EyeDataCollection data = new EyeDataStorage.EyeDataCollection();
            //         data.timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffffK");
            //         EyeDataStorage.Eye eye = data.eye;

            //         _leftEye.IsGazeValid = leftGaze.isValid;
            //         _rightEye.IsGazeValid = rightGaze.isValid;

            //         //Gaze Position
            //         Vector3 leftGazeVec = leftGaze.gazePose.position.ToUnityVector();
            //         Vector3 rightGazeVec = rightGaze.gazePose.position.ToUnityVector();

            //         leftGazeTransform.position = leftGazeVec;
            //         leftGazeTransform.rotation = leftGaze.gazePose.orientation.ToUnityQuaternion();
            //         rightGazeTransform.position = rightGazeVec;
            //         rightGazeTransform.rotation = rightGaze.gazePose.orientation.ToUnityQuaternion();

            //         _leftEye.ObjectGazedAt = currentGazedAtObject;
            //         _rightEye.ObjectGazedAt = currentGazedAtObject;

            //         //Set the Eye Data.
            //         _leftEye.GazeOrigin.SetPosition(leftGazeVec);
            //         _rightEye.GazeOrigin.SetPosition(rightGazeVec);

            //         Quaternion leftGazeQuaternion = leftGaze.gazePose.orientation.ToUnityQuaternion();
            //         Quaternion rightGazeQuaternion = rightGaze.gazePose.orientation.ToUnityQuaternion();

            //         _leftEye.GazeForward.SetPosition(leftGazeQuaternion * Vector3.forward);
            //         _rightEye.GazeForward.SetPosition(rightGazeQuaternion * Vector3.forward);

            //         //Eye Pupil Data.
            //         XR_HTC_eye_tracker.Interop.GetEyePupilData(out XrSingleEyePupilDataHTC[] out_pupils);
            //         leftPupil = out_pupils[(int)XrEyePositionHTC.XR_EYE_POSITION_LEFT_HTC];
            //         rightPupil = out_pupils[(int)XrEyePositionHTC.XR_EYE_POSITION_RIGHT_HTC];
            //         _leftEye.PupilDiameter = leftPupil.pupilDiameter;
            //         _rightEye.PupilDiameter = rightPupil.pupilDiameter;

            //         //Eye Geometric Data.
            //         XR_HTC_eye_tracker.Interop.GetEyeGeometricData(out XrSingleEyeGeometricDataHTC[] out_geometric);
            //         leftGeometricData = out_geometric[(int)XrEyePositionHTC.XR_EYE_POSITION_LEFT_HTC];
            //         rightGeometricData = out_geometric[(int)XrEyePositionHTC.XR_EYE_POSITION_RIGHT_HTC];
            //         _leftEye.EyeOpenness = leftGeometricData.eyeOpenness;
            //         _rightEye.EyeOpenness = rightGeometricData.eyeOpenness;

            //         //This only processes data if recording is enabled.
            //         if (isRecording)
            //         {
            //             EyeDataStorage.Instance.UpdateEyeData(data);
            //         }
            //         EyeTrackingDataChanged?.Invoke(data);
            //     }
            // }
        }

        public RaycastHit? PerformForwardRaycast(Vector3 _origin, Vector3 _direction)
        {
            Vector3 origin = _origin;
            Vector3 direction = _direction;
            Vector3 endPoint = origin + direction * raycastDistance;

            if (lineRenderer != null)
            {
                lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, origin);
                lineRenderer.SetPosition(1, endPoint);
                lineRenderer.startColor = rayColour;
                lineRenderer.endColor = rayColour;
            }
            else
            {
                Debug.LogWarning("No Line renderer Assigned.");
            }

            // Draw the ray in Scene view
            Debug.DrawRay(origin, direction * raycastDistance, rayColour);

            // Perform the raycast
            if (Physics.Raycast(origin, direction, out RaycastHit hitInfo, raycastDistance))
            {
                Debug.Log("Raycast hit: " + hitInfo.collider.name);
                return hitInfo;
            }

            return null;
        }

    }
}
#endif