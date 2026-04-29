using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cineon.ELE.Networking;

namespace Cineon.ELE.Networking
{
    /// <summary>
    /// This is a simple script to check for the ping response from the server. You can use this to check if the server is live and responding to pings.
    /// </summary>
    public class ELEPingCheck : MonoBehaviour
    {
        [SerializeField]
        private bool StopPingAfterFirstCheck = false; //This stops the ping after a live connection has been found.

        /// <summary>
        /// Here we subscribe to detect the ping.
        /// </summary>
        private void OnEnable()
        {
            CineonRestClient.OnPingDetected += PingResponse;
        }

        /// <summary>
        /// Here we Unsubscribe the ping listener.
        /// </summary>
        private void OnDisable()
        {
            CineonRestClient.OnPingDetected -= PingResponse;
        }

        /// On the start we begin the ping process by invoking the BeginPing event.
        void Start()
        {
            EyeDataDistributor.BeginPing?.Invoke(2);
        }

        /// <summary>
        /// This checks if the ping response from the server is live or not.
        /// If ping has been found and the StopPingAfterFirstCheck is true, it will stop the ping and unsubscribe the listener.
        /// </summary>
        /// <param name="isLive">Indicates whether the server is live or not.</param>
        void PingResponse(bool isLive)
        {
            if (isLive)
            {
                Debug.Log("Ping Found, Stop Ping.");
                if (StopPingAfterFirstCheck)
                {
                    EyeDataDistributor.EndPing?.Invoke();
                    CineonRestClient.OnPingDetected -= PingResponse;
                }
            }
            else
            {
                Debug.Log("Ping Not Found.");
            }
        }
    }
}
