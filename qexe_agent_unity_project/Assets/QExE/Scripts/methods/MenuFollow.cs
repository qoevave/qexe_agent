using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

public class MenuFollow : MonoBehaviour
{

	#region Public Variables

	[Range(0.5f, 1.5f)]
	public float DistanceScale = 1.2f;

	public GameObject TestInterface;

    public TestManager testManager;

    public GameObject LeftController;

    public GameObject RightController;

	public GameObject DevObject;

	#endregion Public Variables

	#region Private Vars

	private ActionBasedControllerManager LeftActionBasedControllerManager;

	private ActionBasedControllerManager RightActionBasedControllerManager;

	#endregion Private vars


	private void Awake()
	{
        TestInterface = GameObject.Find("QExETestInterface");
		try
		{
			testManager = GameObject.Find("TestManager").GetComponent<TestManager>();
		}
		catch
		{
			Debug.LogWarning("testManager.cs not found in scene.");
		}
        
		LeftActionBasedControllerManager = LeftController.GetComponent<ActionBasedControllerManager>();
		RightActionBasedControllerManager = RightController.GetComponent<ActionBasedControllerManager>();
	}

	void Start()
    {
		if (LeftActionBasedControllerManager == null)
			Debug.LogWarning("QExE: Left action based controller not found");

		if (RightActionBasedControllerManager == null)
			Debug.LogWarning("QExE: Right action based controller not found");
	}

	/// <summary>
	/// Turns the Interface on and off at the correct position depending on the controller states. 
	/// 1. If a next button has been found, if means a menu has been loaded.
	/// 2. If the controller state in the ActionBasedControllerManager for either left or right controller is in 'interfacing' state, turn the UI on or off.
	/// 3. If the UI if off, update the position of the TestInterface game object. 
	/// </summary>
	void Update()
	{
		if (testManager != null && testManager.InterfaceNextButtonFound)
		{
			if (LeftActionBasedControllerManager.m_Interfacing == true | RightActionBasedControllerManager.m_Interfacing == true)
			{
				TestInterface.SetActive(true);
			}
			else
			{
				TestInterface.SetActive(false);
			}
		}

		if (!TestInterface.activeSelf)
		{
			TestInterface.transform.position = this.gameObject.GetComponent<Camera>().transform.position + (this.gameObject.GetComponent<Camera>().transform.forward * DistanceScale);
			TestInterface.transform.rotation = Quaternion.Euler(+15, this.gameObject.transform.rotation.eulerAngles.y, 0);
			TestInterface.transform.position = new Vector3(TestInterface.transform.position.x, Mathf.Clamp(TestInterface.transform.position.y - 1, this.gameObject.transform.position.y, this.gameObject.transform.position.y + 5) - 0.5f, TestInterface.transform.position.z);
		}

		if (DevObject != null)
		{
			if (LeftActionBasedControllerManager.m_Interfacing == true | RightActionBasedControllerManager.m_Interfacing == true)
			{
				DevObject.SetActive(true);
			}
			else
			{
				DevObject.SetActive(false);
			}
		}
	}
}
