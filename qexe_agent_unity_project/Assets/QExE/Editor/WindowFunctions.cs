using UnityEditor;
using UnityEngine;
using System.Collections;

using System;
using System.Reflection;

using extOSC;

// Shows the Assets menu when you right click on the contextRect Rectangle.
public class WindowFunctions : MonoBehaviour
{
	[MenuItem("GameObject/QExE/Import QExE Rig", false, priority = 1)]
	static void ImportObject()
	{
		GameObject _myRig;

		GameObject _oscManager;

		TrackedPoseOSC _oscHeadTracker;

		OSCTransmitter _oscTransmitter;

		_myRig = Resources.Load("QExE XR Set Up") as GameObject;
		Instantiate(_myRig, new Vector3(0, 0, 0), Quaternion.identity);

		_oscManager = Resources.Load("OSCManager") as GameObject;
		Instantiate(_oscManager, new Vector3(0, 0, 0), Quaternion.identity);

		_oscHeadTracker = _myRig.GetComponentInChildren<TrackedPoseOSC>();
		_oscTransmitter = _oscManager.GetComponent<OSCTransmitter>();

		_oscHeadTracker._transmitter = _oscTransmitter;
	}
}