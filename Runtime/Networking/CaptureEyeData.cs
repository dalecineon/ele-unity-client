using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cineon.ELE.Networking;

namespace Cineon.ELE.Networking
{
    /// <summary>
    /// This is an example script to show how to capture eye data using the ELEViveEyeTrackingBridge. You can call the StartCapture and StopCapture functions to control the recording of the eye data.
    /// </summary>
    public class CaptureEyeData : MonoBehaviour
    {
        [SerializeField]
        private bool startCaptureOnStart = false; //Set this to true if you want to start the capture process on the start.

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
        public void StartCapture()
        {
            ELEViveEyeTrackingBridge.RecordingStateChanged?.Invoke(ELEViveEyeTrackingBridge.RecordingState.Start);
        }

        /// <summary>
        /// This is an example on how to stop the eye capture process.
        /// </summary>
        public void StopCapture()
        {
            ELEViveEyeTrackingBridge.RecordingStateChanged?.Invoke(ELEViveEyeTrackingBridge.RecordingState.Stop);
        }

    }
}