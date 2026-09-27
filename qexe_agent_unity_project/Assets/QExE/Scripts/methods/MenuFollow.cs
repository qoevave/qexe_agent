using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Shows the QExE test interface while the participant holds (or toggles) the interface button on
/// either controller, and keeps it parked in front of the camera while it is hidden.
///
/// Previously this read <c>m_Interfacing</c> from a hand-modified copy of the XRI 2.x Starter Assets
/// <c>ActionBasedControllerManager</c>. That sample was replaced by XRI 3.x
/// (<c>ControllerInputActionManager</c>), so the input handling now lives here and no longer depends
/// on sample code that is overwritten whenever the XR Interaction Toolkit samples are re-imported.
/// </summary>
public class MenuFollow : MonoBehaviour
{
	public enum ActivationMode
	{
		/// <summary>Interface is visible only while the button is held (previous behaviour).</summary>
		Hold,
		/// <summary>Each press toggles the interface on or off.</summary>
		Toggle
	}

	#region Public Variables

	[Range(0.5f, 1.5f)]
	public float DistanceScale = 1.2f;

	public GameObject TestInterface;

	public TestManager testManager;

	/// <summary>Optional debug object shown alongside the interface.</summary>
	public GameObject DevObject;

	[Header("Interface button")]
	public ActivationMode Mode = ActivationMode.Hold;

	[Tooltip("Button on the left controller that opens the test interface. Either assign an action from an " +
	         "Input Action Asset, or edit the bindings of the embedded action.")]
	public InputActionProperty LeftInterfaceAction = new InputActionProperty(
		CreateDefaultAction("QExE Left Interface", "LeftHand"));

	[Tooltip("Button on the right controller that opens the test interface. Either assign an action from an " +
	         "Input Action Asset, or edit the bindings of the embedded action.")]
	public InputActionProperty RightInterfaceAction = new InputActionProperty(
		CreateDefaultAction("QExE Right Interface", "RightHand"));

	/// <summary>True while the participant has the interface open.</summary>
	public bool IsInterfacing { get; private set; }

	#endregion Public Variables

	#region Private Vars

	private Camera _camera;

	private bool _toggleState;

	#endregion Private Vars

	private static InputAction CreateDefaultAction(string name, string hand)
	{
		var action = new InputAction(name, InputActionType.Button);
		// A/X on Quest-style controllers, menu button on Vive wands / Index.
		action.AddBinding($"<XRController>{{{hand}}}/primaryButton");
		action.AddBinding($"<XRController>{{{hand}}}/menuButton");
		return action;
	}

	private void Awake()
	{
		_camera = GetComponent<Camera>();

		if (TestInterface == null)
			TestInterface = GameObject.Find("QExETestInterface");

		if (testManager == null)
		{
			var tm = GameObject.Find("TestManager");
			if (tm != null)
				testManager = tm.GetComponent<TestManager>();
		}

		if (TestInterface == null)
			Debug.LogWarning("QExE: MenuFollow could not find 'QExETestInterface' in the scene.");

		if (testManager == null)
			Debug.LogWarning("QExE: MenuFollow could not find 'TestManager' in the scene.");
	}

	private void OnEnable()
	{
		EnableAction(LeftInterfaceAction);
		EnableAction(RightInterfaceAction);
	}

	private void OnDisable()
	{
		DisableAction(LeftInterfaceAction);
		DisableAction(RightInterfaceAction);
	}

	/// <summary>
	/// 1. Reads the interface button on either controller.
	/// 2. Once a method/questionnaire interface has been loaded (TestManager found its 'Next' button),
	///    shows or hides the interface accordingly.
	/// 3. While hidden, re-positions the interface in front of the camera so it opens where the user looks.
	/// </summary>
	private void Update()
	{
		IsInterfacing = ReadInterfaceInput();

		if (TestInterface == null)
			return;

		if (testManager != null && testManager.InterfaceNextButtonFound)
			TestInterface.SetActive(IsInterfacing);

		if (!TestInterface.activeSelf && _camera != null)
		{
			Transform cam = _camera.transform;
			Vector3 pos = cam.position + cam.forward * DistanceScale;
			pos.y = Mathf.Clamp(pos.y - 1f, cam.position.y, cam.position.y + 5f) - 0.5f;

			TestInterface.transform.SetPositionAndRotation(pos, Quaternion.Euler(15f, cam.rotation.eulerAngles.y, 0f));
		}

		if (DevObject != null)
			DevObject.SetActive(IsInterfacing);
	}

	private bool ReadInterfaceInput()
	{
		if (Mode == ActivationMode.Hold)
			return IsPressed(LeftInterfaceAction) || IsPressed(RightInterfaceAction);

		if (WasPressedThisFrame(LeftInterfaceAction) || WasPressedThisFrame(RightInterfaceAction))
			_toggleState = !_toggleState;

		return _toggleState;
	}

	private static bool IsPressed(InputActionProperty property)
	{
		var action = property.action;
		return action != null && action.IsPressed();
	}

	private static bool WasPressedThisFrame(InputActionProperty property)
	{
		var action = property.action;
		return action != null && action.WasPressedThisFrame();
	}

	private static void EnableAction(InputActionProperty property)
	{
		property.action?.Enable();
	}

	private static void DisableAction(InputActionProperty property)
	{
		// Only disable actions owned by this component. Actions referenced from a shared asset are
		// managed by XRI's InputActionManager and may be in use elsewhere.
		if (property.reference == null)
			property.action?.Disable();
	}
}
