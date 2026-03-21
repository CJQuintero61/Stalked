/*
    MainMenu.cs
    Created by: Christian Quintero
    Created on: 03/21/2026

    This script is used to control the main menu buttons.
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class MainMenu : MonoBehaviour
{
    public void Play()
    {
        // load the next scene in the build index
        // this takes us from the main menu to the start of the game
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void Quit()
    {
        // when testing in the editor, this doesn't actually quit the game
        // so a debug is printed to test
        Application.Quit();
        Debug.Log("Player has quit the game.");
    }
}
