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

        private OSCTransmitter Transmitter;
        private OSCReceiver Receiver;

        // Start is called before the first frame update
        void Start()
        {
            Transmitter = OSCManager.GetComponent<OSCTransmitter>();
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
    }
}
