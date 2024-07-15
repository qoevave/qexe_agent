using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using extOSC;

public class QEXEObject : MonoBehaviour
{
    public int ObjectIndex;

    private Vector3 position;
    private Vector3 rotation;
    private bool initialized = false;

    private extOSC.OSCTransmitter _transmitter;


    // Start is called before the first frame update
    void Start()
    {
        
    }

	private void OnEnable()
	{
        position = this.gameObject.transform.position;
        rotation = this.gameObject.transform.eulerAngles;
    }

	// Update is called once per frame
	void Update()
    {
        if (!initialized)
            Initialization();
    }

	private void FixedUpdate()
    {
        if (position != this.gameObject.transform.position)
		{
            position = this.gameObject.transform.position;
            TrackedPosePos(position);
        }
    }


    private void Initialization()
	{
        _transmitter = GameObject.Find("OSCManager").GetComponent<OSCTransmitter>();
        TrackedPosePos(position);
        initialized = true;
	}

    private void TrackedPosePos(Vector3 pos)
    {
        //Debug.Log("sent");
        var message = new OSCMessage("/server/audio/object/position");
        message.AddValue(OSCValue.Int(ObjectIndex));
        message.AddValue(OSCValue.Float(pos.x));
        message.AddValue(OSCValue.Float(pos.y));
        message.AddValue(OSCValue.Float(pos.z));

        if (_transmitter)
        {
            if (_transmitter.isActiveAndEnabled)
                _transmitter.Send(message);
        }
    }
}
