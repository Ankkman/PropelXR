using UnityEngine;
using Meta.XR.MRUtilityKit;
using System.Collections;

public class DelayedRoomLoader : MonoBehaviour
{
    void Start()
    {
        // Wait 2 seconds for AR Foundation to initialize the tracking space
        StartCoroutine(LoadRoomRoutine());
    }

    IEnumerator LoadRoomRoutine()
    {
        yield return new WaitForSeconds(2.0f);
        
        if (MRUK.Instance != null)
        {
            Debug.Log("Attempting to load MRUK Scene...");
            MRUK.Instance.LoadSceneFromDevice();
        }
    }
}