using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.ProBuilder;

public class ControllerData : MonoBehaviour
{
    public OVRCameraRig cameraRig;
    public GameObject barbellObject;
    private float verticalRange = 0.025f;
    private float horizontalRange = 0.025f;
    private float maxIncorrectAngle = 15f;
    private float minIncorrectAngle = 0f;
    private int repCounter = 0;
    private bool correctRep = false;
    private int totalReps = 0;
    private int repRange = 3;
    private string[] messageArr = {"great job!", "keep going!", "nice work", "good effort!", "great focus", "well done", "you're on track", "great form!", "good job"};
    private enum BarbellState { Idle, MovingUp, MovingDown, Stopped }
    private BarbellState currentState = BarbellState.Idle;
    private float bottomRange = 1f;
    private Vector3 lastTopPosition = new Vector3(0,0,0);
    private Vector3 lastBottomPosition = new Vector3(0,0,0);
    private Vector3 lastBarbellPos;

    public SkinnedMeshRenderer rightController;
    public SkinnedMeshRenderer leftController;

    public Material material;
    private Color feedbackGood = Color.green;
    private Color feedbackBad = Color.red;
    private ProBuilderMesh pbMesh;
    private Mesh barbellMesh;
    private List<int> rightVertices = new List<int>();
    private List<int> leftVertices = new List<int>();
    private Color[] originalColours;
    
    public GameObject rightTextFeedback;
    public GameObject leftTextFeedback;
    public GameObject middleTextFeedback;
    public TextMeshProUGUI rightText;
    public TextMeshProUGUI leftText;
    public TextMeshProUGUI middleText;
    private Coroutine hideRightCoroutine;
    private Coroutine hideMiddleCoroutine;
    private Coroutine hideLeftCoroutine;
    private float delayHideTimer = 2f;

    public AudioSource rightFeedback;
    public AudioSource leftFeedback;
    private bool isOnSameVerticalLevel = false;
    private bool isOnSameHorizontalLevel = false;

    void Start()
    {
        pbMesh = barbellObject.transform.Find("Container/Bar").GetComponent<ProBuilderMesh>();
        barbellMesh = pbMesh.GetComponent<MeshFilter>().mesh;
        IdentifyVertices();
        InitializeAllVertexColours(feedbackGood);

        rightController.enabled = false;
        leftController.enabled = false;

        lastBarbellPos = barbellObject.transform.Find("Container").position;

        HideRightText(true);
        HideLeftText(true);
        HideMiddleText(true);

        HideRightArrows();
        HideLeftArrows();
    }

    void Update()
    {        
        UpdateBarbellPosition();
        UpdateFeedbackPositions();

        CalculatePrecision();
        DetermineMovement();
    }

