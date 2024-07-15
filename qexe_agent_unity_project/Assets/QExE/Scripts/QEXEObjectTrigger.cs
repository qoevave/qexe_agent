using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using extOSC;

public class QEXEObjectTrigger : MonoBehaviour
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
        var message_position = new OSCMessage("/stream/objectspose/pos/");
        message_position.AddValue(OSCValue.Int(ObjectIndex));
        message_position.AddValue(OSCValue.Float(pos.x));
        message_position.AddValue(OSCValue.Float(pos.y));
        message_position.AddValue(OSCValue.Float(pos.z));

        var message_rotation = new OSCMessage("/stream/objectspose/rot/");
        message_rotation.AddValue(OSCValue.Int(ObjectIndex));
        message_rotation.AddValue(OSCValue.Float(rotation.x));
        message_rotation.AddValue(OSCValue.Float(rotation.y));
        message_rotation.AddValue(OSCValue.Float(rotation.z));

        if (_transmitter)
        {
            if (_transmitter.isActiveAndEnabled)
			{
                _transmitter.Send(message_position);
                _transmitter.Send(message_rotation);
            }
               
        }
    }

    public void TriggerAudio(int sampleIndex, string triggerMsg)
	{
        var message_trigger = new OSCMessage("/stream/objectstrigger/");
        message_trigger.AddValue(OSCValue.Int(sampleIndex+1));
        message_trigger.AddValue(OSCValue.String("event"));
        message_trigger.AddValue(OSCValue.String(triggerMsg));

        if (_transmitter)
        {
            if (_transmitter.isActiveAndEnabled)
                _transmitter.Send(message_trigger);
        }
    }
}
