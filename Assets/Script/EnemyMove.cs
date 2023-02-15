using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMove : MonoBehaviour
{
  Rigidbody2D rigid;
  public int nextMove;
  Animator anim;
  SpriteRenderer spriteRenderer;
  CapsuleCollider2D collision;

  void Awake()
  {
    rigid = GetComponent<Rigidbody2D>();
    anim = GetComponent<Animator>();
    spriteRenderer = GetComponent<SpriteRenderer>();
    collision = GetComponent<CapsuleCollider2D>();

    Invoke("Think", 1);
  }

  void FixedUpdate()
  {
    // Move
    rigid.velocity = new Vector2(nextMove, rigid.velocity.y);

    // Platform Check
    Vector2 frontVec = new Vector2(rigid.position.x + nextMove * 0.4f, rigid.position.y);
    RaycastHit2D hit = Physics2D.Raycast(frontVec, Vector2.down, 1.3f, LayerMask.GetMask("Platform"));
    if (hit.collider == null)
    {
      Turn();
    }

    // Preventing slipping on slopes
    if (nextMove != 0)
      rigid.constraints = RigidbodyConstraints2D.FreezeRotation;
    else
      rigid.constraints = ~RigidbodyConstraints2D.FreezePositionY;
  }

  // Auto Move
  void Think()
  {
    // Setting the direction of Enemy movement
    nextMove = Random.Range(-1, 2);

    // animation
    anim.SetInteger("WalkSpeed", nextMove);
    if (nextMove != 0)
      spriteRenderer.flipX = nextMove > 0;

    // Set reorientation time to random
    float nextTinkTime = Random.Range(0.5f, 1.5f);
    Invoke("Think", nextTinkTime);
  }

  // Preventing Enemy drops
  void Turn()
  {
    nextMove *= -1;
    if (nextMove != 0)
      spriteRenderer.flipX = nextMove > 0;
  }

  public void Ondamaged()
  {
    // Translucent
    spriteRenderer.color = new Color(1, 1, 1, 0.4f);
    // Reaction
    spriteRenderer.flipY = true;
    rigid.AddForce(Vector2.up * 3, ForceMode2D.Impulse);
    // No Collider => Fall
    collision.enabled = false;
    // Disappeared
    Invoke("DeActive", 3);
  }

  private void DeActive()
  {
    gameObject.SetActive(false);
  }
}
