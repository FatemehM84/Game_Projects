using UnityEngine;
namespace NeonHighway {public sealed class RoadSegment : PooledObject {public override void OnRent(){var cfg=GameManager.Instance.config;transform.localScale=new Vector3(cfg.roadWidth/12f,1,cfg.segmentLength/60f);base.OnRent();}}}
