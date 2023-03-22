using System.Collections;
using UnityEngine;

public class Racer : MonoBehaviour,IResettable
{
    private bool isAlive = true;
    [SerializeField]
    [Range(0f, 1f)]
    private float collisionProbability = 0.01f;

    [SerializeField]
    private Collider collider;

    [SerializeField]
    bool updated = true;

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

    private IEnumerator WaitForUpdate(float time)
    {
        updated = false;
        yield return new WaitForSeconds(time / 1000);
        updated = true;
    } 

    public void UpdateRacer(float time)
    {
        if (!updated) return;
        StartCoroutine(WaitForUpdate(time));
    }

    public bool IsCollidable()
    {
        return (collider != null);
    }

    public void Destroy()
    {
        isAlive = false;
        Destroy(gameObject);
        Debug.Log("Destroy racer");
    }

    public bool CollidesWith(Racer racer)
    {
        return Random.Range(0f, 1f) < collisionProbability * racer.collisionProbability;     
    }
}