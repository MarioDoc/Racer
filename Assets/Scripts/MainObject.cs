using UnityEngine;
[RequireComponent(typeof(Rigidbody))]
public class MainObject : MonoBehaviour
{
   private CollisionEventChannel collisionEventChannel;

    private void Awake()
    {
        collisionEventChannel = GameManager.Get.CollisionChannel;
    }
    private void OnTriggerEnter(Collider other)
    {
        collisionEventChannel.RaiseCollisionEvent(other);

        Debug.Log("collision!");
    }

}
