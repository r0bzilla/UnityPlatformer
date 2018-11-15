using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionSpawner : MonoBehaviour
{
    public ParticleSystem particles;
    public Collider2D knockbackArea;
    public float delay = 5f;
    public float knockbackStrength = 20;

    private float lastExplosionTimestamp;

    // Use this for initialization
    void Start()
    {
        this.lastExplosionTimestamp = Time.realtimeSinceStartup;
        this.knockbackArea.enabled = false;
    }

    // Update is called once per frame
    protected void FixedUpdate()
    {
        if (knockbackArea.enabled && Time.realtimeSinceStartup - lastExplosionTimestamp >= 0.15)
            this.knockbackArea.enabled = false;

        if (Time.realtimeSinceStartup - lastExplosionTimestamp >= delay)
            Explode();
    }

    protected void OnTriggerEnter2D(Collider2D other)
    {
        GameObject collidingObject = other.gameObject;
        Rigidbody2D collidingBody = collidingObject.GetComponentInParent<Rigidbody2D>();
        if (collidingBody)
        {
            Vector3 explosionForce = knockbackStrength * Vector3.Normalize(collidingBody.transform.position - transform.position);
            collidingBody.AddForce(explosionForce);
        }

    }

    private void Explode()
    {
        lastExplosionTimestamp = Time.realtimeSinceStartup;
        particles.Play();
        knockbackArea.enabled = true;
    }
}
