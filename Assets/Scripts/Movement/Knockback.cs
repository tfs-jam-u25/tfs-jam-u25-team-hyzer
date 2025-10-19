using UnityEngine;

public class Knockback : MonoBehaviour
{
    //Currently this could be used to apply a knockback to 'any object' whether it's the one hitting calling it or the one getting hit calling it
    //Should determine what the flow of our communication and execution of effects will be in the near future
    private float counter = 0f;
    public float effectDuration = 0f; //need a list or something else if we want this to do multiple effects to a single entity at a time
    private bool effectActive = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void FixedUpdate()
    {
    }

    public void Apply(Rigidbody2D affectedObj, Vector2 force, ForceMode2D mode = ForceMode2D.Impulse)
    {
        affectedObj.AddForce(force, mode);
    }

    //consider an applyImmediate and applyOverTime function - or a parameter in apply to specify which
}