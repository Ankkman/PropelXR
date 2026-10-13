using UnityEngine;
using Meta.XR.MRUtilityKit;
using System.Collections;

public class RoomDiagnosticLoader : MonoBehaviour
{
    [Tooltip("Drag the drone's 3D model here so it can change color")]
    public MeshRenderer droneRenderer; 

    IEnumerator Start()
    {
        // 1. Wait a full 5 seconds for AR Foundation to finish its setup
        yield return new WaitForSeconds(5.0f);
        
        if (MRUK.Instance != null)
        {
            // 2. Attempt to pull the room from the OS
            MRUK.Instance.LoadSceneFromDevice();
            
            // 3. Wait 2 seconds for the geometry to generate
            yield return new WaitForSeconds(2.0f);
            
            // 4. Check if the room actually loaded
            if (MRUK.Instance.GetCurrentRoom() != null)
            {
                // SUCCESS: MRUK found the room. Turn the drone GREEN.
                if (droneRenderer) droneRenderer.material.color = Color.green;
            }
            else
            {
                // FAIL: MRUK was blocked by the OS or AR Foundation. Turn drone RED.
                if (droneRenderer) droneRenderer.material.color = Color.red;
            }
        }
    }
}