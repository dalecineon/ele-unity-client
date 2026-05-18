using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cineon.ELE.Utils
{
    /// <summary>
    /// This is an example script to show how to capture eye data using the ELEViveEyeTrackingBridge. You can call the StartCapture and StopCapture functions to control the recording of the eye data.
    /// </summary>
    public class ELERecorder : ELEMonoBehaviour
    {
        [SerializeField]
        private bool startCaptureOnStart = false; //Set this to true if you want to start the capture process on the start.
        
        private static bool isRecording = false; // Tracks whether recording is in progress.

        public static bool IsRecording => isRecording; // Public getter for the recording state.

        /// <summary>
        /// In the start we check to see if the user wants to start the capture process immediately, if so we call the StartCapture function.
        /// </summary>
        void Start()
        {
            if (startCaptureOnStart)
            {
                StartCapture();
            }
        }

        /// <summary>
        /// This is an example on how to start the eye capture process.
        /// </summary>
        public static void StartCapture()
        {
            if (!isRecording) // Prevent redundant state changes.
            {
                isRecording = true;
                ELEViveEyeTrackingBridge.RecordingStateChanged?.Invoke(ELEViveEyeTrackingBridge.RecordingState.Start);
            }
        }

        /// <summary>
        /// This is an example on how to stop the eye capture process.
        /// </summary>
        public static void StopCapture()
        {
            if (isRecording) // Prevent redundant state changes.
            {
                isRecording = false;
                ELEViveEyeTrackingBridge.RecordingStateChanged?.Invoke(ELEViveEyeTrackingBridge.RecordingState.Stop);
            }
        }

    }
}