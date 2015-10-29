using UnityEngine;
using System.Collections;

public class GloveScript : MonoBehaviour {
	public GameObject hitPoof;
	private CharacterBehaviors characterBehvaiors;

	void Start () {
		characterBehvaiors = gameObject.GetComponentInParent<CharacterBehaviors>();
	}
	
	private void OnTriggerEnter(Collider hit) {
		if (null == FisticuffsController.Instance) {
			Debug.LogError("GloveScript::OnTriggerEnter - FisticuffsController.Instance not set. Is there one in the scene?");
			return;
		}

		if (hit.gameObject.tag == "Floor" && characterBehvaiors.punchPhase > 0) {
			FisticuffsController.Instance.oneShotAudio.PlayOneShot(FisticuffsController.Instance.punchMiss);
			FinishPunch(hit.gameObject, false);
		} else if (hit.gameObject.tag == "Character" && characterBehvaiors.punchPhase > 0) {
			// Do not hit myself.
			if (hit.gameObject != characterBehvaiors.gameObject) {
				FisticuffsController.Instance.oneShotAudio.PlayOneShot(FisticuffsController.Instance.punchHit);
				FinishPunch(hit.gameObject, true);
			}
		} else if (hit.gameObject.tag == "Target" && characterBehvaiors.punchPhase > 0) {
			FisticuffsController.Instance.oneShotAudio.PlayOneShot(FisticuffsController.Instance.punchMiss);
			FinishPunch(hit.gameObject, false);
		}
	}
	
	private void FinishPunch(GameObject otherCharacter, bool hit) {
		characterBehvaiors.punchPhase = 2;
		if (hit == true) {
			GameObject poof = Instantiate(hitPoof, transform.position, Quaternion.identity) as GameObject;
			characterBehvaiors.CalculateDamageToOpponent();
		}
		if (characterBehvaiors.myTempTarget != null) {
			Destroy(characterBehvaiors.myTempTarget);
		}
	}
}
