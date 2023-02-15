using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// 점수와 스테이지 관리
public class GameManager : MonoBehaviour
{
  public int totalPoint;
  public int stagePoint;
  public int stageIndex;
  public int health;
  public int maxHealth = 3;

  public bool isFall;

  public Movement player;
  public GameObject[] Stages;

  // UI
  public Image[] UIhealth;
  public Text UIPoint;
  public Text UIStage;
  public GameObject RestartButton;

  private void Awake()
  {
    health = maxHealth;
  }

  private void Update()
  {
    UIPoint.text = (totalPoint + stagePoint).ToString();
  }

  public void NextStage()
  {
    // Cange Stage
    if (stageIndex < Stages.Length - 1)
    {
      Stages[stageIndex].SetActive(false);
      stageIndex++;
      Stages[stageIndex].SetActive(true);
      player.Respawn();

      UIStage.text = "STAGE " + (stageIndex + 1);
    }
    else
    {
      // Game Clear
      Time.timeScale = 0;

      Text btnText = RestartButton.GetComponentInChildren<Text>();
      btnText.text = "Game Clear";
      RestartButton.SetActive(true);
    }

    // Calculate Point
    totalPoint += stagePoint;
    stagePoint = 0;

  }

  private void OnTriggerEnter2D(Collider2D collision)
  {
    // 낙뎀
    if (collision.gameObject.tag == "Player")
    {
      isFall = true;
      HealthDown();
    }
  }

  public void HealthDown()
  {
    if (health > 1 && !isFall)
    {
      health--;
      UIhealth[health].color = new Color(1, 0, 0, 0.4f);
    }
    else
    {
      for (int i = 0; i < maxHealth; i++)
        UIhealth[i].color = new Color(1, 0, 0, 0.4f);

      player.OnDie();

      Text btnText = RestartButton.GetComponentInChildren<Text>();
      btnText.text = "Game Over";
      RestartButton.SetActive(true);
      StartCoroutine("DelayStopTime");
    }
  }

  IEnumerator DelayStopTime()
  {
    yield return new WaitForSeconds(2f);
    Time.timeScale = 0;
  }

  public void Restart()
  {
    Time.timeScale = 1;
    // Player Respawn
    //player.Respawn();

    SceneManager.LoadScene(0);
  }
}
