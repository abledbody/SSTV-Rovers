using System.Collections.Generic;

using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class RandomSoundPlayer : MonoBehaviour {
    private AudioSource source;

    public AudioClip[] clips;
    public float minDelay = 0f;
    public float maxDelay = 1f;
    public int redundancyAvoidance = 2;

    private int[] lastPlayedClips;
    private int lastPlayedIndex = 0;

    void Awake() {
        source = GetComponent<AudioSource>();
        Invoke("PlayRandomSound", Random.Range(minDelay, maxDelay));
        lastPlayedClips = new int[redundancyAvoidance];
    }

    private void PlayRandomSound() {
        int index = Random.Range(0, clips.Length - redundancyAvoidance);
        int outIndex = index;
        // Reinterpret the index to account for redundancyAvoidance
        for (int i = 0; i < redundancyAvoidance; i++) {
            if (index >= lastPlayedClips[i])
                outIndex++;
        }

        source.PlayOneShot(clips[outIndex]);
        lastPlayedClips[lastPlayedIndex] = outIndex;
        lastPlayedIndex = (lastPlayedIndex + 1) % redundancyAvoidance;
        
        Invoke("PlayRandomSound", Random.Range(minDelay, maxDelay));
    }
}