using UnityEngine;
namespace NeonHighway {
[RequireComponent(typeof(BoxCollider))]
public abstract class Interactable : PooledObject {
 protected float age; bool interacted;
 public override void OnRent(){age=0;interacted=false;transform.rotation=Quaternion.identity;GetComponent<Collider>().enabled=true;base.OnRent();}
 protected virtual void Update(){var gm=GameManager.Instance;if(gm==null||!gm.IsPlaying)return;age+=Time.deltaTime;Animate(Time.deltaTime);if(transform.position.z<gm.player.transform.position.z-22)ReturnToPool();}
 protected virtual void Animate(float dt){}
 void OnTriggerEnter(Collider other){var gm=GameManager.Instance;if(interacted||gm==null||!gm.IsPlaying||other.GetComponentInParent<PlayerCar>()==null)return;interacted=true;Interact(gm);ReturnToPool();}
 public abstract void Interact(GameManager game);
}
}