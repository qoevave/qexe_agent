using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using extOSC;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;

namespace extOSC
{
    public class TrackedPoseOSC : MonoBehaviour
    {
        public Camera _camera;

        public OSCTransmitter _transmitter;

        // public InputActionReference _leftTeleportSelectReference = null;
        // public InputActionReference _rightTeleportSelectReference = null;

        public TeleportationProvider _teleportationProvider;

        public GameObject leftHand;
        public GameObject rightHand;

        private Vector3 _cameraPos;
        private Vector3 _cameraRot;
        private Vector3 _leftControllerPos;
        private Vector3 _leftControllerRot;
        private Vector3 _rightControllerPos;
        private Vector3 _rightControllerRot;

        private Vector3 leftPose;
        private Vector3 rightPose;

        // Start is called before the first frame update
        void Start()
        {

        }

		private void OnEnable()
		{
            //  _leftTeleportSelectReference.action.canceled += TeleportFlag;
            //  _rightTeleportSelectReference.action.canceled += TeleportFlag;
            leftPose = leftHand.transform.position;
            rightPose = rightHand.transform.position;
        }

        private void OnDisable()
		{
          //  _leftTeleportSelectReference.action.started -= TeleportFlag;
          //  _rightTeleportSelectReference.action.canceled -= TeleportFlag;
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            TrackedPoseRot();
            TrackedPosePos();

            if (leftPose != leftHand.transform.position)
            {
                leftPose = leftHand.transform.position;
                LeftControllerPose(leftHand.transform.position, leftHand.transform.eulerAngles);
            }
            if (rightPose != rightHand.transform.position)
            {
                rightPose = rightHand.transform.position;
                RightControllerPose(rightHand.transform.position, rightHand.transform.eulerAngles);
            }
        }

		private void Update()
		{
            if (_teleportationProvider.locomotionPhase == LocomotionPhase.Done)
                TeleportFlag();
        }

		public void TeleportFlag()
        {
            Debug.Log("User Teleported");
            TrackedLocomotion();
        }


        private void TrackedPoseRot()
        {
            _cameraRot = _camera.transform.eulerAngles;
            //_cameraRot.Normalize();

            var message = new OSCMessage("/stream/userpose/rot");
            message.AddValue(OSCValue.Float(_cameraRot.x));
            message.AddValue(OSCValue.Float(_cameraRot.y));
            message.AddValue(OSCValue.Float(_cameraRot.z));

            _transmitter.Send(message);
        }

        private void TrackedPosePos()
		{
            _cameraPos = _camera.transform.position;

            var message = new OSCMessage("/stream/userpose/pos");
            message.AddValue(OSCValue.Float(_cameraPos.x));
            message.AddValue(OSCValue.Float(_cameraPos.y));
            message.AddValue(OSCValue.Float(_cameraPos.z));

            _transmitter.Send(message);
        }

        private void TrackedLocomotion()
        {
            var message = new OSCMessage("/stream/userpose/teleport");
            message.AddValue(OSCValue.Int(1));
            _transmitter.Send(message);
        }


        private void LeftControllerPose(Vector3 pos, Vector3 rot)
		{
            var controllers_pose_left = new OSCMessage("/stream/controllerpose/left");
            controllers_pose_left.AddValue(OSCValue.Float(pos.x));
            controllers_pose_left.AddValue(OSCValue.Float(pos.y));
            controllers_pose_left.AddValue(OSCValue.Float(pos.z));
            controllers_pose_left.AddValue(OSCValue.Float(rot.x));
            controllers_pose_left.AddValue(OSCValue.Float(rot.y));
            controllers_pose_left.AddValue(OSCValue.Float(rot.z));
            _transmitter.Send(controllers_pose_left);
        }

        private void RightControllerPose(Vector3 pos, Vector3 rot)
        {
            var controllers_pose_right = new OSCMessage("/stream/controllerpose/right");
            controllers_pose_right.AddValue(OSCValue.Float(pos.x));
            controllers_pose_right.AddValue(OSCValue.Float(pos.y));
            controllers_pose_right.AddValue(OSCValue.Float(pos.z));
            controllers_pose_right.AddValue(OSCValue.Float(rot.x));
            controllers_pose_right.AddValue(OSCValue.Float(rot.y));
            controllers_pose_right.AddValue(OSCValue.Float(rot.z));

            _transmitter.Send(controllers_pose_right);
        }
    }
}
