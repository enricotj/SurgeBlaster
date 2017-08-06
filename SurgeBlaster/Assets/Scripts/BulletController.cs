using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletController : MonoBehaviour {

    public float deathTimer = 1;

	// Use this for initialization
	void Start ()
    {
		
	}
	
	// Update is called once per frame
	void Update ()
    {
        deathTimer -= Time.deltaTime;
        if (deathTimer < 0)
        {
            Destroy(gameObject);
        }
	}
}
