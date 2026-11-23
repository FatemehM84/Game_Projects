using System;using System.Collections.Generic;using UnityEngine;
namespace NeonHighway {
public sealed class PoolManager : MonoBehaviour {
 [Serializable] public class Entry {public PooledObject prefab;public int capacity=32;}
 public Entry[] entries; readonly List<Queue<PooledObject>> available=new(); readonly List<PooledObject> instances=new();
 public int CreatedCount=>instances.Count; public int RentCount {get;private set;} public int ReturnCount {get;private set;}
 public void Initialize(){if(available.Count>0)return;for(int i=0;i<entries.Length;i++){available.Add(new());for(int j=0;j<entries[i].capacity;j++){var obj=Instantiate(entries[i].prefab,transform);obj.Owner=this;obj.PoolId=i;obj.gameObject.SetActive(false);available[i].Enqueue(obj);instances.Add(obj);}}}
 public PooledObject Rent(int id,Vector3 position){if(id<0||id>=available.Count||available[id].Count==0){Debug.LogError("Pool exhausted: "+id);return null;}var obj=available[id].Dequeue();obj.transform.SetPositionAndRotation(position,Quaternion.identity);obj.OnRent();RentCount++;return obj;}
 public void Return(PooledObject obj){obj.OnReturn();available[obj.PoolId].Enqueue(obj);ReturnCount++;}
 public void ShiftOrigin(float z){foreach(var obj in instances)if(obj.gameObject.activeSelf)obj.transform.position-=Vector3.forward*z;}
}
}