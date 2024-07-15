using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using extOSC;

public class LatencyTesting : MonoBehaviour
{
    public extOSC.OSCTransmitter Transmitter;
    
    public extOSC.OSCReceiver Receiver;

    public bool measure = false;

    private DateTime _timeTag;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        _timeTag = DateTime.Now;
        SendTimeTag();
    }

    public void SendTimeTag()
    {
        Send("/host/latencyTest", OSCValue.TimeTag(_timeTag));
    }


    private void Send(string address, OSCValue value)
    {
        var message = new OSCMessage(address, value);
        Transmitter.Send(message);
    }
}
