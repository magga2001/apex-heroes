using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelLoader : MonoBehaviour
{
    [SerializeField] private Animator[] transition;

    [SerializeField] private float time;

    public void LoadingLevel(int index)
    {
        StartCoroutine(LoadLevel(index));
    }

    //Transition to new scene
    IEnumerator LoadLevel(int index)
    {

        for (int i = 0; i < transition.Length; i++)
        {
            transition[i].SetTrigger("Start");
        }

        yield return new WaitForSeconds(time);

        Debug.Log("Loaded");
        SceneManager.LoadScene(index);
    }

}
