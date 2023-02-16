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

  // Move Speed
  [SerializeField]
  private float speed = 5.0f;
  private int pDir = 0; // Player movement direction
  // Jump Power
  [SerializeField]
  private float jumpForce = 12.0f;

  // State Check
  // Determine if the player is invulnerable to damage.
  [HideInInspector]
  public bool isUnbeat = false;
  // Check if the player is stepping on the ground
  [HideInInspector]
  public bool isGrounded = false;
  // Makes it unaffected by other elements when moved by an attack.
  [HideInInspector]
  public bool isDamaged = false;

  // Jump Count
  [SerializeField]
  private int maxJumpCount = 2;
  private int currentJumpCount = 0;

  // Point
  private int bronzeCoin = 10;
  private int silverCoin = 50;
  private int goldCoin = 200;
  private int enemyDefeat = 100;

  // Player Reaction
  Vector2 onDamaged = new Vector2(2, 3);  // Player moves when hit.
  private float stopSpeed = 0.3f;
  private float dieJump = 3.0f; // Player moves when die.
  private int dirc = 0; // Collider Orientation

  // Time
  private float offDamagedTime = 1.5f;  // Player unbeatable time
  private float notDamageTime = 1f;  // Player is able to move after being hit time

  // Layer
  private int playerLayer = 9;
  private int playerDamagedLayer = 10;

  // Player Position Check
  private float downHill = 1.02f;
  private float flatSurface = 0.5f;

  // Player Gravity
  private float downHillGravity = 10.0f;
  private float normalGravity = 2.5f;

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
    // If the player lands on the ground
    if (rayHit.collider != null)
    {
      if (rayHit.distance < 1f)
      {
        // Change Ground State On
        isGrounded = true;
        // Initialize the number of jumps available
        if (rigid.velocity.y <= 0)  // Prevent initialization on jump
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
  }

  private void OnCollisionEnter2D(Collision2D collision)
  {
    if (!isUnbeat)
    {
      switch (collision.gameObject.tag)
      {
        // Hit by Enemy
        case "Enemy":
          OnDamaged(collision.transform.position);
          break;
        // Hit by Obstacle
        case "Obstacle":
          OnDamaged(collision.transform.position);
          break;
      }
    }
    // Player lands on the ground after being hit, Player can move immediately
    else if (collision.gameObject.tag == "Platform")
      isNotDamage();
  }

  private void OnTriggerEnter2D(Collider2D collision)
  {
    switch (collision.gameObject.tag)
    {
      // Attack Enemy
      case "Enemy":
        if (rigid.velocity.y < 0 && transform.position.y > collision.transform.position.y)
          OnAttack(collision.transform);
        break;
      // Get Coin
      case "Item":
        // Get Point
        bool isBronze = collision.gameObject.name.Contains("Bronze");
        bool isSilver = collision.gameObject.name.Contains("Silver");
        bool isGold = collision.gameObject.name.Contains("Gold");
        if (isBronze)
          gameManager.stagePoint += bronzeCoin;
        else if (isSilver)
          gameManager.stagePoint += silverCoin;
        else if (isGold)
          gameManager.stagePoint += goldCoin;

        // Deactive Item
        collision.gameObject.SetActive(false);

        PlaySound("ITEM");
        break;
      // Reach Finish
      case "Finish":
        // Move Next Stage
        gameManager.NextStage();

        PlaySound("FINISH");
        break;
    }
  }

  void OnAttack(Transform enemy)
  {
    // Get Point
    gameManager.stagePoint += enemyDefeat;
    // Enemy Reaction
    EnemyMove enemyMove = enemy.GetComponent<EnemyMove>();
    enemyMove.Ondamaged();
    // Player Reaction
    rigid.velocity = new Vector2(rigid.velocity.x, 7f);

    PlaySound("ATTACK");
  }

  void OnDamaged(Vector2 targetPos)
  {
    // Change Unabeatable State
    isUnbeat = true;
    // Change Layer
    gameObject.layer = playerDamagedLayer;
    // View
    spriteRenderer.color = new Color(1, 1, 1, 0.4f);

    // Player Reaction
    dirc = transform.position.x - targetPos.x > 0 ? 1 : -1;
    rigid.velocity = onDamaged * new Vector2(dirc, 1);

    // Health Down
    gameManager.HealthDown();

    isDamage();
    // Return Layer and View
    Invoke("offDamaged", offDamagedTime);

    PlaySound("DAMAGED");
  }

  // Player is unbeat
  void offDamaged()
  {
    // Change Layer
    gameObject.layer = playerLayer;
    // View
    spriteRenderer.color = new Color(1, 1, 1, 1);
    // Return Beatable State
    isUnbeat = false;
  }

  // Player can't move
  void isDamage()
  {
    isDamaged = true;

    // When a player lands on the ground or stops being knocked back by a hit
    if ((isGrounded && rigid.velocity.y <= 0) || Mathf.Abs(rigid.velocity.x) < stopSpeed)
      isNotDamage();
    else
      Invoke("isNotDamage", notDamageTime);
  }

  // Player can move again
  void isNotDamage()
  {
    isDamaged = false;
  }

  public void OnDie()
  {
    // Translucent
    spriteRenderer.color = new Color(1, 1, 1, 0.4f);
    // Reation
    spriteRenderer.flipY = true;
    rigid.AddForce(Vector2.up * dieJump, ForceMode2D.Impulse);
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
      {
        spriteRenderer.flipX = rigid.velocity.x < 0;
        pDir = rigid.velocity.x > 0 ? 1 : -1;
      }

      // Prevent floating when moving from uphill to flat
      if (isGrounded && rigid.velocity.y > 0 && !Input.GetButton("Jump") && !isDamaged)
        rigid.velocity = new Vector2(rigid.velocity.x, 0);

      // Prevent floating when moving from flat to downhill
      RaycastHit2D hit = Physics2D.Raycast(rigid.position + new Vector2(0.5f * pDir, 0), Vector2.down, 2f, LayerMask.GetMask("Platform"));
      if (hit.distance < downHill && hit.distance > flatSurface && isGrounded)
        rigid.gravityScale = downHillGravity;
      else
        rigid.gravityScale = normalGravity;

      // Animator
      if (Mathf.Abs(rigid.velocity.x) < stopSpeed)
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
