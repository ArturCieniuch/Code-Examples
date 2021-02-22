using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Default", menuName = "Objects/Enemies/Default", order = 1)]
public class DefaultEnemySciptableObject : EnemyScriptableObject {
    public override void Init(EnemyCharacter enemy) {
        enemy.spriteRenderer.sprite = sprite;
        enemy.spriteRenderer.color = color;
        enemy.rigidbody2D.velocity = (PlayerCharacter.instance.transform.position - enemy.transform.position).normalized * speed;
    }
}
