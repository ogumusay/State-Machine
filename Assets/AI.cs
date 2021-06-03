using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AI : MonoBehaviour
{
    public Transform[] waypoints;

    State currentState;
    public Transform player;
    float detectionDegree = 60f;
    public float detectionDistance = 7f;
    public float escapeDistance = 9f;
    Color lineColor = Color.green;

    [SerializeField] Text playerText;
    [SerializeField] Text botText;

    void Start()
    {
        currentState = new Idle(this, player);
    }

    void Update()
    {
        currentState = currentState.Process();       

        DebugDraw();
    }

    void DebugDraw()
    {
        Vector3 rightPosition = transform.position + (transform.forward + Mathf.Tan(Mathf.Deg2Rad * detectionDegree) * transform.right) * 2;
        Vector3 leftPosition = transform.position + (transform.forward - Mathf.Tan(Mathf.Deg2Rad * detectionDegree) * transform.right) * 2;

        Debug.DrawLine(transform.position, rightPosition, lineColor);
        Debug.DrawLine(transform.position, leftPosition, lineColor);

        Vector3 playerPos = new Vector3(player.position.x, transform.position.y, player.position.z);

        float angle = Vector3.Angle(playerPos - transform.position, transform.forward);
        float distance = Vector3.Distance(playerPos, transform.position);

        Debug.DrawLine(playerPos, transform.position, lineColor);

        if (angle <= detectionDegree && distance < detectionDistance)
        {
            lineColor = Color.red;
        }   
        else if(distance > escapeDistance)
        {
            lineColor = Color.green;
        }

        playerText.text = "Distance: " + distance.ToString("F2") + "\nAngle: " + angle.ToString("F2") + "°";
        botText.text = "State: " + currentState.state.ToString();
    }
}
