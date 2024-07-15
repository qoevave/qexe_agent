using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class VideoManager : MonoBehaviour
{
    public VideoPlayer videoPlayer;

	public TestManager manager;

	public string PathToFile;

	private bool simulatorNextCalled = false;

	private void Awake()
	{
		manager = GameObject.Find("TestManager").GetComponent<TestManager>();
	}

	void Start()
	{
		manager.StartPlaybackButton.onClick.AddListener(() => Play());
		manager.StartPlaybackButton.interactable = false;
		videoPlayer.loopPointReached += SendEndFlag;
		PathToFile = manager.ThisSceneVideoPath;
		if (PathToFile != null)
		{
			URLToVideo(PathToFile);
		}
	}

	public void Update()
	{
		if (manager.SimulatorNext == true && simulatorNextCalled == false)
		{
			Play();
			simulatorNextCalled = true;
		}
	}

	public void Play()
	{
		videoPlayer.Play();
	}

	public void SendEndFlag(UnityEngine.Video.VideoPlayer vp)
	{
		manager.SendMessage("/control/", "VideoTagEnd");
	}

	public void URLToVideo(string URL)
	{
		videoPlayer.source = VideoSource.Url;
		videoPlayer.url = URL;
		videoPlayer.Prepare();
		videoPlayer.prepareCompleted += ActivatePlayButton;

	}

	public void ActivatePlayButton(VideoPlayer source)
	{
		manager.StartPlaybackButton.interactable = true;
	}
}
