using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controls an ending cutscene where a car drives forward and the camera smoothly follows it.
/// Attach this script to an empty GameObject in your scene named "CutsceneManager".
/// </summary>
public class EndingCutsceneController : MonoBehaviour
{
    [Header("Car Settings")]
    [Tooltip("Drag your Car GameObject here.")]
    public Transform car;
    
    [Tooltip("How fast the car drives down the road.")]
    public float carSpeed = 15f;

    [Tooltip("The local direction the car moves in. Default is (0, 0, 1) for forward.")]
    public Vector3 moveDirection = new Vector3(0, 0, 1);

    [Header("Camera Settings")]
    [Tooltip("Drag your Main Camera here.")]
    public Transform sceneCamera;

    [Tooltip("If true, uses the Camera Offset below. If false, uses the camera's starting position in the scene relative to the car.")]
    public bool useCustomCameraOffset = true;
    
    [Tooltip("The position of the camera relative to the car (X: left/right, Y: up/down, Z: forward/back).")]
    public Vector3 cameraOffset = new Vector3(0f, 4f, -10f);
    
    [Tooltip("How smoothly the camera catches up to the car. Lower is smoother, higher is more rigid.")]
    public float cameraSmoothSpeed = 3f;
    
    [Tooltip("Check this if you want the camera to always tilt to look directly at the car.")]
    public bool lookAtCar = true;

    [Header("Cutscene Settings")]
    [Tooltip("How long the cutscene lasts in seconds.")]
    public float cutsceneDuration = 10f;
    
    [Tooltip("The exact name of the Scene you want to load when it finishes.")]
    public string mainMenuSceneName = "MainMenu";

    [Header("Cutscene State")]
    public bool isPlaying = true;
    
    private float timer = 0f;

    void Start()
    {
        // If we are not using a custom offset, calculate the initial offset based on where the camera is placed in the scene
        if (!useCustomCameraOffset && car != null && sceneCamera != null)
        {
            cameraOffset = car.InverseTransformDirection(sceneCamera.position - car.position);
        }
    }

    // Update is called once per frame. We use this for movement.
    void Update()
    {
        if (isPlaying && car != null)
        {
            // Manage the cutscene timer
            timer += Time.deltaTime;
            if (timer >= cutsceneDuration)
            {
                EndCutscene();
                return; // Stop executing movement for this frame
            }

            // Move the car along its chosen local direction
            car.Translate(moveDirection.normalized * carSpeed * Time.deltaTime);
        }
    }

    // LateUpdate is called after all Update functions have been called.
    // It is best practice to move the camera in LateUpdate so it perfectly tracks objects that moved in Update.
    void LateUpdate()
    {
        if (isPlaying && car != null && sceneCamera != null)
        {
            // 1. Calculate where the camera SHOULD be based on the car's rotation and our chosen offset
            Vector3 desiredPosition = car.position + car.TransformDirection(cameraOffset);

            // 2. Smoothly move the camera from its current position to the desired position
            sceneCamera.position = Vector3.Lerp(sceneCamera.position, desiredPosition, cameraSmoothSpeed * Time.deltaTime);

            // 3. Handle camera rotation (Looking at the car)
            if (lookAtCar)
            {
                // We create a target point slightly ahead of the car's center based on movement direction
                Vector3 lookTarget = car.position + (car.TransformDirection(moveDirection.normalized) * 3f); 
                
                // Calculate the rotation needed to look at that target
                Quaternion targetRotation = Quaternion.LookRotation(lookTarget - sceneCamera.position);
                
                // Smoothly rotate the camera towards that target
                sceneCamera.rotation = Quaternion.Slerp(sceneCamera.rotation, targetRotation, cameraSmoothSpeed * Time.deltaTime);
            }
        }
    }
    
    // Optional: A public method to trigger the cutscene from another script (like a UI button or trigger zone)
    public void StartCutscene()
    {
        isPlaying = true;
    }

    public void StopCutscene()
    {
        isPlaying = false;
    }

    public void EndCutscene()
    {
        isPlaying = false;
        
        // Try to load the specified scene
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            Debug.LogWarning("Cutscene ended, but no Main Menu Scene Name was provided!");
        }
    }
}