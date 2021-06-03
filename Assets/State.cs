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
    public AI bot;
    public Transform player;
   
    public State(AI bot, Transform player)
    {
        this.bot = bot;
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
        //DO THINGS IN ENTER METHOD
        currentEvent = EVENT.UPDATE;        
    }

    public virtual void Update()
    {
        //DO THINGS IN UPDATE METHOD
    }

    public virtual void Exit()
    {
        //DO THINGS IN EXIT METHOD
    }
}

public class Idle : State
{
    public int idleTime = 0;
    public float timer = 0f;

    public float detectionDegree = 60f;
    public float detectionDistance = 7f;

    public Idle(AI bot, Transform player) : base (bot, player)
    {
        state = STATE.IDLE;
    }

    public override void Enter()
    {
        //Random waiting time for Idle event
        idleTime = Random.Range(1, 4);
        base.Enter();
    }

    public override void Update()
    {
        timer += Time.deltaTime;

        //When finished idle time
        if(timer >= idleTime)
        {
            nextState = new Patrol(bot, player);
            currentEvent = EVENT.EXIT;
        }
        
        DistanceDetection();

        base.Update();
    }

    //Measure distance between player and bot
    public void DistanceDetection()
    {
        Vector3 playerPos = new Vector3(player.position.x, bot.transform.position.y, player.position.z);

        float angle = Vector3.Angle(playerPos - bot.transform.position, bot.transform.forward);
        float distance = Vector3.Distance(playerPos, bot.transform.position);

        if (angle <= detectionDegree && distance < detectionDistance)
        {
            //DETECTED
            nextState = new Chase(bot, player);
            currentEvent = EVENT.EXIT;
        }        
    }
    public override void Exit()
    {
        base.Exit();
    }
}

public class Patrol : State
{
    public int waypointIndex = 0;
    public float speed = 5f;
    public float rotationSpeed = 4f;

    public float detectionDegree = 60f;
    public float detectionDistance = 7f;

    public Patrol(AI bot, Transform player) : base (bot, player)
    {
        state = STATE.PATROL;
    }

    //Start on random waypoint
    public override void Enter()
    {
        waypointIndex = Random.Range(0, 4);
        base.Enter();
    }

    public void PatrolWaypoint()
    {
        //Patrol
        if (Vector3.Distance(bot.transform.position, bot.waypoints[waypointIndex].position) < 2f)
        {
            int previousIndex = waypointIndex;
            //Set random waypoint
            waypointIndex = Random.Range(0, 4);

            //if we get same number, increase it one 
            if (previousIndex == waypointIndex)
            {
                waypointIndex++;

                if (waypointIndex >= bot.waypoints.Length)
                {
                    waypointIndex = 0;
                }
            }

            // 40% chance to be idle on waypoint
            if(Random.Range(0, 100) < 40)
            {
                nextState = new Idle(bot, player);
                currentEvent = EVENT.EXIT;
                return;
            }
        }

        //Movement
        bot.transform.Translate(bot.transform.forward * speed * Time.deltaTime, Space.World);

        Quaternion lookAtWP = Quaternion.LookRotation(bot.waypoints[waypointIndex].position - bot.transform.position);
        bot.transform.rotation = Quaternion.Slerp(bot.transform.rotation, lookAtWP, Time.deltaTime * rotationSpeed);        
    }

    //Measure distance between player and bot
    public void DistanceDetection()
    {
        Vector3 playerPos = new Vector3(player.position.x, bot.transform.position.y, player.position.z);

        float angle = Vector3.Angle(playerPos - bot.transform.position, bot.transform.forward);
        float distance = Vector3.Distance(playerPos, bot.transform.position);

        if (angle <= detectionDegree && distance < detectionDistance)
        {
            //DETECTED
            nextState = new Chase(bot, player);
            currentEvent = EVENT.EXIT;
        }        
    }

    public override void Update()
    {
        if(Input.GetKeyDown(KeyCode.I))
        {
            nextState = new Idle(bot, player);
            currentEvent = EVENT.EXIT;
        }
        else if(Input.GetKeyDown(KeyCode.A))
        {
            nextState = new Attack(bot, player);
            currentEvent = EVENT.EXIT;
        }

        PatrolWaypoint();
        DistanceDetection();

        base.Update();
    }

    public override void Exit()
    {
        base.Exit();
    }
}

public class Attack : State
{
    public float attackDistance = 3f;
    public float attackDegree = 20f;

    public Attack(AI bot, Transform player) : base (bot, player)
    {
        state = STATE.ATTACK;
    }

    public override void Enter()
    {
        base.Enter();
    }

    public override void Update()
    {
        if(Input.GetKeyDown(KeyCode.I))
        {
            nextState = new Idle(bot, player);
            currentEvent = EVENT.EXIT;
        }
        
        DistanceDetection();

        base.Update();
    }

    public override void Exit()
    {
        base.Exit();
    }

    void DistanceDetection()
    {
        Vector3 playerPos = new Vector3(player.position.x, bot.transform.position.y, player.position.z);

        float distance = Vector3.Distance(playerPos, bot.transform.position);
        float angle = Vector3.Angle(playerPos - bot.transform.position, bot.transform.forward);

        if (distance >= attackDistance || angle > attackDegree)
        {
            //DETECTED
            nextState = new Chase(bot, player);
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
    public float escapeDistance = 9f;

    public Chase(AI bot, Transform player) : base (bot, player)
    {
        state = STATE.CHASE;
    }

    public override void Enter()
    {
        base.Enter();
    }

    public override void Update()
    {
        if(Input.GetKeyDown(KeyCode.I))
        {
            nextState = new Idle(bot, player);
            currentEvent = EVENT.EXIT;
        }
        
        Pursue();
        DistanceDetection();

        base.Update();
    }

    void Pursue()
    {
        bot.transform.Translate(bot.transform.forward * speed * Time.deltaTime, Space.World);

        Quaternion lookAtWP = Quaternion.LookRotation(player.position - bot.transform.position);
        bot.transform.rotation = Quaternion.Slerp(bot.transform.rotation, lookAtWP, Time.deltaTime * rotationSpeed);  
    }

    void DistanceDetection()
    {
        Vector3 playerPos = new Vector3(player.position.x, bot.transform.position.y, player.position.z);

        float angle = Vector3.Angle(playerPos - bot.transform.position, bot.transform.forward);
        float distance = Vector3.Distance(playerPos, bot.transform.position);

        if (angle <= attackDegree && distance < attackDistance)
        {
            //DETECTED
            nextState = new Attack(bot, player);
            currentEvent = EVENT.EXIT;
        }  

        if (distance >= escapeDistance)
        {
            nextState = new Patrol(bot, player);
            currentEvent = EVENT.EXIT;
        }
    }

    public override void Exit()
    {
        base.Exit();
    }
}