    Vector3 GetRightControllerPosition()
    {
        return OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch);
    }

    Vector3 GetLeftControllerPosition()
    {
        return OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);
    }

    void UpdateBarbellPosition()
    {
        var barbell = barbellObject.transform.Find("Container");
        Vector3 rightControllerPos = GetRightControllerPosition();
        Vector3 leftControllerPos = GetLeftControllerPosition();
        Vector3 newPosition = (rightControllerPos + leftControllerPos) / 2;
        barbell.position = newPosition;

        Vector3 direction = leftControllerPos - rightControllerPos;
        barbell.rotation = Quaternion.LookRotation(direction);
    }

    void UpdateFeedbackPositions()
    {
        var barbellRight = barbellObject.transform.Find("Container/Bar/Bar Right");
        var barbellLeft = barbellObject.transform.Find("Container/Bar/Bar Left");

        var rightFeedback = barbellObject.transform.Find("Right");
        var leftFeedback = barbellObject.transform.Find("Left");

        rightFeedback.transform.position = barbellRight.transform.position;
        leftFeedback.transform.position = barbellLeft.transform.position;

        rightFeedback.transform.rotation = new Quaternion(0, 0, 0, 1);
        leftFeedback.transform.rotation = new Quaternion(0, 0, 0, 1);
    }

    void ShowArrow(char side, char direction)
    {
        var path = "";
        if (side.CompareTo('r') == 0) // right
            path = "Right/Right Arrows/";
        else if (side.CompareTo('l') == 0) 
            path = "Left/Left Arrows/";

        switch(direction)
        {
            case 't': // top
            {
                barbellObject.transform.Find(path + "Pointer Up").gameObject.SetActive(true);
                break;
            }
            
            case 'd': // down
            {
                barbellObject.transform.Find(path + "Pointer Down").gameObject.SetActive(true);
                break;
            }

            case 'f': // front
            {
                barbellObject.transform.Find(path + "Pointer Front").gameObject.SetActive(true);
                break;
            }

            case 'b': // back
            {
                barbellObject.transform.Find(path + "Pointer Back").gameObject.SetActive(true);
                break;
            }
        }
    }

    void HideRightText(bool state)
    {
        if (!state && !rightTextFeedback.activeSelf)
        {
            rightTextFeedback.SetActive(true);
        }
        else if (state && rightTextFeedback.activeSelf)
        {
            rightText.text = "";
            rightTextFeedback.SetActive(false);
        }
    }

    void HideMiddleText(bool state)
    {
        // false - don't hide text
        // true - hide text

        if (!state && !middleTextFeedback.activeSelf)
        {
            middleTextFeedback.SetActive(true);
        }
        else if (state && middleTextFeedback.activeSelf)
        {
            middleTextFeedback.SetActive(false);
        }
    }

    void HideLeftText(bool state)
    {
        if (!state && !leftTextFeedback.activeSelf)
        {
            leftTextFeedback.SetActive(true);
        }
        else if (state && leftTextFeedback.activeSelf)
        {
            leftText.text = "";
            leftTextFeedback.SetActive(false);
        }
    }

    void DisplayTextFeedback(char side, string feedback)
    {
        switch (side)
        {
            case 'r':
                HideRightText(false);
                rightText.text = feedback;
                if (hideRightCoroutine != null) StopCoroutine(hideRightCoroutine);
                hideRightCoroutine = StartCoroutine(HideAfterDelay('r'));
                break;

            case 'm':
                HideMiddleText(false);
                middleText.text = feedback;
                if (hideMiddleCoroutine != null) StopCoroutine(hideMiddleCoroutine);
                hideMiddleCoroutine = StartCoroutine(HideAfterDelay('m'));
                break;

            case 'l':
                HideLeftText(false);
                leftText.text = feedback;
                if (hideLeftCoroutine != null) StopCoroutine(hideLeftCoroutine);
                hideLeftCoroutine = StartCoroutine(HideAfterDelay('l'));
                break;
        }
    }

    IEnumerator HideAfterDelay(char side)
    {
        yield return new WaitForSeconds(delayHideTimer);

        switch (side)
        {
            case 'r':
                Debug.Log("hide right");
                HideRightText(true);
                break;

            case 'm':
                HideMiddleText(true);
                break;

            case 'l':
                Debug.Log("hide left");
                HideLeftText(true);
                break;
        }
    }

    void HideRightArrows()
    {
        var arrows = barbellObject.transform.Find("Right/Right Arrows");
        foreach (Transform child in arrows)
        {
            child.gameObject.SetActive(false);
        }
    }

    void HideLeftArrows()
    {
        var arrows = barbellObject.transform.Find("Left/Left Arrows");
        foreach (Transform child in arrows) 
        {
            child.gameObject.SetActive(false);
        }
    }

    void CalculatePrecision()
    {
        HorizontalPrecision();
        VerticalPrecision();
    }

    void HorizontalPrecision()
    {
        float zRightUpperRange = GetRightControllerPosition().z + horizontalRange;
        float zRightLowerRange = GetRightControllerPosition().z - horizontalRange;
        float currRightPos = GetRightControllerPosition().z;
        float zLeftLowerRange = GetLeftControllerPosition().z - horizontalRange;
        float currLeftPos = GetLeftControllerPosition().z;

        Vector3 direction = GetLeftControllerPosition() - GetRightControllerPosition();
        float angleRadians = Mathf.Atan2(direction.z, direction.x);
        float angleDegrees = angleRadians * Mathf.Rad2Deg;

        if ((zRightLowerRange <= currLeftPos) && (currLeftPos <= zRightUpperRange))
        {
            if (!isOnSameHorizontalLevel)
            {
                material.SetColor("_Right", feedbackGood);
                material.SetColor("_Left", feedbackGood);

                isOnSameHorizontalLevel = true;
                ResetVertexColours();

                HideRightArrows();
                HideLeftArrows();
                
                HideRightText(true);
                HideLeftText(true);
            }
        }

        else
        {
            correctRep = false;

            if (zRightLowerRange > currLeftPos) // right side is too back
            {
                isOnSameHorizontalLevel = false;
                ShowArrow('r', 'b');
                angleDegrees *= -1;
                AdjustAudioFeedback(1, angleDegrees);
                AdjustBarbellColour(1, angleDegrees);
                DisplayTextFeedback('r', "Move the right side of your barbell forwards");                    
            }

            else if (zLeftLowerRange > currRightPos) // left side is too back
            {
                isOnSameHorizontalLevel = false;
                ShowArrow('l', 'b');
                angleDegrees *= -1;
                AdjustAudioFeedback(0, angleDegrees);
                AdjustBarbellColour(0, angleDegrees);
                DisplayTextFeedback('l', "Move the left side of your barbell fowards");                    
            }
        }
    }

    void VerticalPrecision()
    {
        float yRightUpperRange = GetRightControllerPosition().y + verticalRange;
        float yRightLowerRange = GetRightControllerPosition().y - verticalRange;
        float currRightPos = GetRightControllerPosition().y;
        float yLeftLowerRange = GetLeftControllerPosition().y - verticalRange;
        float currLeftPos = GetLeftControllerPosition().y;

        Vector3 direction = GetLeftControllerPosition() - GetRightControllerPosition();
        float angleRadians = Mathf.Atan2(direction.y, direction.x);
        float angleDegrees = angleRadians * Mathf.Rad2Deg;

        if ((yRightLowerRange <= currLeftPos) && (currLeftPos <= yRightUpperRange)) // Right and left side are on same level
        {
            if (!isOnSameVerticalLevel)
            {
                isOnSameVerticalLevel = true;
                ResetVertexColours();
                ClearAudio();

                HideRightArrows();
                HideLeftArrows();

                HideRightText(true);
                HideLeftText(true);
            }
        }

        else 
        {
            correctRep = false;

            if (yRightLowerRange > currLeftPos) // left side is low
            {
                isOnSameVerticalLevel = false;
                ShowArrow('l', 'd');
                AdjustAudioFeedback(0, angleDegrees);
                AdjustBarbellColour(0, angleDegrees);
                DisplayTextFeedback('l', "Move the left side of your barbell down");
            }

            else if (yLeftLowerRange > currRightPos) // right side is low
            {
                isOnSameVerticalLevel = false;
                ShowArrow('r', 'd');
                AdjustAudioFeedback(1, angleDegrees);
                AdjustBarbellColour(1, angleDegrees);
                DisplayTextFeedback('r', "Move the right side of your barbell down");                    
            }
        }
    }

    void DetermineMovement()
    {
        Vector3 currentPosition = barbellObject.transform.Find("Container").position;

        switch (currentState)
        {
            case BarbellState.Idle:
                if (IsMovingUp(currentPosition, lastBarbellPos))
                {
                    currentState = BarbellState.MovingUp;
                }
                break;

            case BarbellState.MovingUp:
                if (IsMovingDown(currentPosition, lastBarbellPos))
                {
                    currentState = BarbellState.MovingDown;
                }
                else
                {
                    lastTopPosition = currentPosition;
                }
                break;

            case BarbellState.MovingDown:
                if (IsStationary(currentPosition, lastBarbellPos) && IsAtBottom(currentPosition))
                {
                    lastBottomPosition = currentPosition;
                    currentState = BarbellState.Stopped;
                    CompleteRep();
                }
                break;

            case BarbellState.Stopped:
                if (!IsStationary(currentPosition, lastBarbellPos))
                {
                    currentState = BarbellState.Idle;
                }
                break;
        }

        lastBarbellPos = currentPosition;
    }

    void CompleteRep()
    {
        float distance = Vector3.Distance(lastBottomPosition, lastTopPosition);
        if (distance < 0.2f)
        {
            return;
        }

        if (correctRep)
        {
            repCounter += 1;
            totalReps += 1;

            if (repCounter == repRange)
            {
                repCounter = 0;
                repRange += 2;
                int index  = Random.Range(0, messageArr.Length);
                string newFeedback = totalReps.ToString() + " flawless reps so far, " + messageArr[index];
                DisplayTextFeedback('m', newFeedback);
            }
        }
        else
        {
            repCounter = 0;
            correctRep = true;
        }
    }

    bool IsMovingUp(Vector3 currentPosition, Vector3 lastPosition)
    {
        return currentPosition.y > lastPosition.y;
    }

    bool IsMovingDown(Vector3 currentPosition, Vector3 lastPosition)
    { 
        return currentPosition.y < lastPosition.y;
    }

    bool IsStationary(Vector3 currentPosition, Vector3 lastPosition)
    {
        float range = 0.0015f;
        bool condition1 = lastPosition.y - range <= currentPosition.y && currentPosition.y < lastPosition.y + range;
        bool condition2 = lastPosition.z - range <= currentPosition.z && currentPosition.z < lastPosition.z + range;
        return condition1 && condition2;
    }

    private bool IsAtBottom(Vector3 currentPosition)
    {
        return currentPosition.y < bottomRange;
    }

    void AdjustAudioFeedback(int side, float angleDifference)
    {
        // --- Volume ---
        // min - 0.0f
        // max - 1.0f

        if (angleDifference < 0)
            angleDifference *= -1;
        angleDifference = Mathf.Abs(angleDifference);
        float volume = Mathf.InverseLerp(minIncorrectAngle, maxIncorrectAngle, angleDifference) / 2;

        switch (side)
        {
            case 0: // left side
                leftFeedback.volume = volume;

                if (volume > 0 && !leftFeedback.isPlaying)
                    leftFeedback.Play();
                else if (volume == 0)
                    leftFeedback.Stop();
                break;

            case 1: // right side
                rightFeedback.volume = volume;

                if (volume > 0 && !rightFeedback.isPlaying)
                    rightFeedback.Play();
                else if (volume == 0)
                    rightFeedback.Stop();
                break;
        }
    }

    void ClearAudio()
    {
        leftFeedback.Stop();
        leftFeedback.volume = 0;

        rightFeedback.Stop();
        rightFeedback.volume = 0;
    }

    void InitializeAllVertexColours(Color colour)
    {
        Color[] colours = new Color[barbellMesh.vertexCount];

        for (int i = 0; i < colours.Length; i++)
            colours[i] = colour;
        barbellMesh.colors = colours;
        originalColours = (Color[])colours.Clone();
    }

    void IdentifyVertices()
    {
        Vector3[] barbellVertices = barbellMesh.vertices;

        rightVertices.Clear();
        leftVertices.Clear();

        for (int i = 0; i < barbellMesh.vertexCount; i++)
        {
            if (barbellVertices[i].x <= 0)
                rightVertices.Add(i);
            if (barbellVertices[i].x >= 0)
                leftVertices.Add(i);
        }
    }

    void AdjustBarbellColour(int side, float angle)
    {
        float lerpFactor = 0;
        Color[] colours = (Color[])originalColours.Clone();

        switch (side)
        {
            case 0: // left
                lerpFactor = Mathf.InverseLerp(0, maxIncorrectAngle, -angle);
                foreach (int vertex in leftVertices)
                    colours[vertex] = Color.Lerp(colours[vertex], feedbackBad, lerpFactor);
                break;
            
            case 1: // right
                lerpFactor = Mathf.InverseLerp(0, maxIncorrectAngle, angle);
                foreach (int vertex in rightVertices)
                    colours[vertex] = Color.Lerp(colours[vertex], feedbackBad, lerpFactor);
                break;
        }

        barbellMesh.colors = colours;
    }

    void ResetVertexColours()
    {
        barbellMesh.colors = originalColours;
    }
}