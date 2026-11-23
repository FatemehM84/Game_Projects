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

[InitializeOnLoad]
public static class HighwayQA {
 static HighwayQA(){EditorApplication.playModeStateChanged+=Changed;EditorApplication.update+=()=>{if(EditorApplication.isPlaying&&SessionState.GetBool("HighwayQA",false))EditorApplication.QueuePlayerLoopUpdate();};}
 [MenuItem("Neon Highway/Run Play Mode Verification")] public static void Run(){typeof(Editor).Assembly.GetType("UnityEditor.LogEntries").GetMethod("SetConsoleFlag",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{4,false});EditorApplication.isPaused=false;SessionState.SetBool("HighwayQA",true);EditorSceneManager.OpenScene("Assets/_Game/Scenes/Gameplay.unity");EditorApplication.EnterPlaymode();}
 static void Changed(PlayModeStateChange change){if(change==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("HighwayQA",false))new GameObject("Automated QA Harness").AddComponent<HighwayQARunner>();}
}
