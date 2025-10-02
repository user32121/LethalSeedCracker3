using UnityEngine;
using UnityEngine.AI;

namespace LethalSeedCracker3.src.cracker
{
    internal class CrackingRoundManager(int seed)
    {
        internal System.Random LevelRandom = new(seed);
        internal System.Random AnomalyRandom = new(seed + 5);
        internal System.Random EnemySpawnRandom = new(seed + 40);
        internal System.Random OutsideEnemySpawnRandom = new(seed + 41);
        internal System.Random BreakerBoxRandom = new(seed + 20);

        public static int GetRandomWeightedIndex(int[] weights, System.Random randomSeed)
        {
            if (weights == null || weights.Length == 0)
            {
                return -1;
            }
            int num = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                if (weights[i] >= 0)
                {
                    num += weights[i];
                }
            }
            if (num <= 0)
            {
                return randomSeed.Next(0, weights.Length);
            }
            float num2 = (float)randomSeed.NextDouble();
            float num3 = 0f;
            for (int i = 0; i < weights.Length; i++)
            {
                if (!(weights[i] <= 0f))
                {
                    num3 += weights[i] / (float)num;
                    if (num3 >= num2)
                    {
                        return i;
                    }
                }
            }
            return randomSeed.Next(0, weights.Length);
        }

        public static Vector3 GetRandomNavMeshPositionInBoxPredictable(Vector3 pos, float radius, System.Random randomSeed, int layerMask = -1, float verticalScale = 1f)
        {
            float y = pos.y;
            float x = RandomNumberInRadius(radius, randomSeed);
            float y2 = RandomNumberInRadius(radius * verticalScale, randomSeed);
            float z = RandomNumberInRadius(radius, randomSeed);
            Vector3 vector = new Vector3(x, y2, z) + pos;
            vector.y = y;
            float num = Vector3.Distance(pos, vector);
            if (NavMesh.SamplePosition(vector, out NavMeshHit navHit, num + 2f, layerMask))
            {
                return navHit.position;
            }
            return pos;
        }

        private static float RandomNumberInRadius(float radius, System.Random randomSeed)
        {
            return ((float)randomSeed.NextDouble() - 0.5f) * radius;
        }

        public static Vector3 PositionWithDenialPointsChecked(Result result, Vector3 spawnPosition, GameObject[] spawnPoints, EnemyType enemyType, float distanceFromShip = -1f)
        {
            if (spawnPoints.Length == 0)
            {
                Debug.LogError("Spawn points array was null in denial points check function!");
                return spawnPosition;
            }
            GameObject[] spawnDenialPoints = GameObject.FindGameObjectsWithTag("SpawnDenialPoint");
            int num = 0;
            bool flag = false;
            for (int i = 0; i < spawnPoints.Length - 1; i++)
            {
                for (int j = 0; j < spawnDenialPoints.Length; j++)
                {
                    flag = true;
                    if (Vector3.Distance(spawnPosition, spawnDenialPoints[j].transform.position) < 16f || (distanceFromShip != -1f && Vector3.Distance(spawnPosition, StartOfRound.Instance.shipLandingPosition.transform.position) < distanceFromShip))
                    {
                        num = (num + 1) % spawnPoints.Length;
                        spawnPosition = spawnPoints[num].transform.position;
                        spawnPosition = GetRandomNavMeshPositionInBoxPredictable(spawnPosition, 10f, result.crm.AnomalyRandom, GetLayermaskForEnemySizeLimit(enemyType));
                        flag = false;
                        break;
                    }
                }
                if (flag)
                {
                    break;
                }
            }
            return spawnPosition;
        }

        public static int GetLayermaskForEnemySizeLimit(EnemyType enemyType)
        {
            if (enemyType.SizeLimit == NavSizeLimit.MediumSpaces)
            {
                return -97;
            }
            if (enemyType.SizeLimit == NavSizeLimit.SmallSpaces)
            {
                return -33;
            }
            return -1;
        }

        public static float YRotationThatFacesTheNearestFromPosition(Vector3 pos, float maxDistance = 25f, int resolution = 6)
        {
            int num = 0;
            float num2 = 100f;
            bool flag = false;
            for (int i = 0; i < 360; i += 360 / resolution)
            {
                RoundManager.Instance.tempTransform.eulerAngles = new Vector3(0f, i, 0f);
                if (Physics.Raycast(pos, RoundManager.Instance.tempTransform.forward, out var hitInfo, maxDistance, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore))
                {
                    flag = true;
                    if (hitInfo.distance < num2)
                    {
                        num2 = hitInfo.distance;
                        num = i;
                    }
                }
            }
            if (!flag)
            {
                return -777f;
            }
            return Random.Range(num - 15, num + 15);
        }

        public static float YRotationThatFacesTheFarthestFromPosition(Vector3 pos, float maxDistance = 25f, int resolution = 6)
        {
            int num = 0;
            float num2 = 0f;
            for (int i = 0; i < 360; i += 360 / resolution)
            {
                RoundManager.Instance.tempTransform.eulerAngles = new Vector3(0f, i, 0f);
                if (Physics.Raycast(pos, RoundManager.Instance.tempTransform.forward, out var hitInfo, maxDistance, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore))
                {
                    if (hitInfo.distance > num2)
                    {
                        num2 = hitInfo.distance;
                        num = i;
                    }
                    continue;
                }
                num = i;
                break;
            }
            return Random.Range(num - 15, num + 15);
        }

        public static Vector3 PositionEdgeCheck(Vector3 position, float width)
        {
            if (NavMesh.FindClosestEdge(position, out NavMeshHit navHit, -1) && navHit.distance < width)
            {
                Vector3 position2 = navHit.position;
                if (NavMesh.SamplePosition(new Ray(position2, position - position2).GetPoint(width + 0.5f), out navHit, 10f, -1))
                {
                    position = navHit.position;
                    return position;
                }
                return Vector3.zero;
            }
            return position;
        }
    }
}
