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
        public Transform head;
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

        [Header("Eye Visualization")]
        //This is to show your eye direction.
        public Transform leftEyeDebugger;
        public LineRenderer leftEyeLineRenderer;
        public Transform rightEyeDebugger;
        public LineRenderer rightEyeLineRenderer;

        void Awake()
        {
            head = Camera.main.transform;
        }

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
            EyeDataStorage.Head headData = data.head;

            Vector3 dummyHeadPosition = new Vector3(UnityEngine.Random.Range(-0.1f, 0.1f), UnityEngine.Random.Range(1.5f, 1.7f), UnityEngine.Random.Range(-0.1f, 0.1f));
            Vector3 dummyHeadDirection = CreateRandomUnitVector(true);
            headData.Position.Add(dummyHeadPosition);
            headData.Direction.Add(dummyHeadDirection);

            Vector3 gazeDir = CreateRandomUnitVector(true);
            eye.GazeDirection.Add(gazeDir);
            eye.GazeObject.Add(currentGazedAtObject);
            eye.PupilDiameter.Add(UnityEngine.Random.Range(2f, 8f));
            eye.Openness.Add(UnityEngine.Random.Range(0f, 1f));

            if (debugData)
            {
                Debug.Log($"Dummy EyeData: GazeDirection=({gazeDir.x:F4},{gazeDir.y:F4},{gazeDir.z:F4}), GazeObject={eye.GazeObject[0]}, PupilDiameter={eye.PupilDiameter[0]}, Openness={eye.Openness[0]}, HeadPosition=({dummyHeadPosition.x:F2},{dummyHeadPosition.y:F2},{dummyHeadPosition.z:F2}), HeadDirection=({dummyHeadDirection.x:F2},{dummyHeadDirection.y:F2},{dummyHeadDirection.z:F2})");
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

        ///<summary>
        /// This will convert the eye gaze data from the Vive format to a forward direction vector in Unity's coordinate system.
        /// </summary>
        Vector3 GetEyeForward(XrSingleEyeGazeDataHTC eyeData)
        {
            Quaternion gazeRot = eyeData.gazePose.orientation.ToUnityQuaternion();
            return (gazeRot * Vector3.forward).normalized;
        }

        /// <summary>
        /// In the update we are getting the Vive Eye Tracking Data and converting it to our EyeData format, as well as invoking the EyeTrackingDataChanged event for any listeners to update with the new data. We are also doing a debug raycast to show the gaze direction in the scene.
        /// </summary>
        void Update()
        {
            if (eyeTrackingMode == EyeTrackingMode.DummyData)
            {
                DummyDataProcessor();
                if (leftEyeLineRenderer != null && leftGazeTransform != null)
                {
                    Vector3 origin = leftGazeTransform.position;
                    Vector3 direction = leftGazeTransform.forward;
                    leftEyeLineRenderer.positionCount = 2;
                    leftEyeLineRenderer.SetPosition(0, origin);
                    leftEyeLineRenderer.SetPosition(1, origin + direction * raycastDistance);
                }
                if (rightEyeLineRenderer != null && rightGazeTransform != null)
                {
                    Vector3 origin = rightGazeTransform.position;
                    Vector3 direction = rightGazeTransform.forward;
                    rightEyeLineRenderer.positionCount = 2;
                    rightEyeLineRenderer.SetPosition(0, origin);
                    rightEyeLineRenderer.SetPosition(1, origin + direction * raycastDistance);
                }
            }
            else
            {
                bool isEyeTrackingEnabled = XR_HTC_eye_tracker.Interop.GetEyeGazeData(out XrSingleEyeGazeDataHTC[] gazes);
                if (isEyeTrackingEnabled)
                {
                    XR_HTC_eye_tracker.Interop.GetEyeGazeData(out XrSingleEyeGazeDataHTC[] out_gazes);
                    XR_HTC_eye_tracker.Interop.GetEyePupilData(out XrSingleEyePupilDataHTC[] out_pupils);
                    XR_HTC_eye_tracker.Interop.GetEyeGeometricData(out XrSingleEyeGeometricDataHTC[] out_geometric);

                    leftGaze = out_gazes[(int)XrEyePositionHTC.XR_EYE_POSITION_LEFT_HTC];
                    rightGaze = out_gazes[(int)XrEyePositionHTC.XR_EYE_POSITION_RIGHT_HTC];
                    leftPupil = out_pupils[(int)XrEyePositionHTC.XR_EYE_POSITION_LEFT_HTC];
                    rightPupil = out_pupils[(int)XrEyePositionHTC.XR_EYE_POSITION_RIGHT_HTC];

                    EyeDataStorage.EyeDataCollection data = new EyeDataStorage.EyeDataCollection();
                    EyeDataStorage.Head headData = data.head;
                    headData.Direction.Add(head.forward);
                    headData.Position.Add(head.position);

                    data.timestamp.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffffK"));
                    EyeDataStorage.Eye eye = data.eye;

                    Vector3 leftGazeForward = head.InverseTransformDirection(GetEyeForward(leftGaze));
                    Vector3 rightGazeForward = head.InverseTransformDirection(GetEyeForward(rightGaze));

                    if (leftGazeForward.z > 0) leftGazeForward.z = -leftGazeForward.z;
                    if (rightGazeForward.z > 0) rightGazeForward.z = -rightGazeForward.z;

                    Vector3 combinedGazeForward = ((leftGazeForward + rightGazeForward) / 2).normalized;

                    eye.GazeDirection.Add(combinedGazeForward);
                    eye.GazeObject.Add(currentGazedAtObject);
                    eye.PupilDiameter.Add((leftPupil.pupilDiameter + rightPupil.pupilDiameter) / 2f);

                    if (leftEyeLineRenderer != null)
                    {
                        if (leftGaze.isValid)
                        {
                            leftEyeLineRenderer.enabled = true;
                            Vector3 leftWorldDir = GetEyeForward(leftGaze);
                            Vector3 origin = head.position;
                            leftEyeLineRenderer.positionCount = 2;
                            leftEyeLineRenderer.SetPosition(0, origin);
                            leftEyeLineRenderer.SetPosition(1, origin + leftWorldDir * raycastDistance);
                        }
                        else
                        {
                            leftEyeLineRenderer.enabled = false;
                        }
                    }
                    if (rightEyeLineRenderer != null)
                    {
                        if (rightGaze.isValid)
                        {
                            rightEyeLineRenderer.enabled = true;
                            Vector3 rightWorldDir = GetEyeForward(rightGaze);
                            Vector3 origin = head.position;
                            rightEyeLineRenderer.positionCount = 2;
                            rightEyeLineRenderer.SetPosition(0, origin);
                            rightEyeLineRenderer.SetPosition(1, origin + rightWorldDir * raycastDistance);
                        }
                        else
                        {
                            rightEyeLineRenderer.enabled = false;
                        }
                    }

                    leftGeometricData = out_geometric[(int)XrEyePositionHTC.XR_EYE_POSITION_LEFT_HTC];
                    rightGeometricData = out_geometric[(int)XrEyePositionHTC.XR_EYE_POSITION_RIGHT_HTC];
                    eye.Openness.Add((leftGeometricData.eyeOpenness + rightGeometricData.eyeOpenness) / 2f);

                    if (isRecording)
                    {
                        EyeDataStorage.Instance.UpdateEyeData(data);
                    }
                    EyeTrackingDataChanged?.Invoke(data);
                }
            }
        }
    }
}
#endif