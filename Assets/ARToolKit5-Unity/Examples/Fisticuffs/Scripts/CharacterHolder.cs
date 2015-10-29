using UnityEngine;
using System.Collections;

public class CharacterHolder : MonoBehaviour {
	void OnEnable()	{
		if (null == FisticuffsController.Instance) {
			Debug.LogError("CharacterHolder::OnEnable - FisticuffsController.Instance not set. Is there one in the scene?");
			return;
		}

		if (FisticuffsController.Instance.cardsInPlay.Count < FisticuffsController.Instance.maxNumberOfCardsInPlay
		    && !FisticuffsController.Instance.cardsInPlay.Contains(gameObject)) {
			FisticuffsController.Instance.cardsInPlay.Add(gameObject);
		}
	}

	void OnDisable() {
		if (null == FisticuffsController.Instance) {
			Debug.LogError("CharacterHolder::OnDisable - FisticuffsController.Instance not set. Is there one in the scene?");
			return;
		}

		FisticuffsController.Instance.cardsInPlay.Remove(gameObject);
	}
}
