using UnityEngine;
namespace NeonHighway {
[CreateAssetMenu(menuName="Neon Highway/Game Config")]
public sealed class GameConfig : ScriptableObject {
 public float laneWidth=3.6f, segmentLength=60, roadWidth=12;
 public float initialSpeed=24, maximumSpeed=58, acceleration=.16f;
 public float fuelCapacity=100, fuelDrain=1.7f, fuelPickup=32, damageCooldown=1.6f;
 public int coinValue=50;
 public AnimationCurve density=AnimationCurve.Linear(0,0,180,1);
 public float Speed(float elapsed)=>Mathf.Min(maximumSpeed,initialSpeed+elapsed*acceleration);
 public float RowGap(float elapsed)=>Mathf.Lerp(initialSpeed * 2.7f, initialSpeed * 2.1f, Mathf.Clamp01(density.Evaluate(elapsed)));
}
}