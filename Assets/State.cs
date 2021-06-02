using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public abstract class State
{
    public enum STATE {
        IDLE, PATROL, ATTACK, CHASE
    }

    public enum EVENT {
        ENTER, UPDATE, EXIT 
    }

    public STATE state;
    public EVENT currentEvent;
    public State nextState;
    public AI controller;
    public Transform player;
   
    public State(AI controller, Transform player)
    {
        this.controller = controller;
        this.player = player;
    }

    public State Process()
    {
        if (currentEvent == EVENT.ENTER)
            Enter();
        else if (currentEvent == EVENT.UPDATE)
            Update();
        else if (currentEvent == EVENT.EXIT)
        {
            Exit();
            return nextState;
        }

        return this;
    }

    public virtual void Enter()
    {
        currentEvent = EVENT.UPDATE;        
    }

    public virtual void Update()
    {

    }

    public virtual void Exit()
    {

    }
}

public class Idle : State
{
    public int idleTime = 0;
    public float timer = 0f;

    public float detectionDegree = 60f;
    public float detectionDistance = 7f;

    public Idle(AI controller, Transform player) : base (controller, player)
    {
        state = STATE.IDLE;
    }

    public override void Enter()
    {
        idleTime = Random.Range(1, 4);
        base.Enter();
    }

    public override void Update()
    {
        timer += Time.deltaTime;
        if(timer >= idleTime)
        {
            nextState = new Patrol(controller, player);
            currentEvent = EVENT.EXIT;
        }
        else if(Input.GetKeyDown(KeyCode.A))
        {
            nextState = new Attack(controller, player);
            currentEvent = EVENT.EXIT;
        }
        
        DistanceDetection();

        base.Update();
    }

    public void DistanceDetection()
    {
        Vector3 playerPos = new Vector3(player.position.x, controller.transform.position.y, player.position.z);

        float angle = Vector3.Angle(playerPos - controller.transform.position, controller.transform.forward);
        float distance = Vector3.Distance(playerPos, controller.transform.position);

        if (angle <= detectionDegree && distance < detectionDistance)
        {
            //DETECTED
            nextState = new Chase(controller, player);
            currentEvent = EVENT.EXIT;
        }        
    }
    public override void Exit()
    {
        Debug.Log("IDLE: Exit");
        base.Exit();
    }
}

public class Patrol : State
{
    public int waypointIndex;
    public float speed = 5f;
    public float rotationSpeed = 4f;

    public float detectionDegree = 60f;
    public float detectionDistance = 7f;

    public Patrol(AI controller, Transform player) : base (controller, player)
    {
        state = STATE.PATROL;
    }

    public override void Enter()
    {
        waypointIndex = Random.Range(0, 4);
        Debug.Log("PATROL: Enter");
        base.Enter();
    }

    public void PatrolWaypoint()
    {
        //Patrol
        if (Vector3.Distance(controller.transform.position, controller.waypoints[waypointIndex].position) < 2f)
        {
            int previousIndex = waypointIndex;
            waypointIndex = Random.Range(0, 4);

            if (previousIndex == waypointIndex)
            {
                waypointIndex++;

                if (waypointIndex >= controller.waypoints.Length)
                {
                    waypointIndex = 0;
                }
            }

            if(Random.Range(0, 100) < 40)
            {
                nextState = new Idle(controller, player);
                currentEvent = EVENT.EXIT;
                return;
            }
        }

        controller.transform.Translate(controller.transform.forward * speed * Time.deltaTime, Space.World);

        Quaternion lookAtWP = Quaternion.LookRotation(controller.waypoints[waypointIndex].position - controller.transform.position);
        controller.transform.rotation = Quaternion.Slerp(controller.transform.rotation, lookAtWP, Time.deltaTime * rotationSpeed);        
    }

