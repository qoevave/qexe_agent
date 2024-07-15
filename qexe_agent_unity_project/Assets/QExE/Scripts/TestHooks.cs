using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using extOSC;

[RequireComponent(typeof (Canvas))]
[RequireComponent(typeof (OSCPacket))]
public class TestHooks : MonoBehaviour
{
	public bool CameraFound = false;

	public bool OSCManagerFound = false;

	private Canvas UI_CANVAS;

	private OSCPacket UI_OSC;

	private GameObject OSCMANAGER; 

	private Camera VR_MAIN_CAMERA;

	/// <summary>
	/// When the prefab of a user interface is instantiated with this component, it will 
	/// find the main camera and osc manager, and hook them into the missing parts of other
	/// components. 
	/// </summary>
	private void OnEnable()
	{
		Debug.Log("<color=bright_white><b>QExE: </b></color>Applying event camera and OSC manager hooks on User Interface: " + this.gameObject.name);

		UI_CANVAS = this.gameObject.GetComponent<Canvas>();
		UI_OSC = this.gameObject.GetComponent<OSCPacket>();

		try
		{
			VR_MAIN_CAMERA = GameObject.Find("Main Camera").GetComponent<Camera>();
			OSCMANAGER = GameObject.Find("OSCManager");

			CameraFound = true;
			OSCManagerFound = true;
		}
		catch (Exception e)
		{
			Debug.LogWarning("Could not find requires scene components: " + e);
		}

		UI_OSC.OSCManager = OSCMANAGER;
		UI_CANVAS.worldCamera = VR_MAIN_CAMERA;
	}

	private void OnDestroy()
	{

	}
}
