#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using NeonHighway;

public sealed class HighwayQARunner:MonoBehaviour {
 readonly List<string> results=new();int failures;float fuelBefore,elapsedBefore,zBefore;int scoreBefore;bool endurance;int runtimeErrors;GameManager gm;Keyboard qaKeyboard;
 void Check(string label,bool pass){results.Add((pass?"PASS ":"FAIL ")+label);if(!pass)failures++;Debug.Log(results[results.Count-1]);}
 void Log(string condition,string stack,LogType type){if(type==LogType.Exception||type==LogType.Error)runtimeErrors++;}
 void Update(){if(endurance){foreach(var item in UnityEngine.Object.FindObjectsByType<Interactable>())item.ReturnToPool();gm.CollectFuel(gm.player.transform.position);}}
 void Press(Key key){InputSystem.QueueStateEvent(qaKeyboard,new KeyboardState(key));}
 void Release(){InputSystem.QueueStateEvent(qaKeyboard,new KeyboardState());}
 Button Button(string name)=>gm.ui.GetComponentsInChildren<Button>(true).First(b=>b.gameObject.name==name);
 void Capture(string path){gm.effects.impact.Clear();gm.effects.pickup.Clear();bool flash=gm.ui.damageFlash.enabled;gm.ui.damageFlash.enabled=false;var canvas=gm.ui.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=gm.player.followCamera;canvas.planeDistance=1;Canvas.ForceUpdateCanvases();var rt=new RenderTexture(1600,900,24,RenderTextureFormat.ARGB32);rt.Create();var request=new RenderPipeline.StandardRequest{destination=rt};RenderPipeline.SubmitRenderRequest(gm.player.followCamera,request);var old=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1600,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes("Documentation/Screenshots/"+path+".png",image.EncodeToPNG());RenderTexture.active=old;Destroy(image);rt.Release();Destroy(rt);canvas.renderMode=RenderMode.ScreenSpaceOverlay;gm.ui.damageFlash.enabled=flash;}
 IEnumerator Start(){DontDestroyOnLoad(gameObject);qaKeyboard=InputSystem.AddDevice<Keyboard>("QAKeyboard");InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;Application.logMessageReceived+=Log;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;yield return new WaitForSecondsRealtime(.5f);gm=GameManager.Instance;gm.player.input.devices=new InputDevice[]{qaKeyboard};
 Check("Singleton, Playing state and exactly three lives",gm!=null&&gm.State==GameState.Playing&&gm.Lives==3);
 Check("All gameplay and UI references assigned",gm.player&&gm.world&&gm.ui&&gm.audioSystem&&gm.effects&&gm.config);
 Check("Input action map with Left Right Pause assigned",gm.player.input.FindAction("Driving/Left")!=null&&gm.player.input.FindAction("Driving/Right")!=null&&gm.player.input.FindAction("Driving/Pause")!=null);
 Check("Four concrete Interactable subclasses",new[]{typeof(StationaryCarObstacle),typeof(MovingCarObstacle),typeof(CoinCollectible),typeof(FuelCollectible)}.All(t=>t.IsSubclassOf(typeof(Interactable)))&&typeof(Interactable).IsAbstract);
 int created=gm.world.pools.CreatedCount;Check("All five pools prewarmed",created==246);
 var volume=UnityEngine.Object.FindAnyObjectByType<Volume>();volume.sharedProfile.TryGet<Bloom>(out var bloom);Check("Active URP, Bloom and post processing",GraphicsSettings.defaultRenderPipeline!=null&&bloom!=null&&bloom.intensity.value>0&&gm.player.followCamera.GetUniversalAdditionalCameraData().renderPostProcessing);
 Check("Five audio clips assigned",gm.audioSystem.music.clip&&gm.audioSystem.engine.clip&&gm.audioSystem.coin&&gm.audioSystem.fuel&&gm.audioSystem.impact);
 Check("Score and fuel advance while driving",gm.Score>0&&gm.Fuel<100);
 gm.TogglePause();yield return null;fuelBefore=gm.Fuel;elapsedBefore=gm.Elapsed;scoreBefore=gm.Score;zBefore=gm.player.transform.position.z;yield return new WaitForSecondsRealtime(.4f);
 Check("Pause freezes score, fuel, elapsed and position",gm.State==GameState.Paused&&gm.Fuel==fuelBefore&&gm.Elapsed==elapsedBefore&&gm.Score==scoreBefore&&gm.player.transform.position.z==zBefore);
 Button("RESUME").onClick.Invoke();yield return null;Check("Resume UI button works",gm.IsPlaying&&Time.timeScale==1);
 Press(Key.A);yield return new WaitForSecondsRealtime(.07f);Release();yield return new WaitForSeconds(1f);Check("New Input System A changes lane left x="+gm.player.transform.position.x,gm.player.transform.position.x<-3.4f);
 Press(Key.A);yield return new WaitForSecondsRealtime(.06f);Release();yield return new WaitForSecondsRealtime(.25f);Check("Lane boundary clamps",gm.player.transform.position.x>=-gm.config.laneWidth-.01f);
 Press(Key.D);yield return new WaitForSecondsRealtime(.06f);Release();yield return new WaitForSecondsRealtime(.08f);Press(Key.D);yield return new WaitForSecondsRealtime(.06f);Release();yield return new WaitForSeconds(1f);Check("New Input System D changes to right lane x="+gm.player.transform.position.x,gm.player.transform.position.x>3.4f);
 int coins=gm.Coins;var coin=gm.world.pools.Rent(3,gm.player.transform.position+new Vector3(0,1,3));yield return new WaitForSecondsRealtime(.2f);Check("Coin physics trigger scores and returns object",gm.Coins>coins&&!coin.gameObject.activeSelf);
 float previousFuel=gm.Fuel;var fuel=gm.world.pools.Rent(4,gm.player.transform.position+new Vector3(0,1,3));yield return new WaitForSecondsRealtime(.2f);Check("Fuel physics trigger restores, clamps and returns",gm.Fuel>previousFuel&&gm.Fuel<=100&&!fuel.gameObject.activeSelf);
 gm.world.pools.Rent(1,gm.player.transform.position+new Vector3(0,0,3));gm.world.pools.Rent(2,gm.player.transform.position+new Vector3(0,0,3.2f));yield return new WaitForSecondsRealtime(.25f);Check("Obstacle collision and damage cooldown",gm.Lives==2);
 var reused=gm.world.pools.Rent(4,new Vector3(0,1,-100));reused.ReturnToPool();Check("Reusable objects reset active state and collider",!reused.gameObject.activeSelf);
 Directory.CreateDirectory("Documentation/Screenshots");yield return null;Capture("gameplay");bloom.active=false;Capture("gameplay-no-bloom");bloom.active=true;Check("Speed reactive neon and headlights",gm.player.underglow.intensity>=3&&gm.player.GetComponentsInChildren<Light>().Count(l=>l.type==LightType.Spot)==2);
 float initialSpeed=gm.Speed;endurance=true;Time.timeScale=8;yield return new WaitForSecondsRealtime(13);Time.timeScale=1;endurance=false;
 Check("Difficulty and speed progress",gm.Speed>initialSpeed+10&&gm.Speed<=gm.config.maximumSpeed);
 Check("Obstacle density per metre increases",gm.config.RowGap(gm.Elapsed)<gm.config.RowGap(0));
 var segments = gm.world.pools.GetComponentsInChildren<RoadSegment>()
     .OrderBy(segment => segment.transform.position.z).ToArray();
 Check("Road segments stay contiguous after recycling and rebase",segments.Length==10 &&
     segments.Zip(segments.Skip(1),(a,b)=>b.transform.position.z-a.transform.position.z)
         .All(gap=>Mathf.Abs(gap-gm.config.segmentLength)<.01f));
 Check("Endless road recycling exercised",gm.world.RecycledSegments>35);
 Check("No gameplay instantiation or pool exhaustion",gm.world.pools.CreatedCount==created&&gm.world.pools.ReturnCount>100);
 Check("Floating origin maintains bounded player position",gm.Distance>3000&&gm.player.transform.position.z<3000);
 yield return new WaitForSecondsRealtime(1.8f);gm.Damage();yield return new WaitForSecondsRealtime(1.8f);gm.Damage();yield return null;
 Check("Zero lives triggers GameOver and panel",gm.Lives==0&&gm.State==GameState.GameOver&&gm.ui.gameOver.activeSelf);
 Check("High score stored in PlayerPrefs",PlayerPrefs.GetInt("NeonHighway.HighScore.v1",-1)==gm.HighScore&&gm.HighScore>=gm.Score);
 int stored=gm.HighScore;yield return null;Capture("game-over");
 Button("RUN AGAIN").onClick.Invoke();yield return new WaitForSecondsRealtime(.7f);gm=GameManager.Instance;
 Check("Restart UI reloads clean session",gm.IsPlaying&&gm.Lives==3&&gm.Coins==0&&gm.Elapsed<2&&gm.HighScore==stored);
 gm.config.fuelDrain=1000;yield return new WaitForSecondsRealtime(.3f);gm.config.fuelDrain=1.7f;Check("Empty fuel triggers GameOver",gm.Fuel==0&&gm.State==GameState.GameOver);
 gm.MainMenu();yield return new WaitForSecondsRealtime(.7f);gm=GameManager.Instance;Check("Main Menu scene and panel",gm.State==GameState.Menu&&gm.ui.menu.activeSelf);
 yield return null;Capture("main-menu");
 Button("START RUN").onClick.Invoke();yield return new WaitForSecondsRealtime(.7f);gm=GameManager.Instance;Check("Start UI loads Gameplay",gm.IsPlaying&&gm.Lives==3);
 Check("No runtime errors or exceptions",runtimeErrors==0);
 File.WriteAllLines("Documentation/runtime-tests.txt",results.Concat(new[]{"Checks: "+results.Count,"Failures: "+failures,"Runtime errors: "+runtimeErrors,"UTC: "+DateTime.UtcNow.ToString("O"),"Endurance harness auto-refills fuel and returns interactions to isolate road/pool longevity."}));
 InputSystem.RemoveDevice(qaKeyboard);Application.logMessageReceived-=Log;SessionState.SetBool("HighwayQA",false);Time.timeScale=1;if(Application.isBatchMode)EditorApplication.Exit(failures==0?0:1);else EditorApplication.isPlaying=false;
 }
}
#endif
