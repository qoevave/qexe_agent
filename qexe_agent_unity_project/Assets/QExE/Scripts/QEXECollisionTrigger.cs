using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QEXECollisionTrigger : MonoBehaviour
{
    private QEXEObjectTrigger thisQEXEObjectTrigger;
	private void OnEnable()
	{
        thisQEXEObjectTrigger = this.gameObject.GetComponent<QEXEObjectTrigger>();
    }
	// Start is called before the first frame update
	void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //Detect collisions between the GameObjects with Colliders attached
    void OnCollisionEnter(Collision collision)
    {
        //Check for a match with the specified name on any GameObject that collides with your GameObject
        if (collision.gameObject.name == "BrickSurface")
        {
            //If the GameObject's name matches the one you suggest, output this message in the console
            Debug.Log("Brick collision");
            thisQEXEObjectTrigger.TriggerAudio(5, "triggerPlay");
        }

        //Check for a match with the specific tag on any GameObject that collides with your GameObject
        if (collision.gameObject.name == "MetalSurface")
        {
            //If the GameObject has the same tag as specified, output this message in the console
            Debug.Log("Metal collision");
            thisQEXEObjectTrigger.TriggerAudio(6, "triggerPlay");
        }
    }
}
