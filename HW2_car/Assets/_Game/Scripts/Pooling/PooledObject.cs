using UnityEngine;
namespace NeonHighway {
public class PooledObject : MonoBehaviour {
 public PoolManager Owner {get;set;} public int PoolId {get;set;}
 public virtual void OnRent(){gameObject.SetActive(true);}
 public virtual void OnReturn(){gameObject.SetActive(false);}
 public void ReturnToPool(){if(gameObject.activeSelf)Owner.Return(this);}
}
}