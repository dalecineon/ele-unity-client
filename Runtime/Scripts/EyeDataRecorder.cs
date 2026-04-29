using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.OpenXR.Features.Interactions;

namespace Cineon.ELE.Utils
{
    public class EyeDataRecorder : MonoBehaviour
    {
        [Header("User defined settings")]
        [Tooltip("If true, the recording will start automatically when the application starts.")]
        public bool recordAtStartUp = false;

        public void Start()
        {
            if (recordAtStartUp)
            {
                StartRecording();
            }
        }

        /// <summary>
        /// Starts recording eye data. This will trigger the RecordingStateChanged event with the Start state.
        /// </summary>
        public void StartRecording()
        {
            HTCViveHeadsetData.RecordingStateChanged?.Invoke(HTCViveHeadsetData.RecordingState.Start);
        }

        /// <summary>
        /// Stops recording eye data. This will trigger the RecordingStateChanged event with the Stop state.
        /// </summary>
        public void StopRecording()
        {
            HTCViveHeadsetData.RecordingStateChanged?.Invoke(HTCViveHeadsetData.RecordingState.Stop);
        }
    }
}
