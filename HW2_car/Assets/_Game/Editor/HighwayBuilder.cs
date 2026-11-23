using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using NeonHighway;

[InitializeOnLoad]
public static class HighwayBuilder {
 const string Root="Assets/_Game/";
 static Material asphalt,metal,cyan,pink,white,gold,green,glass,rubber,body,red,particle,tail,marking;
 static GameConfig config;static TMP_FontAsset font;static InputActionAsset input;
 static double readyAt;static bool busy;
 static HighwayBuilder(){readyAt=EditorApplication.timeSinceStartup+8;EditorApplication.update+=Tick;}
 static void Tick(){if(busy||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.timeSinceStartup<readyAt)return;
 if(!File.Exists(Root+"Settings/Generated.marker")){Generate();return;}
 const string request="Temp/highway-command.txt";if(!File.Exists(request))return;string command=File.ReadAllText(request).Trim();File.Delete(request);
 try{if(command=="generate")Generate();else if(command=="build")Build();else if(command=="audit")Audit();else if(command=="play"){EditorSceneManager.OpenScene(Root+"Scenes/Gameplay.unity");EditorApplication.isPlaying=true;}File.WriteAllText("Temp/highway-command-result.txt","Completed: "+command);}catch(Exception e){File.WriteAllText("Temp/highway-command-result.txt",e.ToString());Debug.LogException(e);}}
 [MenuItem("Neon Highway/Generate Complete Game")]
 public static void Generate(){busy=true;try{
 foreach(string folder in new[]{"Scenes","Prefabs","Materials","Audio","Input","Settings","Models"})Directory.CreateDirectory(Root+folder);
 PlayerSettings.productName="HW2_Car";PlayerSettings.companyName="University Assignment";PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;PlayerSettings.runInBackground=true;PlayerSettings.colorSpace=ColorSpace.Linear;
 config=AssetDatabase.LoadAssetAtPath<GameConfig>(Root+"Settings/GameConfig.asset");
 if(!config){config=ScriptableObject.CreateInstance<GameConfig>();Save(config,Root+"Settings/GameConfig.asset");}
 ConfigureRender();CreateMaterials();CreateFont();CreateInput();
 var road=CreateRoad();var player=CreatePlayer();var stationary=CreateTraffic(false);var moving=CreateTraffic(true);var coin=CreateCoin();var fuel=CreateFuel();
 CreateScene("MainMenu",road,player,stationary,moving,coin,fuel);CreateScene("Gameplay",road,player,stationary,moving,coin,fuel);
 EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Root+"Scenes/MainMenu.unity",true),new EditorBuildSettingsScene(Root+"Scenes/Gameplay.unity",true)};
 AssetDatabase.SaveAssets();File.WriteAllText(Root+"Settings/Generated.marker","Generated "+DateTime.UtcNow.ToString("O"));AssetDatabase.Refresh();EditorSceneManager.OpenScene(Root+"Scenes/MainMenu.unity");Audit();Debug.Log("NEON HIGHWAY: scenes, prefabs, materials, input, audio and URP authored successfully.");
 }catch(Exception e){Debug.LogException(e);File.WriteAllText("Documentation/highway-generation-error.txt",e.ToString());throw;}finally{busy=false;}}
 static void Save(UnityEngine.Object asset,string path){if(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path)!=null)AssetDatabase.DeleteAsset(path);AssetDatabase.CreateAsset(asset,path);}
 static void ConfigureRender(){GraphicsSettings.defaultRenderPipeline=null;QualitySettings.renderPipeline=null;var renderer=ScriptableObject.CreateInstance<UniversalRendererData>();renderer.postProcessData=AssetDatabase.LoadAssetAtPath<PostProcessData>("Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");Save(renderer,Root+"Settings/NeonRenderer.asset");var pipeline=UniversalRenderPipelineAsset.Create(renderer);pipeline.supportsHDR=true;pipeline.msaaSampleCount=4;pipeline.renderScale=1;var serializedPipeline=new SerializedObject(pipeline);serializedPipeline.FindProperty("m_AdditionalLightsRenderingMode").intValue=1;serializedPipeline.ApplyModifiedPropertiesWithoutUndo();pipeline.maxAdditionalLightsCount=8;pipeline.shadowDistance=90;Save(pipeline,Root+"Settings/NeonURP.asset");GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;
 var profile=ScriptableObject.CreateInstance<VolumeProfile>();Save(profile,Root+"Settings/NightVolume.asset");var bloom=profile.Add<Bloom>(true);bloom.intensity.Override(.8f);bloom.threshold.Override(1);bloom.scatter.Override(.72f);var tone=profile.Add<Tonemapping>(true);tone.mode.Override(TonemappingMode.ACES);var vignette=profile.Add<Vignette>(true);vignette.intensity.Override(.23f);var color=profile.Add<ColorAdjustments>(true);color.postExposure.Override(.4f);color.contrast.Override(12);foreach(var c in profile.components)AssetDatabase.AddObjectToAsset(c,profile);EditorUtility.SetDirty(profile);
 }
 static Material Mat(string name,Color color,float metallic=0,float smooth=.4f,float emission=0){var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.name=name;m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smooth);if(emission>0){m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*emission);}Save(m,Root+"Materials/"+name+".mat");return m;}
 static void CreateMaterials(){asphalt=Mat("Midnight Asphalt",new Color(.025f,.035f,.065f),.28f,.54f);metal=Mat("Titanium",new Color(.12f,.17f,.25f),.8f,.65f);cyan=Mat("Ion Cyan",new Color(1,.51f,.14f),.4f,.8f,5);pink=Mat("Laser Magenta",new Color(.28f,.66f,.52f),.4f,.7f,5);white=Mat("Headlamp",new Color(.65f,.85f,1),0,.6f,7);gold=Mat("Coin Gold",new Color(1,.6f,.035f),.65f,.8f,2.5f);green=Mat("Energy Mint",new Color(.02f,1,.45f),.4f,.6f,3);glass=Mat("Smoked Glass",new Color(.025f,.07f,.13f),.7f,.95f);rubber=Mat("Tires",new Color(.008f,.01f,.018f),.1f,.25f);body=Mat("Player Pearl",new Color(.78f,.32f,.13f),.8f,.8f);red=Mat("Traffic Vermilion",new Color(.75f,.055f,.07f),.6f,.7f);
 tail=Mat("Ruby Lamp",new Color(1,.055f,.025f),0,.5f,3);marking=Mat("Ivory Road Paint",new Color(.72f,.63f,.48f),0,.45f,.75f);
 particle=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));particle.SetColor("_BaseColor",Color.white);particle.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"Materials/ParticleGlow.png"));particle.SetFloat("_Surface",1);particle.SetFloat("_Blend",2);particle.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);particle.SetFloat("_DstBlend",(float)BlendMode.One);particle.SetFloat("_ZWrite",0);particle.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");particle.renderQueue=3000;Save(particle,Root+"Materials/NeonParticles.mat");}
 static void CreateFont(){font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");if(font==null)throw new Exception("TMP essential font missing after import");}
 static void CreateInput(){input=ScriptableObject.CreateInstance<InputActionAsset>();var map=input.AddActionMap("Driving");var l=map.AddAction("Left",InputActionType.Button);l.AddBinding("<Keyboard>/a");l.AddBinding("<Keyboard>/leftArrow");var r=map.AddAction("Right",InputActionType.Button);r.AddBinding("<Keyboard>/d");r.AddBinding("<Keyboard>/rightArrow");map.AddAction("Pause",InputActionType.Button,"<Keyboard>/escape");File.WriteAllText(Root+"Input/Highway.inputactions",input.ToJson());UnityEngine.Object.DestroyImmediate(input);AssetDatabase.ImportAsset(Root+"Input/Highway.inputactions");input=AssetDatabase.LoadAssetAtPath<InputActionAsset>(Root+"Input/Highway.inputactions");}
 static GameObject Box(string name,Transform parent,Vector3 position,Vector3 scale,Material mat,bool collider=false){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=position;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=mat;if(!collider)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;}
 static GameObject Sphere(string name,Transform parent,Vector3 p,Vector3 scale,Material mat){var g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=mat;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;}
 static T Prefab<T>(GameObject go,string name)where T:Component {var saved=PrefabUtility.SaveAsPrefabAsset(go,Root+"Prefabs/"+name+".prefab");UnityEngine.Object.DestroyImmediate(go);return saved.GetComponent<T>();}
 static RoadSegment CreateRoad()
 {
     var root = new GameObject("RoadSegment");
     root.AddComponent<RoadSegment>();
     Box("Road surface", root.transform, new Vector3(0,-.2f,30), new Vector3(12,.4f,60), asphalt, true);
     for (int side = -1; side <= 1; side += 2)
     {
         var parent = root.transform;
         Box("Concrete barrier", parent, new Vector3(side*6.65f,.36f,30), new Vector3(.65f,.72f,60), metal, true);
         Box("Barrier cap", parent, new Vector3(side*6.65f,.75f,30), new Vector3(.32f,.1f,60), body);
         Box("Solid edge stripe", parent, new Vector3(side*5.65f,.028f,30), new Vector3(.13f,.03f,60), cyan);
         for (int z = 5; z < 60; z += 10)
             Box("Cat eye", parent, new Vector3(side*6.31f,.52f,z), new Vector3(.07f,.16f,.58f), gold);
         for (int z = 12; z < 60; z += 30)
         {
             Box("Bent lamp post", parent, new Vector3(side*7.4f,3.3f,z), new Vector3(.17f,6.6f,.17f), metal);
             var arm = Box("Slanted lamp arm", parent, new Vector3(side*6.4f,6.75f,z), new Vector3(2.3f,.15f,.19f), metal);
             arm.transform.localRotation = Quaternion.Euler(0,0,-side*12);
             Box("Amber lamp", parent, new Vector3(side*5.45f,6.45f,z), new Vector3(.68f,.18f,.66f), cyan);
             var light = new GameObject("Amber streetlight").AddComponent<Light>();
             light.transform.SetParent(parent,false);
             light.transform.localPosition = new Vector3(side*5.45f,6.2f,z);
             light.type = LightType.Point;
             light.color = new Color(1,.66f,.34f);
             light.range = 15;
             light.intensity = 7;
         }
         for (int i = 0; i < 3; i++)
         {
             float z = 9 + i*20;
             float x = side*(16 + i%2*4);
             float height = 5 + (i*3 + (side>0?1:0))%7;
             Box("Warehouse", parent, new Vector3(x,height*.5f-.7f,z), new Vector3(10,height,13), metal);
             Box("Stepped roof", parent, new Vector3(x,height-.25f,z+2), new Vector3(7,1.4f,8), asphalt);
             for (int bay=-1; bay<=1; bay++)
                 Box("Warehouse window", parent, new Vector3(x-side*5.03f,height*.6f,z+bay*3), new Vector3(.08f,.42f,1.35f), i%2==0?gold:pink);
             float tower = 12 + (i*7 + (side>0?4:0))%13;
             Box("Distant tower", parent, new Vector3(side*35,tower*.5f-1,z+3), new Vector3(9,tower,10), asphalt);
             Box("Rooftop beacon", parent, new Vector3(side*35,tower-.5f,z+3), new Vector3(.28f,1.1f,.28f), tail);
         }
         Box("Sign post", parent, new Vector3(side*7.2f,1.7f,29), new Vector3(.14f,3.4f,.14f), metal);
         Box("Chevron board", parent, new Vector3(side*7.2f,2.9f,29), new Vector3(1.45f,.75f,.18f), asphalt);
         for (int chevron = 0; chevron < 2; chevron++)
         {
             float x = side*(6.93f + chevron*.49f);
             for (int v = -1; v <= 1; v += 2)
             {
                 var strip = Box("Reflective chevron", parent, new Vector3(x,2.9f+v*.11f,28.89f), new Vector3(.09f,.3f,.05f), cyan);
                 strip.transform.localRotation = Quaternion.Euler(0,0,side*v*38);
             }
         }
     }
     for (int side=-1; side<=1; side+=2)
         for (int z=0; z<60; z+=10)
             for (int dash=0; dash<2; dash++)
                 Box("Paired lane dashes",root.transform,new Vector3(side*1.8f,.024f,z+1.4f+dash*1.75f),new Vector3(.12f,.035f,1.25f),marking);
     CombineRoad(root);
     return Prefab<RoadSegment>(root,"RoadSegment");
 }
 static void CombineRoad(GameObject root){var groups=root.GetComponentsInChildren<MeshFilter>().GroupBy(f=>f.GetComponent<MeshRenderer>().sharedMaterial).ToArray();foreach(var group in groups){var mesh=new Mesh();mesh.name="Road "+group.Key.name;mesh.indexFormat=IndexFormat.UInt32;mesh.CombineMeshes(group.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=root.transform.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray());Save(mesh,Root+"Models/Road_"+group.Key.name+".asset");var g=new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(root.transform,false);g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<MeshRenderer>().sharedMaterial=group.Key;foreach(var f in group){UnityEngine.Object.DestroyImmediate(f.GetComponent<MeshRenderer>());UnityEngine.Object.DestroyImmediate(f);}}}
 static Mesh CoupeCabin(){string path=Root+"Models/CoupeCabin.asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing)return existing;var mesh=new Mesh{name="Sloped Coupe Canopy"};mesh.vertices=new[]{new Vector3(-.5f,-.5f,-.5f),new Vector3(.5f,-.5f,-.5f),new Vector3(.5f,-.5f,.5f),new Vector3(-.5f,-.5f,.5f),new Vector3(-.42f,.5f,-.25f),new Vector3(.42f,.5f,-.25f),new Vector3(.42f,.5f,.16f),new Vector3(-.42f,.5f,.16f)};mesh.triangles=new[]{0,2,1,0,3,2,4,5,6,4,6,7,0,1,5,0,5,4,1,2,6,1,6,5,2,3,7,2,7,6,3,0,4,3,4,7};var indices=mesh.triangles;for(int i=0;i<indices.Length;i+=3){int swap=indices[i];indices[i]=indices[i+1];indices[i+1]=swap;}mesh.triangles=indices;mesh.RecalculateNormals();mesh.RecalculateBounds();Save(mesh,path);return mesh;}
 static Transform CarModel(Transform root,Material paint,Material accent){var visual=new GameObject("Car Visual").transform;visual.SetParent(root,false);
 Box("Low Chassis",visual,new Vector3(0,.48f,0),new Vector3(1.95f,.45f,4.1f),metal);Box("Aerodynamic Body",visual,new Vector3(0,.78f,0),new Vector3(1.92f,.62f,4.04f),paint);Box("Hood",visual,new Vector3(0,1.03f,1.13f),new Vector3(1.72f,.15f,1.4f),paint);
 var cabin=Box("Panoramic Cockpit",visual,new Vector3(0,1.39f,-.48f),new Vector3(1.62f,.8f,2.15f),glass);cabin.GetComponent<MeshFilter>().sharedMesh=CoupeCabin();Box("Wagon Roof",visual,new Vector3(0,1.8f,-.58f),new Vector3(1.5f,.11f,1.72f),paint);
 for(int side=-1;side<=1;side+=2){Box("Shoulder Highlight",visual,new Vector3(side*.96f,.86f,0),new Vector3(.055f,.08f,3.1f),accent);for(int lamp=0;lamp<2;lamp++)
 {
     float x=side*(.48f+lamp*.29f);
     Sphere("Round headlamp",visual,new Vector3(x,.91f,2.04f),new Vector3(.22f,.22f,.13f),white);
     Box("Vertical tail lamp",visual,new Vector3(x,.85f,-2.05f),new Vector3(.12f,.24f,.075f),tail);
 }
 Box(side < 0 ? "Left Indicator" : "Right Indicator",visual,new Vector3(side*.96f,.88f,-1.94f),new Vector3(.12f,.09f,.24f),cyan);
 for(int z=-1;z<=1;z+=2){var wheel=GameObject.CreatePrimitive(PrimitiveType.Cylinder);wheel.name="Alloy Wheel";wheel.transform.SetParent(visual,false);wheel.transform.localPosition=new Vector3(side*.99f,.45f,z*1.22f);wheel.transform.localRotation=Quaternion.Euler(0,0,90);wheel.transform.localScale=new Vector3(.78f,.15f,.78f);wheel.GetComponent<Renderer>().sharedMaterial=rubber;UnityEngine.Object.DestroyImmediate(wheel.GetComponent<Collider>());Sphere("Wheel Hub",visual,new Vector3(side*1.15f,.45f,z*1.22f),new Vector3(.055f,.46f,.46f),metal);}}
 Box("Tailgate",visual,new Vector3(0,1.03f,-1.92f),new Vector3(1.78f,.44f,.17f),paint);Box("Diffuser",visual,new Vector3(0,.45f,-2.06f),new Vector3(1.65f,.17f,.2f),metal);return visual;}
 static PlayerCar CreatePlayer(){var root=new GameObject("PlayerCar");var car=root.AddComponent<PlayerCar>();car.config=config;car.input=input;car.visual=CarModel(root.transform,body,cyan);car.leftIndicator=car.visual.Find("Left Indicator").GetComponent<Renderer>();car.rightIndicator=car.visual.Find("Right Indicator").GetComponent<Renderer>();var rb=root.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;rb.interpolation=RigidbodyInterpolation.Interpolate;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;var box=root.AddComponent<BoxCollider>();box.center=new Vector3(0,.8f,0);box.size=new Vector3(1.88f,1.55f,4f);
 car.neonRenderer=Box("Speed Reactive Underglow",car.visual,new Vector3(0,.24f,0),new Vector3(1.8f,.06f,3.6f),cyan).GetComponent<Renderer>();var under=new GameObject("Underglow").AddComponent<Light>();under.transform.SetParent(root.transform,false);under.transform.localPosition=new Vector3(0,.5f,0);under.type=LightType.Point;under.color=cyan.color;under.range=5;under.intensity=3;car.underglow=under;
 for(int side=-1;side<=1;side+=2){var head=new GameObject("Functional Headlight").AddComponent<Light>();head.transform.SetParent(root.transform,false);head.transform.localPosition=new Vector3(side*.65f,.9f,1.9f);head.transform.localRotation=Quaternion.Euler(7,0,0);head.type=LightType.Spot;head.color=new Color(1,.84f,.61f);head.range=65;head.spotAngle=52;head.innerSpotAngle=28;head.intensity=30;head.shadows=LightShadows.None;}
 car.exhaust=Particles("Amber Exhaust",root.transform,cyan.color,28,.3f,1.1f,.13f,true);car.exhaust.transform.localPosition=new Vector3(0,.55f,-2);car.exhaust.transform.localRotation=Quaternion.Euler(0,180,0);var emission=car.exhaust.emission;emission.rateOverTime=25;var shape=car.exhaust.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=12;shape.radius=.38f;
 return Prefab<PlayerCar>(root,"PlayerCar");}
 static Interactable CreateTraffic(bool moving){var root=new GameObject(moving?"MovingCarObstacle":"StationaryCarObstacle");Interactable item=moving?(Interactable)root.AddComponent<MovingCarObstacle>():root.AddComponent<StationaryCarObstacle>();CarModel(root.transform,moving?gold:red,moving?gold:pink);var collider=root.GetComponent<BoxCollider>();collider.isTrigger=true;collider.center=new Vector3(0,.85f,0);collider.size=new Vector3(1.85f,1.5f,4);return Prefab<Interactable>(root,root.name);}
 static CoinCollectible CreateCoin(){var root=new GameObject("CoinCollectible");var item=root.AddComponent<CoinCollectible>();var g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name="Gold Token";g.transform.SetParent(root.transform,false);g.transform.localRotation=Quaternion.Euler(90,0,0);g.transform.localScale=new Vector3(.85f,.12f,.85f);g.GetComponent<Renderer>().sharedMaterial=gold;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());Box("Coin Inlay",root.transform,new Vector3(0,0,-.135f),new Vector3(.14f,.48f,.03f),white);var collider=root.GetComponent<BoxCollider>();collider.size=new Vector3(1.35f,1.5f,1.4f);collider.isTrigger=true;return Prefab<CoinCollectible>(root,"CoinCollectible");}
 static FuelCollectible CreateFuel(){var root=new GameObject("FuelCollectible");var item=root.AddComponent<FuelCollectible>();Box("Energy Canister",root.transform,Vector3.zero,new Vector3(.65f,.85f,.5f),green);Box("Handle",root.transform,new Vector3(0,.6f,0),new Vector3(.4f,.13f,.3f),metal);Box("Cap",root.transform,new Vector3(.24f,.48f,0),new Vector3(.2f,.17f,.22f),gold);Box("Energy Stripe",root.transform,new Vector3(0,0,-.26f),new Vector3(.12f,.6f,.03f),white);var collider=root.GetComponent<BoxCollider>();collider.size=new Vector3(1.4f,1.8f,1.4f);collider.isTrigger=true;return Prefab<FuelCollectible>(root,"FuelCollectible");}
 static ParticleSystem Particles(string name,Transform parent,Color color,int max,float lifetime,float speed,float size,bool loop=false){var ps=new GameObject(name).AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);ps.transform.SetParent(parent,false);var main=ps.main;main.loop=loop;main.playOnAwake=loop;main.maxParticles=max;main.startLifetime=lifetime;main.startSpeed=speed;main.startSize=size;main.startColor=color*2;main.simulationSpace=ParticleSystemSimulationSpace.World;var emission=ps.emission;emission.rateOverTime=0;var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.2f;var col=ps.colorOverLifetime;col.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(color,1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(0,1)});col.color=gradient;ps.GetComponent<ParticleSystemRenderer>().sharedMaterial=particle;return ps;}
 static AudioSource Source(Transform parent,string name,string clip,float volume,bool loop){var s=new GameObject(name).AddComponent<AudioSource>();s.transform.SetParent(parent,false);s.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"Audio/"+clip+".wav");s.volume=volume;s.loop=loop;s.playOnAwake=loop;s.spatialBlend=0;return s;}
 static void CreateScene(string name,RoadSegment road,PlayerCar playerPrefab,Interactable stationary,Interactable moving,CoinCollectible coin,FuelCollectible fuel){var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
 RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.27f,.16f,.12f);RenderSettings.fog=true;RenderSettings.fogColor=new Color(.05f,.022f,.017f);RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.0037f;
 var sky=new Material(Shader.Find("NeonHighway/NightSky"));if(name=="MainMenu")Save(sky,Root+"Materials/NightSky.mat");else{UnityEngine.Object.DestroyImmediate(sky);sky=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/NightSky.mat");}RenderSettings.skybox=sky;
 var moon=new GameObject("Moon Key Light").AddComponent<Light>();moon.type=LightType.Directional;moon.transform.rotation=Quaternion.Euler(35,-30,0);moon.color=new Color(1,.64f,.43f);moon.intensity=.45f;moon.shadows=LightShadows.Soft;
 var volume=new GameObject("Global Volume - Bloom ACES").AddComponent<Volume>();volume.isGlobal=true;volume.sharedProfile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(Root+"Settings/NightVolume.asset");
 var camera=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();camera.tag="MainCamera";camera.nearClipPlane=.1f;camera.farClipPlane=700;camera.allowHDR=true;camera.backgroundColor=new Color(.005f,.01f,.035f);camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
 var playerObject=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Prefabs/PlayerCar.prefab"));var player=playerObject.GetComponent<PlayerCar>();player.transform.position=new Vector3(0,0,15);player.followCamera=camera;player.ResetCamera();camera.transform.rotation=Quaternion.Euler(14,0,0);
 var pools=new GameObject("Object Pools - Prewarmed").AddComponent<PoolManager>();pools.entries=new[]{new PoolManager.Entry{prefab=road,capacity=10},new PoolManager.Entry{prefab=stationary,capacity=36},new PoolManager.Entry{prefab=moving,capacity=24},new PoolManager.Entry{prefab=coin,capacity=160},new PoolManager.Entry{prefab=fuel,capacity=16}};
 var world=new GameObject("Procedural Highway").AddComponent<HighwayWorld>();world.config=config;world.player=player;world.pools=pools;
 var effects=new GameObject("Pooled Particle Effects").AddComponent<Effects>();effects.impact=Particles("Impact Sparks",effects.transform,new Color(1,.45f,.04f),100,.7f,9,.1f);effects.pickup=Particles("Pickup Burst",effects.transform,Color.cyan,100,.65f,4,.14f);effects.firework=Particles("Sky Fireworks",effects.transform,new Color(1,.38f,.08f),350,2.6f,10,.25f);var fm=effects.firework.main;fm.gravityModifier=.16f;effects.rocket=Particles("Firework Launch",effects.transform,new Color(1,.68f,.25f),8,1.15f,20,.22f);var rs=effects.rocket.shape;rs.shapeType=ParticleSystemShapeType.Cone;rs.angle=0;effects.rocket.transform.rotation=Quaternion.Euler(-90,0,0);
 var stars=Particles("Stars",null,new Color(.4f,.65f,1),350,10000,0,.22f);var sm=stars.main;sm.simulationSpace=ParticleSystemSimulationSpace.Local;sm.startSize=new ParticleSystem.MinMaxCurve(.12f,.45f);sm.loop=true;sm.playOnAwake=true;var se=stars.emission;se.SetBursts(new[]{new ParticleSystem.Burst(0,220)});var ss=stars.shape;ss.shapeType=ParticleSystemShapeType.Box;ss.scale=new Vector3(500,70,550);stars.transform.position=new Vector3(0,105,250);stars.transform.SetParent(player.transform,true);
 var audio=new GameObject("Audio System").AddComponent<GameAudio>();audio.music=Source(audio.transform,"Original Synthwave","music",.16f,true);audio.engine=Source(audio.transform,"Speed Reactive Engine","engine",.19f,true);audio.sfx=Source(audio.transform,"One Shot Effects","coin",.5f,false);audio.coin=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"Audio/coin.wav");audio.fuel=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"Audio/fuel.wav");audio.impact=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"Audio/impact.wav");
 var gm=new GameObject("GameManager - Singleton").AddComponent<GameManager>();gm.config=config;gm.player=player;gm.world=world;gm.audioSystem=audio;gm.effects=effects;gm.ui=CreateUI();
 EditorSceneManager.SaveScene(scene,Root+"Scenes/"+name+".unity");}
 static RectTransform Rect(string name,Transform parent,Vector2 anchor,Vector2 pos,Vector2 size){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=anchor;r.anchoredPosition=pos;r.sizeDelta=size;return r;}
 static TMP_Text Label(string text,Transform parent,Vector2 anchor,Vector2 pos,Vector2 size,float fontSize,Color color,TextAlignmentOptions align=TextAlignmentOptions.Center){var r=Rect(text,parent,anchor,pos,size);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.text=text;t.fontSize=fontSize;t.color=color;t.alignment=align;t.raycastTarget=false;return t;}
 static Image Panel(string name,Transform parent,Color color){var r=Rect(name,parent,new Vector2(.5f,.5f),Vector2.zero,Vector2.zero);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;var image=r.gameObject.AddComponent<Image>();image.color=color;return image;}
 static void Button(string text,Transform parent,Vector2 pos,UnityAction action){var r=Rect(text,parent,new Vector2(.5f,.5f),pos,new Vector2(310,52));var image=r.gameObject.AddComponent<Image>();image.color=new Color(.24f,.105f,.045f,.96f);var outline=r.gameObject.AddComponent<Outline>();outline.effectColor=new Color(1,.62f,.26f,.85f);outline.effectDistance=new Vector2(1,-1);var button=r.gameObject.AddComponent<Button>();button.targetGraphic=image;var colors=button.colors;colors.highlightedColor=new Color(1,.78f,.53f);colors.pressedColor=new Color(.73f,.4f,.25f);button.colors=colors;UnityEventTools.AddPersistentListener(button.onClick,action);Label(text,r,new Vector2(.5f,.5f),Vector2.zero,new Vector2(290,45),21,Color.white);}
 static GameUI CreateUI(){var canvas=new GameObject("Neon Interface",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;var ui=canvas.AddComponent<GameUI>();
 var hud=Panel("HUD",canvas.transform,Color.clear);hud.raycastTarget=false;ui.hud=hud.gameObject;
 var amber=new Color(1,.75f,.42f);
 var dashboard=Rect("Dashboard",hud.transform,new Vector2(.5f,0),new Vector2(0,73),new Vector2(880,114));
 var background=dashboard.gameObject.AddComponent<Image>();
 background.color=new Color(.12f,.045f,.024f,.94f);
 background.raycastTarget=false;
 ui.speed=Label("000 KM/H",dashboard,new Vector2(0,.5f),new Vector2(147,0),new Vector2(265,76),51,amber);
 ui.score=Label("000000",dashboard,new Vector2(.5f,.5f),new Vector2(0,15),new Vector2(220,48),35,Color.white);
 ui.highScore=Label("BEST 0",dashboard,new Vector2(.5f,.5f),new Vector2(0,-21),new Vector2(220,28),17,amber);
 ui.lives=Label("LIVES 3",dashboard,new Vector2(1,.5f),new Vector2(-146,23),new Vector2(250,34),24,Color.white);
 var track=Rect("Fuel Track",dashboard,new Vector2(1,.5f),new Vector2(-146,-26),new Vector2(205,9));
 track.gameObject.AddComponent<Image>().color=new Color(.3f,.18f,.12f);
 var fill=Rect("Fuel Fill",track,new Vector2(.5f,.5f),Vector2.zero,new Vector2(205,9));
 ui.fuelFill=fill.gameObject.AddComponent<Image>();ui.fuelFill.type=Image.Type.Filled;ui.fuelFill.fillMethod=Image.FillMethod.Horizontal;
 ui.fuelFill.sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");ui.fuelFill.color=amber;
 ui.fuelText=Label("FUEL 100%",dashboard,new Vector2(1,.5f),new Vector2(-146,-8),new Vector2(205,24),16,amber);
 Label("A / D  STEER    ESC  PAUSE",hud.transform,new Vector2(.5f,1),new Vector2(0,-33),new Vector2(500,32),18,amber);
 ui.damageFlash=Panel("Damage Flash",hud.transform,Color.clear);ui.damageFlash.raycastTarget=false;

 ui.menu=Panel("Main Menu",canvas.transform,new Color(.04f,.014f,.008f,.26f)).gameObject;
 var menuCard=Rect("Route Card",ui.menu.transform,new Vector2(0,.5f),new Vector2(305,0),new Vector2(490,580));
 menuCard.gameObject.AddComponent<Image>().color=new Color(.09f,.032f,.018f,.95f);
 var title=Label("COPPER\nMILE",menuCard,new Vector2(.5f,.5f),new Vector2(0,135),new Vector2(380,235),74,amber,TextAlignmentOptions.Left);
 title.fontStyle=FontStyles.Bold | FontStyles.Italic;title.characterSpacing=3;title.lineSpacing=-15;
 Label("A / D  STEER    ESC  PAUSE",menuCard,new Vector2(.5f,.5f),new Vector2(0,-23),new Vector2(380,36),19,Color.white,TextAlignmentOptions.Left);
 Button("START RUN",menuCard,new Vector2(-35,-105),ui.StartGame);
 Button("EXIT",menuCard,new Vector2(-35,-177),ui.Exit);
 ui.pause=Panel("Pause Menu",canvas.transform,new Color(.005f,.01f,.04f,.9f)).gameObject;Label("RUN PAUSED",ui.pause.transform,new Vector2(.5f,.5f),new Vector2(0,180),new Vector2(1000,80),56,new Color(1,.78f,.45f));Button("RESUME",ui.pause.transform,new Vector2(0,45),ui.Resume);Button("RESTART",ui.pause.transform,new Vector2(0,-30),ui.StartGame);Button("MAIN MENU",ui.pause.transform,new Vector2(0,-105),ui.MainMenu);
 ui.gameOver=Panel("Game Over",canvas.transform,new Color(.015f,.005f,.035f,.92f)).gameObject;Label("END OF THE LINE",ui.gameOver.transform,new Vector2(.5f,.5f),new Vector2(0,200),new Vector2(1200,90),58,new Color(1,.12f,.45f));Label("GAME OVER",ui.gameOver.transform,new Vector2(.5f,.5f),new Vector2(0,140),new Vector2(800,45),23,Color.white);ui.finalScore=Label("SCORE 0",ui.gameOver.transform,new Vector2(.5f,.5f),new Vector2(0,50),new Vector2(700,100),30,Color.white);Button("RUN AGAIN",ui.gameOver.transform,new Vector2(0,-60),ui.StartGame);Button("MAIN MENU",ui.gameOver.transform,new Vector2(0,-135),ui.MainMenu);
 ui.pause.SetActive(false);ui.gameOver.SetActive(false);
 var es=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));es.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();return ui;}
 [MenuItem("Neon Highway/Audit")]
 public static void Audit(){Directory.CreateDirectory("Documentation");var prefabs=AssetDatabase.FindAssets("t:Prefab",new[]{Root+"Prefabs"});int missing=0;foreach(var guid in prefabs){var go=AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));foreach(var t in go.GetComponentsInChildren<Transform>(true))missing+=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);}
 File.WriteAllText("Documentation/authoring-audit.txt","Unity "+Application.unityVersion+"\nPrefabs: "+prefabs.Length+"\nMissing prefab scripts: "+missing+"\nInput asset: "+(AssetDatabase.LoadAssetAtPath<InputActionAsset>(Root+"Input/Highway.inputactions")!=null)+"\nBuild scenes: "+EditorBuildSettings.scenes.Length+"\nPipeline: "+GraphicsSettings.defaultRenderPipeline+"\nNot a runtime test.\n");}
 [MenuItem("Neon Highway/Build Windows")]
 public static void Build(){Directory.CreateDirectory("Submission/Windows");var report=BuildPipeline.BuildPlayer(EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),"Submission/Windows/NeonHighway.exe",BuildTarget.StandaloneWindows64,BuildOptions.None);Directory.CreateDirectory("Documentation");File.WriteAllText("Documentation/build-result.txt",report.summary.result+"\nSize: "+report.summary.totalSize+"\nErrors: "+report.summary.totalErrors+"\nWarnings: "+report.summary.totalWarnings+"\n");if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Windows build failed: "+report.summary.result);}
}