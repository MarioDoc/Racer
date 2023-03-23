using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Racer : MonoBehaviour,IResettable
{
    private bool isAlive = true;
    [SerializeField]
    [Range(0f, 1f)]
    private Collider collider;
    [SerializeField]
    private List<Racer> collisionList = new List<Racer>();

    private void OnTriggerEnter(Collider other)
    {
        collisionList.Add(other.GetComponent<Racer>());   
    }

    private void Start()
    {
        collider = GetComponent<Collider>();
    }
    public bool activeSelf { get { return gameObject.activeSelf; } set { } }

    public virtual void Explode()
    {
        //This method could be overriden in case we want different explosions for different racers
        print("base explode logic");
    }

    public void SetActive(bool value)
    {
        gameObject.SetActive(value);
    }

    public bool IsAlive()
    {
        return isAlive;
    }


    public void UpdateRacer(float time)
    {
        collisionList.Clear();
        transform.position = Random.insideUnitSphere * GameManager.Get.radiusSpawnZone;
        //Update Logic
    }

    public bool IsCollidable()
    {
        return (collider != null);
    }

    public void Destroy()
    {
        //Destroy if you are not using pooling system
        isAlive = false;
        //Destroy(gameObject);     
        Debug.Log("Destroy racer");
    }

    public bool CollidesWith(Racer racer)
    {
        if (collisionList.Contains(racer))
        {
            return true;
        }
        else
        {
            return false;
        }

    }
}