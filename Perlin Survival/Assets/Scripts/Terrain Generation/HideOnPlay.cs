using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Hides an object when the game is played
 */
public class HideOnPlay : MonoBehaviour {
	void Start () {
        gameObject.SetActive(false);	
	}
}
