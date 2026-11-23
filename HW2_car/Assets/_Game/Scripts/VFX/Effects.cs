using UnityEngine;
namespace NeonHighway {
public sealed class Effects : MonoBehaviour {
 public ParticleSystem impact,pickup,firework,rocket;float nextFirework=3,explodeAt=-1;Vector3 explosionPosition;
 public void Impact(Vector3 p){impact.transform.position=p+Vector3.up;impact.Emit(45);}
 public void Pickup(Vector3 p,Color c){pickup.transform.position=p;var main=pickup.main;main.startColor=c*2;pickup.Emit(20);}
 void Update(){var gm=GameManager.Instance;if(gm==null||gm.State==GameState.Paused||gm.State==GameState.GameOver)return;
 if(Time.time>nextFirework){nextFirework=Time.time+Random.Range(4f,7f);explosionPosition=gm.player.transform.position+new Vector3(Random.Range(-35,35),Random.Range(26,43),Random.Range(95,150));rocket.transform.position=explosionPosition-Vector3.up*22;rocket.Emit(1);explodeAt=Time.time+1.1f;}
 if(explodeAt>0&&Time.time>explodeAt){explodeAt=-1;firework.transform.position=explosionPosition;var main=firework.main;main.startColor=Color.Lerp(new Color(1,.31f,.07f),new Color(1,.82f,.37f),Random.value)*3;firework.Emit(100);}
 }
}
}