using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Radio : MonoBehaviour
{
    public AudioClip[] Tracks;
    public AudioSource source;
    public bool isPaused;
    public int lastTrack;
    public float newClip;
    public GameObject musicButton;
    public Sprite Active;
    public Sprite Inactive;
    public GameObject IconLight;
    public GameObject IconDark;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        isPaused = true;
        StartCoroutine(StartWait());
    }

    // Update is called once per frame
    void Update()
    {
        if (!source.isPlaying && isPaused == false)
        {
            source.PlayOneShot(Tracks[Random.Range(0, 4)]);
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            if (isPaused == false)
            {
                isPaused = true;
                source.Stop();
                musicButton.GetComponent<Image>().sprite = Inactive;
                IconLight.SetActive(false);
                IconDark.SetActive(true);
            }
            else if (isPaused == true)
            {
                isPaused = false;
                musicButton.GetComponent<Image>().sprite = Active;
                IconLight.SetActive(true);
                IconDark.SetActive(false);
            }
        }

    }

    public IEnumerator StartWait()
    {
        yield return new WaitForSeconds(2f);
        source.PlayOneShot(Tracks[0]);
        isPaused = false;
    }


    public void newTrack()
    {

        int trackNumber = Random.Range(0, Tracks.Length);
        while (trackNumber == lastTrack)
        {
            trackNumber = Random.Range(0, Tracks.Length);
        }

        if (source.isPlaying)
        {
            source.Stop();
            source.loop = true;
            source.PlayOneShot(Tracks[trackNumber]);
        }
        else
        {
            source.Stop();
            source.loop = true;
            source.PlayOneShot(Tracks[trackNumber]);
        }

        newClip = Tracks[trackNumber].length;
        lastTrack = trackNumber;
    }



}
