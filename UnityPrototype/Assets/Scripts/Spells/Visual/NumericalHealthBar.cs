using UnityEngine;
using System.Collections;

public class NumericalHealthBar : MonoBehaviour {

	public Damageable damageable;
	public TextMesh text; 

	// Use this for initialization
	void Start () {

	}
	
	// Update is called once per frame
	void Update () {
		text.text = ((int)damageable.CurrentHealth).ToString();
	}
}
