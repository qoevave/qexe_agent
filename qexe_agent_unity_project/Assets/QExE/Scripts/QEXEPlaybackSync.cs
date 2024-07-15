using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QEXEPlaybackSync : MonoBehaviour
{
    public GameObject[] AnimatedObjects;

    // Start is called before the first frame update
    void Start()
    {
        foreach (GameObject animatedObject in AnimatedObjects)
		{
            animatedObject.SetActive(false);
		}
    }

    public void StartAnimations()
	{
        foreach (GameObject animatedObject in AnimatedObjects)
        {
            animatedObject.SetActive(true);
        }
    }
}