    public void DistanceDetection()
    {
        Vector3 playerPos = new Vector3(player.position.x, controller.transform.position.y, player.position.z);

        float angle = Vector3.Angle(playerPos - controller.transform.position, controller.transform.forward);
        float distance = Vector3.Distance(playerPos, controller.transform.position);

        if (angle <= detectionDegree && distance < detectionDistance)
        {
            //DETECTED
            nextState = new Chase(controller, player);
            currentEvent = EVENT.EXIT;
        }        
    }

    public override void Update()
    {
        if(Input.GetKeyDown(KeyCode.I))
        {
            nextState = new Idle(controller, player);
            currentEvent = EVENT.EXIT;
        }
        else if(Input.GetKeyDown(KeyCode.A))
        {
            nextState = new Attack(controller, player);
            currentEvent = EVENT.EXIT;
        }

        PatrolWaypoint();
        DistanceDetection();

        base.Update();
    }

    public override void Exit()
    {
        Debug.Log("PATROL: Exit");
        base.Exit();
    }
}

public class Attack : State
{
    public float attackDistance = 3f;
    public float attackDegree = 20f;

    public Attack(AI controller, Transform player) : base (controller, player)
    {
        state = STATE.ATTACK;
    }

    public override void Enter()
    {
        Debug.Log("ATTACK: Enter");
        base.Enter();
    }

    public override void Update()
    {
        if(Input.GetKeyDown(KeyCode.I))
        {
            nextState = new Idle(controller, player);
            currentEvent = EVENT.EXIT;
        }
        
        Debug.Log("ATTACK: Update");
        DistanceDetection();

        base.Update();
    }

    public override void Exit()
    {
        Debug.Log("ATTACK: Exit");
        base.Exit();
    }

    void DistanceDetection()
    {
        Vector3 playerPos = new Vector3(player.position.x, controller.transform.position.y, player.position.z);

        float distance = Vector3.Distance(playerPos, controller.transform.position);
        float angle = Vector3.Angle(playerPos - controller.transform.position, controller.transform.forward);

        if (distance >= attackDistance || angle > attackDegree)
        {
            //DETECTED
            nextState = new Chase(controller, player);
            currentEvent = EVENT.EXIT;
        }  
    }
}

public class Chase : State
{
    public float speed = 5f;
    public float rotationSpeed = 5f;

    public float attackDegree = 20f;
    public float attackDistance = 3f;
    public float giveUpDistance = 9f;

    public Chase(AI controller, Transform player) : base (controller, player)
    {
        state = STATE.CHASE;
    }

    public override void Enter()
    {
        Debug.Log("Chase: Enter");
        base.Enter();
    }

    public override void Update()
    {
        if(Input.GetKeyDown(KeyCode.I))
        {
            nextState = new Idle(controller, player);
            currentEvent = EVENT.EXIT;
        }
        
        Pursue();
        DistanceDetection();

        base.Update();
    }

    void Pursue()
    {
        controller.transform.Translate(controller.transform.forward * speed * Time.deltaTime, Space.World);

        Quaternion lookAtWP = Quaternion.LookRotation(player.position - controller.transform.position);
        controller.transform.rotation = Quaternion.Slerp(controller.transform.rotation, lookAtWP, Time.deltaTime * rotationSpeed);  
    }

    void DistanceDetection()
    {
        Vector3 playerPos = new Vector3(player.position.x, controller.transform.position.y, player.position.z);

        float angle = Vector3.Angle(playerPos - controller.transform.position, controller.transform.forward);
        float distance = Vector3.Distance(playerPos, controller.transform.position);

        if (angle <= attackDegree && distance < attackDistance)
        {
            //DETECTED
            nextState = new Attack(controller, player);
            currentEvent = EVENT.EXIT;
        }  

        if (distance >= giveUpDistance)
        {
            nextState = new Patrol(controller, player);
            currentEvent = EVENT.EXIT;
        }
    }

    public override void Exit()
    {
        Debug.Log("Chase: Exit");
        base.Exit();
    }
}
