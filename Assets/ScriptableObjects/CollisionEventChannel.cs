using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Events/Collision Event Channel")]
public class CollisionEventChannel : ScriptableObject
{
    //Here we the event modifier to force the action invocation through the RaiseCollisionEvent.
    //This way we can enssure this is not null i.e.
    public event Action<Collider> onCollisionEnterEvent;

    public void RaiseCollisionEvent(Collider collider)
    {
        onCollisionEnterEvent?.Invoke(collider);
    }
}
