using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Pausemenu : MonoBehaviour
{
    public static bool GameIsPaused = false;

    public GameObject pauseCanvas;

    public GameObject mainCanvas;

    public LevelLoader levelLoader;

    private void Start()
    {

        //mainCanvas.SetActive(false);
    }

    void Update()
    {
        //if (GameIsPaused)
        //{
            //pauseCanvas.SetActive(true);
            //Time.timeScale = 0f;
        //}
        //else
        //{
            //pauseCanvas.SetActive(false);
            //Time.timeScale = 1f;
        //}

        //if (GameManager.Instance.GameIsOver)
        //{
            //mainCanvas.SetActive(false);
        //}
        //else
        //{
            //mainCanvas.SetActive(true);
        // }

    }

    public void Resume()
    {
        pauseCanvas.SetActive(false);
        mainCanvas.SetActive(true);
        //GameIsPaused = false;
    }

    public void Pause()
    {
        pauseCanvas.SetActive(true);
        //GameIsPaused = true;
    }

    public void LoadMenu()
    {
        GameIsPaused = false;
        levelLoader.LoadingLevel(SceneName.mainmenu);
    }

    public void RestartGame()
    {
        //AudioManager.instance.Play("ButtonClick");

        GameIsPaused = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnApplicationFocus(bool focus)
    {
        //SaveManager.SavePlayerInfo();

        //if (focus == false && !GameManager.Instance.GameIsOver)
        //{
            //GameIsPaused = true;
        //}
    }

}