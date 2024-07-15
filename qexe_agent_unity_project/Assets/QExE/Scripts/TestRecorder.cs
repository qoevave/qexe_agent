using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using extOSC;
using System.IO;
using UnityEngine.SceneManagement;

public class TestRecorder : MonoBehaviour
{
    public bool recordTest = true;

    public bool resultsDirectoryReceived = false;

    public string resultsDirectory;

    public OSCReceiver _receiver;

    public OSCTransmitter _transmitter;

    private StreamWriter stream;

    private bool recordingStarted = false; 


    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }


    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }


    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindSceneObjects();
        _transmitter.CurrentScene = scene.name;

        if(scene.name != "_config" && resultsDirectoryReceived == false)
		{
            StartCoroutine(RequestSubjectsDirectory());
        }

        if(scene.name != "config" && resultsDirectoryReceived == true && recordingStarted == false)
		{
            _transmitter.RecordingStream = stream;
            _transmitter.RecordOSC = true;
        }
	}

    public void OnSceneUnloaded(Scene current)
    {
        recordingStarted = false; 
    }

    private void FindSceneObjects()
    {
        _receiver = GameObject.Find("OSCManager").GetComponent<OSCReceiver>();
        _transmitter = GameObject.Find("OSCManager").GetComponent<OSCTransmitter>();
        _receiver.Bind("/client/results/", ReceiveDirectory);   
    }


	IEnumerator RequestSubjectsDirectory()
	{
		yield return new WaitForSeconds(1);
        SendMessage("/control/", "TestManager get_results_directory");
        Debug.Log("<color=cyan><b>QExE:udp:out: </b></color> -->> Requesting subjects results directory.");
    }

    public void SendMessage(string address, string msg)
    {
        Send(address, OSCValue.String(msg));
    }

    private void Send(string address, OSCValue value)
    {
        var message = new OSCMessage(address, value);
        _transmitter.Send(message);
    }

    public void ReceiveDirectory(OSCMessage message)
    {
        if (message.ToString(out var value))
        {
            Debug.Log(value);
            resultsDirectory = value;

            string[] address = resultsDirectory.Split('/');
            string name = address[address.Length - 1].Substring(4);
            resultsDirectoryReceived = true;
            OpenStream(resultsDirectory, name);


        }
    }

    public void OpenStream(string path, string name)
    {
        stream = new StreamWriter(path + "/behaviour" + name + ".txt", false);
        _transmitter.RecordingStream = stream;
        _transmitter.RecordOSC = true;
    }

    private void OnApplicationQuit()
    {
        try
        {
            stream.Close();
        }
        catch
        {

        }
    }
}


