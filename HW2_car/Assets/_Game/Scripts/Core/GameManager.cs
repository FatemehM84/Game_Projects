using UnityEngine;
using UnityEngine.SceneManagement;
namespace NeonHighway
{
    public enum GameState
    {
        Menu, Playing, Paused, GameOver
    }
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance
        {
            get;
            private set;
        }
        public GameConfig config;
        public PlayerCar player;
        public HighwayWorld world;
        public GameUI ui;
        public GameAudio audioSystem;
        public Effects effects;
        public GameState State
        {
            get;
            private set;
        }
        public int Lives
        {
            get;
            private set;
        }
        public float Fuel
        {
            get;
            private set;
        }
        public float Elapsed
        {
            get;
            private set;
        }
        public float Distance
        {
            get;
            private set;
        }
        public int Coins
        {
            get;
            private set;
        }
        public int Score=>Mathf.FloorToInt(Distance)+Coins*config.coinValue;
        public int HighScore
        {
            get;
            private set;
        }
        public float Speed=>config.Speed(Elapsed);
        public bool IsPlaying=>State==GameState.Playing;
        float invulnerableUntil;
        const string HighScoreKey="NeonHighway.HighScore.v1";
        void Awake()
        {
            if(Instance!=null&&Instance!=this)
            {
                Destroy(gameObject);
                return;
            }
            Instance=this;
            Time.timeScale=1;
            HighScore=PlayerPrefs.GetInt(HighScoreKey,0);
            Lives=3;
            Fuel=config.fuelCapacity;
        }
        void Start()
        {
            world.Initialize();
            State=SceneManager.GetActiveScene().name=="MainMenu"?GameState.Menu:GameState.Playing;
            ui.RefreshState();
        }
        void Update()
        {
            if(!IsPlaying)return;
            Elapsed+=Time.deltaTime;
            Distance+=Speed*Time.deltaTime;
            Fuel=Mathf.Max(0,Fuel-config.fuelDrain*Time.deltaTime);
            if(Fuel<=0)EndGame();
        }
        public void StartGame()
        {
            SaveScore();
            Time.timeScale=1;
            SceneManager.LoadScene("Gameplay");
        }
        public void TogglePause()
        {
            if(State==GameState.Playing)
            {
                State=GameState.Paused;
                Time.timeScale=0;
            }
            else if(State==GameState.Paused)
            {
                State=GameState.Playing;
                Time.timeScale=1;
            }
            ui.RefreshState();
        }
        public void MainMenu()
        {
            SaveScore();
            Time.timeScale=1;
            SceneManager.LoadScene("MainMenu");
        }
        public void Damage()
        {
            if(!IsPlaying||Elapsed<invulnerableUntil)return;
            Lives=Mathf.Max(0,Lives-1);
            invulnerableUntil=Elapsed+config.damageCooldown;
            audioSystem.PlayImpact();
            effects.Impact(player.transform.position);
            player.Shake();
            if(Lives==0)EndGame();
        }
        public bool Invulnerable=>IsPlaying&&Elapsed<invulnerableUntil;
        public void CollectCoin(Vector3 position)
        {
            if(!IsPlaying)return;
            Coins++;
            audioSystem.PlayCoin();
            effects.Pickup(position,Color.yellow);
        }
        public void CollectFuel(Vector3 position)
        {
            if(!IsPlaying)return;
            Fuel=Mathf.Clamp(Fuel+config.fuelPickup,0,config.fuelCapacity);
            audioSystem.PlayFuel();
            effects.Pickup(position,Color.green);
        }
        public void EndGame()
        {
            if(!IsPlaying)return;
            State=GameState.GameOver;
            SaveScore();
            ui.RefreshState();
        }
        public void SaveScore()
        {
            if(Score>HighScore)
            {
                HighScore=Score;
                PlayerPrefs.SetInt(HighScoreKey,HighScore);
                PlayerPrefs.Save();
            }
        }
        public void Exit()
        {
            SaveScore();
            Application.Quit();
        }
        void OnApplicationPause(bool paused)
        {
            if(paused&&IsPlaying)TogglePause();
        }
        void OnApplicationQuit()=>SaveScore();
        void OnDestroy()
        {
            if(Instance==this)
            {
                Instance=null;
                Time.timeScale=1;
            }
        }
    }
}
