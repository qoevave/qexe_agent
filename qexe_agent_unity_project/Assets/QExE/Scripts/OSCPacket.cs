using UnityEngine;
using UnityEngine.UI;

using System.Collections;
using System.Collections.Generic;

namespace extOSC
{
    public class OSCPacket : MonoBehaviour
    {
        [Header("OSC Settings")]

        public GameObject OSCManager;

        public GameObject ReaperOSCManager;         //declared new ReaperOSCManager

        private OSCTransmitter Transmitter;

        public OSCTransmitter ReaperOSCTransmitter;  //declared a new Reaper Transmitter

        private OSCReceiver Receiver;

        // Start is called before the first frame update
        void Start()
        {
            Transmitter = OSCManager.GetComponent<OSCTransmitter>();
            ReaperOSCTransmitter = ReaperOSCManager.GetComponent<OSCTransmitter>(); //From this Gameobject we get the Component of type OSCTransmitter and storing it into an instance called ReaperOSCTransmitter
            Receiver= OSCManager.GetComponent<OSCReceiver>();
        }


		public void SendButton(string address)
		{
			Send(address, OSCValue.String(address));
		}


        public void SendSlider (GameObject slider)
        {
            float val = slider.GetComponent<Slider>().value;
            Send(slider.name, OSCValue.Float(val));
        }


        public void SendMessage(string address, string msg)
		{
            Send(address, OSCValue.String(msg));
        }


        private void Send(string address, OSCValue value)
        {
            var message = new OSCMessage(address, value);

            Transmitter.Send(message);
            
        }


        public void SendToReaper(string address, int value)
        {
            var message = new OSCMessage(address);
            message.AddValue(OSCValue.Int(value));

            ReaperOSCTransmitter.Send(message);
            
        }
    
    }
}
