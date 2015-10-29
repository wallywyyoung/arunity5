using UnityEngine;
using System.Collections;

public class AnimationBehaviors : MonoBehaviour {
	
	public AudioClip ding;
	public AudioClip beepCount;
	public AudioClip fanfare;
	
	void DeactivateSelf() {
		gameObject.SetActiveRecursively(false);
	}
	
	void PlaySound(string whichSound) {
		if (whichSound == "ding") {
			GetComponent<AudioSource>().PlayOneShot(ding);
		} else if (whichSound == "beepCount") {
			GetComponent<AudioSource>().PlayOneShot(beepCount);
		} else if (whichSound == "fanfare") {
			GetComponent<AudioSource>().PlayOneShot(fanfare);
		} else {
			Debug.LogWarning("AnimationBehaviors::PlaySound - Sound \"" + whichSound + "\" does not exist!");
		}
	}
	
	void PlayMainAudioLoop() {
		GetComponent<AudioSource>().Play();
	}
	
	void StopAudio() {
		GetComponent<AudioSource>().Stop();
	}
	
	void TriggerGameStart() {
		if (null == FisticuffsController.Instance) {
			Debug.LogError("AnimationBehviors::TriggerGameStart - FisticuffsController.Instance not set. Is there one in the scene?");
			return;
		}
		FisticuffsController.Instance.GameStart();
	}
	
}
