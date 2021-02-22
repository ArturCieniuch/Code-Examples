using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class GameController : MonoBehaviour {
    public static GameController instance;
    public bool isGameRunning;

    [Header("Spawns")]
    public GameObject leftSpawn;
    public GameObject centerSpawn;
    public GameObject rightSpawn;

    [Header("Points")]
    public GameObject pointPrefab;
    [Tooltip("Maximum amount of points that can be present on level at the same time")]
    public int maxSpawnedPoints;
    public int pointSpawnRate;

    private int spawnedPointsCount;

    private LevelScriptableObject activeLevel;

    private Coroutine leftSpawnCoroutine;
    private Coroutine centerSpawnCoroutine;
    private Coroutine rightSpawnCoroutine;
    private Coroutine pointSpawnerCoroutine;

    private int currentLeftSpawnPointThreshold;
    private int currentCenterSpawnPointThreshold;
    private int currentRightSpawnPointThreshold;

    private int collectedPoints;

    private void Awake() {
        instance = this;
    }

    public void LoadLevel(LevelScriptableObject level) {
        activeLevel = level;
        collectedPoints = 0;
        currentLeftSpawnPointThreshold = 0;
        currentCenterSpawnPointThreshold = 0;
        currentRightSpawnPointThreshold = 0;

        isGameRunning = true;

        pointSpawnerCoroutine = StartCoroutine(pointSpawner());
    }

    private IEnumerator PointSpawner() {
        float timer = 0;

        while (true) {
            if (!isGameRunning || spawnedPointsCount >= maxSpawnedPoints) {
                yield return null;
                continue;
            }

            if (timer < pointSpawnRate) {
                timer += Time.deltaTime;
                yield return null;
                continue;
            }

            timer = 0;

            GameObject point = Instantiate(pointPrefab, transform);
            point.transform.position = new Vector3(Random.Range(-2f, 2f), Random.Range(-4f, 2f));
            spawnedPointsCount++;
        }
    }

    public void OnPointCollected() {
        spawnedPointsCount--;

        if (++collectedPoints >= activeLevel.pointTarget) {
            return;
        }

        CheckEnemiesToSpawn();
    }

    private void CheckEnemiesToSpawn() {
        foreach (SpawnSettings spawnSettings in activeLevel.enemies) {
            if (spawnSettings.pointThreshold > collectedPoints) {
                continue;
            }

            switch (spawnSettings.selectedSpawn) {
                case Spawns.LEFT:
                    if (spawnSettings.pointThreshold > currentLeftSpawnPointThreshold) {
                        if (leftSpawnCoroutine != null) {
                            StopCoroutine(leftSpawnCoroutine);
                        }

                        currentLeftSpawnPointThreshold = spawnSettings.pointThreshold;
                        leftSpawnCoroutine = StartCoroutine(SpawnEnemiesCoroutine(spawnSettings));
                    }
                    break;
                case Spawns.CENTER:
                    if (spawnSettings.pointThreshold > currentCenterSpawnPointThreshold) {
                        if (centerSpawnCoroutine != null) {
                            StopCoroutine(centerSpawnCoroutine);
                        }

                        currentCenterSpawnPointThreshold = spawnSettings.pointThreshold;
                        centerSpawnCoroutine = StartCoroutine(SpawnEnemiesCoroutine(spawnSettings));
                    }
                    break;
                case Spawns.RIGHT:
                    if (spawnSettings.pointThreshold > currentRightSpawnPointThreshold) {
                        if (rightSpawnCoroutine != null) {
                            StopCoroutine(rightSpawnCoroutine);
                        }

                        currentRightSpawnPointThreshold = spawnSettings.pointThreshold;
                        rightSpawnCoroutine = StartCoroutine(SpawnEnemiesCoroutine(spawnSettings));
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private IEnumerator SpawnEnemiesCoroutine(SpawnSettings spawnSettings) {
        float timer = 0;

        while (true) {
            if (!isGameRunning) {
                yield return null;
                continue;
            }

            if (timer < spawnSettings.spawnRate) {
                timer += Time.deltaTime;
                yield return null;
                continue;
            }

            timer = 0;

            EnemyCharacter enemyInstance = Instantiate(Resources.Load<EnemyCharacter>("Prefabs/EnemyCharacter"));
            enemyInstance.transform.position = GetSpawn(spawnSettings.selectedSpawn).position;
            enemyInstance.Init(spawnSettings.enemy);
        }
    }

    private Transform GetSpawn(Spawns spawn) {
        switch (spawn) {
            case Spawns.LEFT:
                return leftSpawn.transform;
            case Spawns.CENTER:
                return centerSpawn.transform;
            case Spawns.RIGHT:
                return rightSpawn.transform;
            default:
                throw new ArgumentOutOfRangeException(nameof(spawn), spawn, null);
        }
    }
}

public enum Spawns {
    LEFT,
    CENTER,
    RIGHT
}