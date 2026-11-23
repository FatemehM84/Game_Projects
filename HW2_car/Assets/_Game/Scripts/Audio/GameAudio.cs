using UnityEngine;
namespace NeonHighway {
public sealed class GameAudio : MonoBehaviour {
 public AudioSource music,engine,sfx;public AudioClip impact,coin,fuel;
 void Update(){var gm=GameManager.Instance;if(gm==null)return;engine.volume=Mathf.Lerp(engine.volume,gm.IsPlaying?.19f:0,Time.unscaledDeltaTime*5);engine.pitch=Mathf.Lerp(.75f,1.75f,Mathf.InverseLerp(24,58,gm.Speed));music.volume=gm.State==GameState.Paused?.09f:.16f;}
 public void PlayImpact()=>sfx.PlayOneShot(impact,.6f);public void PlayCoin()=>sfx.PlayOneShot(coin,.35f);public void PlayFuel()=>sfx.PlayOneShot(fuel,.45f);
}
}