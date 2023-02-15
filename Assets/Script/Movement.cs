using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
  public GameManager gameManager;
  public AudioClip audioJump;
  public AudioClip audioAttack;
  public AudioClip audioDamaged;
  public AudioClip audioItem;
  public AudioClip audioDie;
  public AudioClip audioFinish;

  Rigidbody2D rigid;
  SpriteRenderer spriteRenderer;
  private Animator anim;
  CapsuleCollider2D collision;
  AudioSource audioSource;

  // Move Power
  [SerializeField]
  private float speed = 5.0f;
  [SerializeField]
  private float jumpForce = 12.0f;

  // State Check
  [HideInInspector]
  public bool isGrounded = false;
  [HideInInspector]
  public bool isDamaged = false;

  // Jump Count
  [SerializeField]
  private int maxJumpCount = 2;
  private int currentJumpCount = 0;

  private int dirc = 0;  // 충돌체 방향

  void Awake()
  {
    rigid = GetComponent<Rigidbody2D>();
    spriteRenderer = GetComponent<SpriteRenderer>();
    anim = GetComponent<Animator>();
    collision = GetComponent<CapsuleCollider2D>();
    audioSource = GetComponent<AudioSource>();
  }

  private void Update()
  {
    // Jump as much as you press the key
    if (Input.GetButtonUp("Jump") && rigid.velocity.y > 0)
    {
      rigid.velocity = new Vector2(rigid.velocity.x, 0);
    }
  }

  void FixedUpdate()
  {
    // Landing Ground State
    // 발사위치, 사각형 크기, 회전, 발사방향, 거리
    RaycastHit2D rayHit = Physics2D.BoxCast(rigid.position - new Vector2(0, 0.2f), new Vector2(0.6f, 0.7f), 0, Vector2.down, 1f, LayerMask.GetMask("Platform"));
    if (rayHit.collider != null)  // If the player lands on the ground
    {
      if (rayHit.distance < 1f)
      {
        // Change Ground State On
        isGrounded = true;
        // Initialize the number of jumps available
        if (rigid.velocity.y<=0)  // Prevent initialization on jump
          currentJumpCount = maxJumpCount;
        // Jump Animation Off
        anim.SetBool("isJumping", false);
        anim.SetBool("isMultipleJumping", false);
      }
    }
    // If the player is Air State
    else
    {
      // Change Ground State Off
      isGrounded = false;
      // Jump Animation On
      anim.SetBool("isJumping", true);
      if (currentJumpCount % 2 == 0)
        anim.SetBool("isMultipleJumping", true);
      else
        anim.SetBool("isMultipleJumping", false);
    }

    // Preventing slipping on slopes
    if (isGrounded && !Input.GetButton("Horizontal") && !isDamaged)
      rigid.constraints = ~RigidbodyConstraints2D.FreezePositionY;
    else
      rigid.constraints = RigidbodyConstraints2D.FreezeRotation;

    // Prevent floating when moving from uphill to flat
    if (isGrounded && rigid.velocity.y > 0 && !Input.GetButton("Jump") && !isDamaged)
      rigid.velocity = new Vector2(rigid.velocity.x, 0);
  }

  private void OnCollisionEnter2D(Collision2D collision)
  {
    // Be Attacked
    if (collision.gameObject.tag == "Enemy")
      if (rigid.velocity.y < 0 && transform.position.y > collision.transform.position.y)
        OnAttack(collision.transform);
      else
        OnDamaged(collision.transform.position);
    // Run Into Obstacle
    else if (collision.gameObject.tag == "Obstacle")
      OnDamaged(collision.transform.position);
  }

  private void OnTriggerEnter2D(Collider2D collision)
  {
    // Get Coin
    if (collision.gameObject.tag == "Item")
    {
      // Get Point
      bool isBronze = collision.gameObject.name.Contains("Bronze");
      bool isSilver = collision.gameObject.name.Contains("Silver");
      bool isGold = collision.gameObject.name.Contains("Gold");
      if (isBronze)
        gameManager.stagePoint += 10;
      else if (isSilver)
        gameManager.stagePoint += 50;
      else if (isGold)
        gameManager.stagePoint += 200;

      // Deactive Item
      collision.gameObject.SetActive(false);

      PlaySound("ITEM");
    }
    // Reach Finish
    else if (collision.gameObject.tag == "Finish")
    {
      // Next Stage
      gameManager.NextStage();

      PlaySound("FINISH");
    }
  }

  void OnAttack(Transform enemy)
  {
    // Get Point
    gameManager.stagePoint += 100;

    EnemyMove enemyMove = enemy.GetComponent<EnemyMove>();
    enemyMove.Ondamaged();
    // Player Reaction
    rigid.velocity = new Vector2(rigid.velocity.x, 7f);

    PlaySound("ATTACK");
  }

  void OnDamaged(Vector2 targetPos)
  {
    // Change Layer
    gameObject.layer = 10;
    // View
    spriteRenderer.color = new Color(1, 1, 1, 0.4f);

    // Reaction
    dirc = transform.position.x - targetPos.x > 0 ? 1 : -1;
    rigid.velocity = new Vector2(dirc, 1.5f) * 2;

    // Health Down
    gameManager.HealthDown();

    isDamage();
    PlaySound("DAMAGED");
    // Return Layer and View
    Invoke("offDamaged", 3f);
  }

  void isDamage()
  {
    isDamaged = true;
    Invoke("isNotDamage", 1);
  }

  void isNotDamage()
  {
    isDamaged = false;
  }

  void offDamaged()
  {
    // Change Layer
    gameObject.layer = 9;
    // View
    spriteRenderer.color = new Color(1, 1, 1, 1);
  }

  public void OnDie()
  {
    // Translucent
    spriteRenderer.color = new Color(1, 1, 1, 0.4f);
    // Reation
    spriteRenderer.flipY = true;
    rigid.AddForce(Vector2.up * 3, ForceMode2D.Impulse);
    // No Collider => Fall
    collision.enabled = false;
    // Sound
    StartCoroutine("DieSoundDelay");
  }

  IEnumerator DieSoundDelay()
  {
    yield return new WaitForSeconds(0.02f);
    PlaySound("DIE");
  }

  public void Respawn()
  {
    // Speed Initialization
    rigid.velocity = Vector2.zero;
    // Position Initialization
    rigid.transform.position = new Vector3(0, 2, -1);
    // Health Initialization
    gameManager.health = gameManager.maxHealth;
    // View Initialization
    spriteRenderer.color = new Color(1, 1, 1, 1);
    spriteRenderer.flipY = false;
    // Collision On
    collision.enabled = true;
    // Fall State Off
    gameManager.isFall = false;
    // Health Indication Initialization
    for (int i = 0; i < gameManager.maxHealth; i++)
      gameManager.UIhealth[i].color = new Color(1, 1, 1, 1f);
  }

  public void Move(float x)
  {
    if (!isDamaged)
    {
      rigid.velocity = new Vector2(x * speed, rigid.velocity.y);

      // Curse
      if (rigid.velocity.x != 0)
        spriteRenderer.flipX = rigid.velocity.x < 0;

      // Animator
      if (Mathf.Abs(rigid.velocity.x) < 0.3f)
        anim.SetBool("isWalking", false);
      else
        anim.SetBool("isWalking", true);
    }
  }

  public void Jump()
  {
    if (!isDamaged)
    {
      // Recognize a walk to a cliff as a jump
      if (!isGrounded && currentJumpCount == maxJumpCount)
        currentJumpCount--;

      // Player can more jump
      if (currentJumpCount > 0)
      {
        rigid.velocity = new Vector2(rigid.velocity.x, jumpForce);
        currentJumpCount--;
        PlaySound("JUMP");
      }
    }
  }

  private void PlaySound(string action)
  {
    switch (action)
    {
      case "JUMP":
        audioSource.clip = audioJump;
        break;
      case "ATTACK":
        audioSource.clip = audioAttack;
        break;
      case "DAMAGED":
        audioSource.clip = audioDamaged;
        break;
      case "ITEM":
        audioSource.clip = audioItem;
        break;
      case "DIE":
        audioSource.clip = audioDie;
        break;
      case "FINISH":
        audioSource.clip = audioFinish;
        break;
    }
    audioSource.Play();
  }
}
