using TMPro;using UnityEngine;using UnityEngine.UI;
namespace NeonHighway {
public sealed class GameUI : MonoBehaviour {
 public GameObject menu,hud,pause,gameOver; public TMP_Text speed,score,highScore,lives,fuelText,finalScore;public Image fuelFill,damageFlash;float shownFuel=1;
 public void RefreshState(){var gm=GameManager.Instance;menu.SetActive(gm.State==GameState.Menu);hud.SetActive(gm.State!=GameState.Menu);pause.SetActive(gm.State==GameState.Paused);gameOver.SetActive(gm.State==GameState.GameOver);finalScore.text="SCORE  "+gm.Score.ToString("N0")+"\nBEST  "+gm.HighScore.ToString("N0");}
 void Update(){var gm=GameManager.Instance;if(gm==null)return;speed.text=Mathf.RoundToInt(gm.Speed*3.6f)+" <size=18>KM/H</size>";score.text=gm.Score.ToString("000000");highScore.text="BEST  "+Mathf.Max(gm.Score,gm.HighScore).ToString("N0");lives.text="LIVES  "+gm.Lives;fuelText.text="FUEL  "+Mathf.CeilToInt(gm.Fuel)+"%";shownFuel=Mathf.MoveTowards(shownFuel,gm.Fuel/gm.config.fuelCapacity,Time.unscaledDeltaTime*.5f);fuelFill.fillAmount=shownFuel;fuelFill.color=gm.Fuel<25?Color.Lerp(new Color(1,.12f,.3f),new Color(1,.6f,.15f),Mathf.PingPong(Time.unscaledTime*2,1)):new Color(1,.69f,.28f);damageFlash.color=new Color(1,.02f,.15f,gm.Invulnerable?.1f*(.5f+.5f*Mathf.Sin(Time.time*20)):0);}
 public void StartGame()=>GameManager.Instance.StartGame();public void Resume()=>GameManager.Instance.TogglePause();public void MainMenu()=>GameManager.Instance.MainMenu();public void Exit()=>GameManager.Instance.Exit();
}
}