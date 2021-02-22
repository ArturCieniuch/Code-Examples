using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EnemyScriptableObject : ScriptableObject {
    public Vector2 size;
    public int damage;
    public float speed;
    public Sprite sprite;
    public Color color;

    public abstract void Init(EnemyCharacter enemyCharacter);
}
