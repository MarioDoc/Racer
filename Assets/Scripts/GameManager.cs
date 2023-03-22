using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoSingleton<GameManager>
{
    [Header("Racers Config")]
    [SerializeField]
    private int initialNumberOfRacers = 100;
    [Tooltip("Time in seconds that takes to update each racer")]
    [SerializeField]
    private float racersUpdateTime = 5;
    [SerializeField]
    private List<Racer> racerPrefabList;
    private List<Racer> racerList = new List<Racer>();
    [Tooltip("Sphere radius of spawn volume")]
    [SerializeField]
    private int radiusSpawnZone = 100;
    private Vector3 spawnPoint = Vector3.zero;
    [SerializeField]
    private Transform spawnedObjectsParent;
    //To check performance of update funtions
    private float updateComputingTime = 0f;
    private int collisionCheckCount = 0;


    [Space]
    [Header("Buttons")]
    [SerializeField]
    private Button oldUpdateButton;
    [SerializeField]
    private Button optimizedUpdateButton;
    [SerializeField]
    private Button resetButton;
    [SerializeField]
    private Button moveMainGameObject;

    [Space]
    [Header("MainGameObject")]
    [SerializeField]
    private MainObject mainObjectPrefab;
    private MainObject mainObject;
    private Collider mainCollider;
    [SerializeField]
    [Range(1f, 100f)]
    private float mainObjectSpeed;
    [SerializeField]
    private List<Transform> movementPoints = new List<Transform>();
    private Rigidbody mainObjectRigidBody;

    [Space]
    [Header("Collisions")]
    [SerializeField]
    private CollisionEventChannel collisionEventChannel;
    public CollisionEventChannel CollisionChannel { get => collisionEventChannel; private set => collisionEventChannel = value;   }
    [SerializeField]
    private ParticleSystem explosionParticleSystemPrefab;
    private ParticleSystem explosionParticleSystem;
    private bool collisionDetected;

    private AudioSource audioSource;
    private Coroutine movementCoroutine;
    private PoolSystem<Racer> poolingSystem = new();

    private void Awake()
    {
        audioSource = Camera.main.GetComponent<AudioSource>();
        collisionEventChannel.onCollisionEnterEvent += RefreshCollisionState;
    }

    private void OnDestroy()
    {
        collisionEventChannel.onCollisionEnterEvent -= RefreshCollisionState;
    }
    private void Start()
    {

        InitMainObject();
        InitRacers();

        oldUpdateButton.onClick.AddListener(() => UpdateRacers(racersUpdateTime, racerList));
        optimizedUpdateButton.onClick.AddListener(() => OptimizedUpdateRacers(racersUpdateTime,ref racerList));
        moveMainGameObject.onClick.AddListener(() => movementCoroutine = StartCoroutine(MoveMainObject(movementPoints)));
        resetButton.onClick.AddListener(Reset);
    }

    private void RefreshCollisionState(Collider collider)
    {
        if (collider)
        {
            
            collisionDetected = true;
            //To reduce physics callbacks
            mainCollider.enabled = false;
        }
    }
    public void OnRacerExplodes(Racer racer)
    {
        Debug.Log("Car explodes");
    }

    private void Reset()
    {
        InitMainObject();
        InitRacers();
    }

    private void InitMainObject()
    {
        if (!mainObject)
        {
            mainObject = Instantiate(mainObjectPrefab);
            mainObjectRigidBody = mainObject.GetComponent<Rigidbody>();
            mainCollider = mainObject.GetComponent<Collider>();
            collisionDetected = false;
            FindObjectOfType<CinemachineVirtualCamera>().LookAt = mainObject.transform;
        }

        if (!explosionParticleSystem)
        {
            explosionParticleSystem = Instantiate(explosionParticleSystemPrefab);
        }
        explosionParticleSystem.Stop();
        
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
        }
        mainCollider.enabled = true;
        mainObjectRigidBody.position = Vector3.zero;
    }
    private void InitRacers()
    {
        poolingSystem.ClearItems(racerList);
        racerList.Clear();
        for (int i = 0; i < initialNumberOfRacers; i++)
        {
            spawnPoint = Random.insideUnitSphere * radiusSpawnZone;
            Racer racer = poolingSystem.PoolAddElement(racerPrefabList[Random.Range(0, racerPrefabList.Count)], spawnPoint, Quaternion.Euler(-90, 90, 180), spawnedObjectsParent);
            racerList.Add(racer);
        }
    }

    private IEnumerator MoveMainObject(List<Transform> destination)
    {
        if (movementCoroutine != null)
        {
            InitMainObject();
        }

        for (int i = 0; i < destination.Count; i++)
        {
            Vector3 initialPosition = mainObjectRigidBody.position;
            Vector3 lastPosition = initialPosition;
            Vector3 direction = destination[i].position - initialPosition;
            float maxDistance = direction.magnitude;
            float distanceTravelled = 0;

            while (distanceTravelled < maxDistance)
            {
                lastPosition += mainObjectSpeed * Time.deltaTime * direction.normalized;
                distanceTravelled = (lastPosition - initialPosition).magnitude;

                if (distanceTravelled < maxDistance)
                {
                    mainObjectRigidBody.MovePosition(lastPosition);
                }
                else
                {
                    mainObjectRigidBody.MovePosition(destination[i].position);
                    break;
                }
                yield return null;
            }
        }
        //TODO: Add event for extended behaivour
        OnMovementFinished();
    }

    private void OnMovementFinished()
    {
        if (collisionDetected)
        {
            explosionParticleSystem.transform.position = mainObjectRigidBody.position;
            explosionParticleSystem.Play();
            audioSource.Play();
            Destroy(mainObject.gameObject);
        }
    }

    void OptimizedUpdateRacers(float deltaTimeS, ref List<Racer> racers)
    {
        // Gets the racers that are still alive. Initialize by 
        List<Racer> newRacerList = new(racers);
        List<Racer> racersNeedingRemoved = new List<Racer>();

        // Updates the racers that are alive
        for (int racerIndex = 0; racerIndex < racers.Count; racerIndex++)
        {
            if (racers[racerIndex].IsAlive())
            {
                //Racer update takes milliseconds
                racers[racerIndex].UpdateRacer(deltaTimeS * 1000.0f);
            }
        }

        // Collides
        //FIX: we only have to check the collision once between the same 2 racers
        //the loops has less iterations
        for (int racerIndex1 = 0; racerIndex1 < racers.Count; racerIndex1++)
        {
            Racer racer1 = racers[racerIndex1];

            for (int racerIndex2 = racerIndex1 + 1; racerIndex2 < racers.Count; racerIndex2++)
            {
                Racer racer2 = racers[racerIndex2];

                if (racer1.IsCollidable() && racer2.IsCollidable() && racer1.CollidesWith(racer2))
                {
                    OnRacerExplodes(racer1);
                    racersNeedingRemoved.Add(racer1);
                    racersNeedingRemoved.Add(racer2);
                    newRacerList.Remove(racer1);
                    newRacerList.Remove(racer2);
                }

            }
        }

        // Get rid of all the exploded racers
        for (int racerIndex = 0; racerIndex < racersNeedingRemoved.Count; racerIndex++)
        {
            racersNeedingRemoved[racerIndex].Destroy();
        }

        //poolingSystem.ClearItems(racersNeedingRemoved);

        racers = newRacerList;
        
    }


    void UpdateRacers(float deltaTimeS, List<Racer> racers)
    {
        updateComputingTime = 0;
        Debug.Log("Active racers BEFORE UPDATE" + racers.Count);
        List<Racer> racersNeedingRemoved = new List<Racer>();
        racersNeedingRemoved.Clear();

        // Updates the racers that are alive
        int racerIndex = 0;
        for (racerIndex = 1; racerIndex <= 1000; racerIndex++)
        {
            if (racerIndex <= racers.Count)
            {
                if (racers[racerIndex - 1].IsAlive())
                {
                    //Racer update takes milliseconds
                    racers[racerIndex - 1].UpdateRacer(deltaTimeS * 1000.0f);
                    updateComputingTime += deltaTimeS;
                }
            }
        }
        // Collides
        //Fix. Continues checking afterIs collide was checked in the second car
        for (int racerIndex1 = 0; racerIndex1 < racers.Count; racerIndex1++)
        {
            for (int racerIndex2 = 0; racerIndex2 < racers.Count; racerIndex2++)
            {
                Racer racer1 = racers[racerIndex1];
                Racer racer2 = racers[racerIndex2];
                if (racerIndex1 != racerIndex2)
                {
                    if (racer1.IsCollidable() && racer2.IsCollidable() && racer1.CollidesWith(racer2))
                    {
                        OnRacerExplodes(racer1);
                        racersNeedingRemoved.Add(racer1);
                        racersNeedingRemoved.Add(racer2);
                    }
                }
            }
        }
        // Gets the racers that are still alive
        List<Racer> newRacerList = new List<Racer>();
        for (racerIndex = 0; racerIndex != racers.Count; racerIndex++)
        {
            // check if this racer must be removed
            if (racersNeedingRemoved.IndexOf(racers[racerIndex]) < 0)
            {
                newRacerList.Add(racers[racerIndex]);
            }
        }
        // Get rid of all the exploded racers
        for (racerIndex = 0; racerIndex != racersNeedingRemoved.Count; racerIndex++)
        {
            int foundRacerIndex = racers.IndexOf(racersNeedingRemoved[racerIndex]);
            if (foundRacerIndex >= 0) // Check we've not removed this already!
            {
                collisionCheckCount++;
                racersNeedingRemoved[racerIndex].Destroy();
                racers.Remove(racersNeedingRemoved[racerIndex]);
            }
        }
        // Builds the list of remaining racers
        racers.Clear();
        for (racerIndex = 0; racerIndex < newRacerList.Count; racerIndex++)
        {
            racers.Add(newRacerList[racerIndex]);
        }

        for (racerIndex = 0; racerIndex < newRacerList.Count; racerIndex++)
        {
            newRacerList.RemoveAt(0);
        }

        racerList = racers;
        Debug.Log("Collision Check Count " + collisionCheckCount);
        collisionCheckCount = 0;
        Debug.Log("Active racers AFTER UPDATE" + racers.Count);
        Debug.Log("Computing time " + updateComputingTime);
    }
}
