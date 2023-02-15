using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
  private Movement movement2D;
  public Vector2 move;

  void Awake()
  {
    movement2D = GetComponent<Movement>();
  }

  void Update()
  {
    // Jump
    if (Input.GetButtonDown("Jump"))
    {
      movement2D.Jump();
    }
  }


  private void FixedUpdate()
  {
    // Move By Key Control
    move.x = Input.GetAxis("Horizontal");
    //if (Input.GetButton("Horizontal"))
      movement2D.Move(move.x);
  }
}
