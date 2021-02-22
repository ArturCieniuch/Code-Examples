using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DefaultLevel", menuName = "Objects/Levels/Default", order = 1)]
public class LevelScriptableObject : ScriptableObject {
    public int pointTarget;
    public List<SpawnSettings> enemies;
}

[Serializable]
public class SpawnSettings {
    public Spawns selectedSpawn;
    public EnemyScriptableObject enemy;
    public int pointThreshold;
    public float spawnRate;
}