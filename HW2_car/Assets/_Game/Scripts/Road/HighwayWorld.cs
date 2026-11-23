using System.Collections.Generic;
using UnityEngine;
namespace NeonHighway
{
    public sealed class HighwayWorld : MonoBehaviour
    {
        public PoolManager pools;
        public GameConfig config;
        public PlayerCar player;
        readonly Queue<PooledObject> roads=new();
        float nextRoad=-60,nextRow=100;
        int row,safeLane=1;
        System.Random random;
        public int RecycledSegments
        {
            get;
            private set;
        }
        public void Initialize()
        {
            random=new System.Random(48021);
            pools.Initialize();
            for(int i=0;i<10;i++)
            {
                roads.Enqueue(pools.Rent(0,new Vector3(0,0,nextRoad)));
                nextRoad+=config.segmentLength;
            }
        }
        void Update()
        {
            var gm=GameManager.Instance;
            if(gm==null||!gm.IsPlaying)return;
            float z=player.transform.position.z;
            while(roads.Count>0&&roads.Peek().transform.position.z+config.segmentLength<z-30)
            {
                roads.Dequeue().ReturnToPool();
                roads.Enqueue(pools.Rent(0,new Vector3(0,0,nextRoad)));
                nextRoad+=config.segmentLength;
                RecycledSegments++;
            }
            while(nextRow<z+360)
            {
                SpawnRow(gm);
                nextRow+=config.RowGap(gm.Elapsed);
            }
            if(z>3000)
            {
                pools.ShiftOrigin(3000);
                player.ShiftOrigin(3000);
                nextRoad-=3000;
                nextRow-=3000;
            }
        }
        void SpawnRow(GameManager gm)
        {
            row++;
            // Move the safe lane at most one lane between consecutive rows.
            safeLane = Mathf.Clamp(safeLane + random.Next(-1, 2), 0, 2);
            int obstacleLane = (safeLane + random.Next(1, 3)) % 3;
            int obstaclePool = row % 3 == 0 ? 2 : 1;
            float obstacleX = (obstacleLane - 1) * config.laneWidth;
            float pickupX = (safeLane - 1) * config.laneWidth;

            pools.Rent(obstaclePool, new Vector3(obstacleX, 0, nextRow));

            for (int coin = 0; coin < 5; coin++)
                pools.Rent(3, new Vector3(pickupX, 1.15f, nextRow + coin * 3));

            if (row % 4 == 0)
                pools.Rent(4, new Vector3(pickupX, 1.1f, nextRow + 20));
        }
    }
}
