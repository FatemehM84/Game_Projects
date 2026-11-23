using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using UnityEngine.Rendering;

namespace NeonHighway {
/// <summary>Opt-in verification for the actual Windows player. Normal runs never create this object.</summary>
public sealed class StandaloneVerification : MonoBehaviour {
 readonly List<string> checks=new();int failures,errors;Keyboard keyboard;GameManager game;bool endurance;
 static string Output=>Path.Combine(Application.dataPath,"..","..","..","Documentation");
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Bootstrap(){if(Application.isEditor)return;var args=Environment.GetCommandLineArgs();if(args.Contains("-highway-smoke-test")||args.Contains("-highway-check-persistence")){var go=new GameObject("Standalone Verification");DontDestroyOnLoad(go);go.AddComponent<StandaloneVerification>();}}
 void Check(string message,bool pass){checks.Add((pass?"PASS ":"FAIL ")+message);if(!pass)failures++;Debug.Log(checks.Last());}
 void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception)errors++;}
 Button FindButton(string name)=>game.ui.GetComponentsInChildren<Button>(true).First(b=>b.gameObject.name==name);
 void Press(Key key){InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));InputSystem.Update();}
 void Release(){InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();}
 void Update(){if(endurance&&game!=null){foreach(var item in FindObjectsByType<Interactable>())item.ReturnToPool();if(game.Fuel<70)game.CollectFuel(game.player.transform.position);}}
 IEnumerator Start(){InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;Application.logMessageReceived+=Log;Directory.CreateDirectory(Output);yield return new WaitForSecondsRealtime(1);game=GameManager.Instance;
 if(Environment.GetCommandLineArgs().Contains("-highway-check-persistence")){int expected=int.Parse(File.ReadAllText(Path.Combine(Output,"persistence-expected.txt")));Check("High score survives closing and relaunching Windows executable",game.HighScore>=expected&&expected>0);Finish("player-persistence-tests.txt");yield break;}
 Check("Windows executable starts in Main Menu",game!=null&&game.State==GameState.Menu&&game.ui.menu.activeSelf);
 FindButton("START RUN").onClick.Invoke();yield return new WaitForSeconds(1);game=GameManager.Instance;Check("Start button loads Gameplay with three lives",game.IsPlaying&&game.Lives==3);
 keyboard=InputSystem.AddDevice<Keyboard>("StandaloneTestKeyboard");Press(Key.A);yield return new WaitForSeconds(.1f);Release();yield return new WaitForSeconds(1);Check("New Input System A moves to left lane x="+game.player.transform.position.x,game.player.transform.position.x<-3.4f);
 Press(Key.D);yield return new WaitForSeconds(.1f);Release();yield return new WaitForSeconds(.1f);Press(Key.D);yield return new WaitForSeconds(.1f);Release();yield return new WaitForSeconds(1);Check("New Input System D moves to right lane x="+game.player.transform.position.x,game.player.transform.position.x>3.4f);
 Press(Key.Escape);yield return null;Release();yield return null;float fuel=game.Fuel,elapsed=game.Elapsed;int score=game.Score;yield return new WaitForSecondsRealtime(.5f);Check("Escape pauses and freezes fuel, time and score",game.State==GameState.Paused&&game.Fuel==fuel&&game.Elapsed==elapsed&&game.Score==score);
 if(game.State==GameState.Paused)FindButton("RESUME").onClick.Invoke();yield return null;Check("Resume button resumes Windows player",game.IsPlaying);
 int coins=game.Coins;var coin=game.world.pools.Rent(3,game.player.transform.position+new Vector3(0,1,3));yield return new WaitForSeconds(.25f);Check("Coin trigger works in Windows player",game.Coins>coins&&!coin.gameObject.activeSelf);
 float before=game.Fuel;var can=game.world.pools.Rent(4,game.player.transform.position+new Vector3(0,1,3));yield return new WaitForSeconds(.25f);Check("Fuel trigger works in Windows player",game.Fuel>before&&!can.gameObject.activeSelf);
 int lives=game.Lives;game.world.pools.Rent(1,game.player.transform.position+new Vector3(0,0,3));yield return new WaitForSeconds(.3f);Check("Collision damage works in Windows player",game.Lives==lives-1);
 Directory.CreateDirectory(Path.Combine(Output,"Screenshots"));game.effects.impact.Clear();game.effects.pickup.Clear();game.ui.damageFlash.enabled=false;Capture(Path.Combine(Output,"Screenshots","windows-gameplay.png"));yield return new WaitForSeconds(.15f);game.ui.damageFlash.enabled=true;
 int count=game.world.pools.CreatedCount;endurance=true;Time.timeScale=6;yield return new WaitForSecondsRealtime(5);Time.timeScale=1;endurance=false;Check("Windows road recycling uses fixed pool count",game.world.RecycledSegments>5&&game.world.pools.CreatedCount==count);
 while(game.Lives>0){yield return new WaitForSeconds(1.8f);game.Damage();}
 Check("Windows GameOver interface opens",game.State==GameState.GameOver&&game.ui.gameOver.activeSelf);File.WriteAllText(Path.Combine(Output,"persistence-expected.txt"),game.HighScore.ToString());Check("High score is saved to PlayerPrefs",game.HighScore>=game.Score&&game.HighScore>0);
 FindButton("RUN AGAIN").onClick.Invoke();yield return new WaitForSeconds(1);game=GameManager.Instance;Check("Windows restart resets session",game.IsPlaying&&game.Lives==3&&game.Coins==0&&game.Elapsed<2);
 Check("Windows player has no errors or exceptions",errors==0);Finish("player-smoke-tests.txt");
 }
 void Capture(string path){var canvas=game.ui.GetComponent<Canvas>();var camera=game.player.followCamera;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;Canvas.ForceUpdateCanvases();var target=new RenderTexture(1600,900,24,RenderTextureFormat.ARGB32);target.Create();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});var previous=RenderTexture.active;RenderTexture.active=target;var texture=new Texture2D(1600,900,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1600,900),0,0);texture.Apply();File.WriteAllBytes(Path.GetFullPath(path),texture.EncodeToPNG());RenderTexture.active=previous;canvas.renderMode=RenderMode.ScreenSpaceOverlay;Destroy(texture);target.Release();Destroy(target);}
 void Finish(string name){ReleaseIfNeeded();Application.logMessageReceived-=Log;checks.Add("Checks: "+checks.Count);checks.Add("Failures: "+failures);checks.Add("Errors: "+errors);checks.Add("Unity: "+Application.unityVersion);checks.Add("UTC: "+DateTime.UtcNow.ToString("O"));File.WriteAllLines(Path.Combine(Output,name),checks);Application.Quit(failures==0?0:1);}
 void ReleaseIfNeeded(){if(keyboard!=null){Release();InputSystem.RemoveDevice(keyboard);}}
}
}
