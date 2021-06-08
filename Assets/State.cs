using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


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
    public NavMeshAgent agent;
    public Transform player;
   
    public State(AI bot, Transform player, NavMeshAgent agent)
    {
        this.bot = bot;
        this.player = player;
        this.agent = agent;
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
    public int MinIdleTime = 2;
    public int MaxIdleTime = 4;
    public float timer = 0f;
    public float rotationSpeed = 20f;

    public float detectionDegree = 60f;
    public float detectionDistance = 7f;

    public Idle(AI bot, Transform player, NavMeshAgent agent) : base (bot, player, agent)
    {
        state = STATE.IDLE;
    }

    public override void Enter()
    {
        //Random waiting time for Idle event
        idleTime = Random.Range(MinIdleTime, MaxIdleTime + 1);
        base.Enter();
    }

    public override void Update()
    {
        timer += Time.deltaTime;

        LookAround();

        //When finished idle time
        if (timer >= idleTime)
        {
            nextState = new Patrol(bot, player, agent);
            currentEvent = EVENT.EXIT;
        }

        DistanceDetection();

        base.Update();
    }

    private void LookAround()
    {
        if (timer < idleTime / 2)
        {
            bot.transform.Rotate(new Vector3(0f, rotationSpeed * Time.deltaTime, 0f), Space.World);
        }
        else
        {
            bot.transform.Rotate(new Vector3(0f, -rotationSpeed * Time.deltaTime, 0f), Space.World);
        }
    }

    //Measure distance between player and bot
    public void DistanceDetection()
    {
        Vector3 playerPos = new Vector3(player.position.x, bot.transform.position.y, player.position.z);

        float distance = Vector3.Distance(playerPos, bot.transform.position);

        if (distance < detectionDistance)
        {
        
            float angle = Vector3.Angle(playerPos - bot.transform.position, bot.transform.forward);
            
            if (angle <= detectionDegree)
            {
                Vector3 dir = (playerPos - bot.transform.position);
                
                Ray ray = new Ray(bot.transform.position, dir);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {
                    bot.lineColor = Color.yellow;

                    if (hit.transform.CompareTag("Player"))
                    {
                        //DETECTED
                        nextState = new Chase(bot, player, agent);
                        bot.lineColor = Color.red;
                        currentEvent = EVENT.EXIT;                        
                    }
                }
            }
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
    public float speed = 3f;
    public float rotationSpeed = 4f;

    public float detectionDegree = 60f;
    public float detectionDistance = 7f;

    public Patrol(AI bot, Transform player, NavMeshAgent agent) : base (bot, player, agent)
    {
        state = STATE.PATROL;
    }

    //Start on random waypoint
    public override void Enter()
    {
        waypointIndex = Random.Range(0, 4);
        agent.speed = speed;
        base.Enter();
    }

    public void PatrolWaypoint()
    {
        //Patrol
        if (Vector3.Distance(bot.transform.position, bot.waypoints[waypointIndex].position) < 1f)
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
                nextState = new Idle(bot, player, agent);
                currentEvent = EVENT.EXIT;
                return;
            }
        }

        agent.SetDestination(bot.waypoints[waypointIndex].position);    

        /*
        //Movement
        bot.transform.Translate(bot.transform.forward * speed * Time.deltaTime, Space.World);

        Quaternion lookAtWP = Quaternion.LookRotation(bot.waypoints[waypointIndex].position - bot.transform.position);
        bot.transform.rotation = Quaternion.Slerp(bot.transform.rotation, lookAtWP, Time.deltaTime * rotationSpeed);       
        */ 
    }

    //Measure distance between player and bot
    public void DistanceDetection()
    {
        Vector3 playerPos = new Vector3(player.position.x, bot.transform.position.y, player.position.z);

        float distance = Vector3.Distance(playerPos, bot.transform.position);

        if (distance < detectionDistance)
        {
            float angle = Vector3.Angle(playerPos - bot.transform.position, bot.transform.forward);
    
            if (angle <= detectionDegree)
            {
                Vector3 dir = (playerPos - bot.transform.position);
                
                Ray ray = new Ray(bot.transform.position, dir);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {
                    bot.lineColor = Color.yellow;

                    if (hit.transform.CompareTag("Player"))
                    {
                        //DETECTED
                        nextState = new Chase(bot, player, agent);
                        bot.lineColor = Color.red;
                        currentEvent = EVENT.EXIT;                        
                    }
                }
            }
        }  


/*
        if (angle <= detectionDegree && distance < detectionDistance)
        {
            //DETECTED
            nextState = new Chase(bot, player, agent);
            currentEvent = EVENT.EXIT;
        }    
        */    
    }

    public override void Update()
    {
        if(Input.GetKeyDown(KeyCode.I))
        {
            nextState = new Idle(bot, player, agent);
            currentEvent = EVENT.EXIT;
        }
        else if(Input.GetKeyDown(KeyCode.A))
        {
            nextState = new Attack(bot, player, agent);
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
    public float rotationSpeed = 5f;

    public Attack(AI bot, Transform player, NavMeshAgent agent) : base (bot, player, agent)
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
            nextState = new Idle(bot, player, agent);
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

        if (distance >= attackDistance)
        {
            //DETECTED
            nextState = new Chase(bot, player, agent);
            currentEvent = EVENT.EXIT;
        }  

        if (angle > attackDegree)
        {
            Quaternion lookAtWP = Quaternion.LookRotation(playerPos - bot.transform.position);
            bot.transform.rotation = Quaternion.Slerp(bot.transform.rotation, lookAtWP, Time.deltaTime * rotationSpeed);   
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

    public Chase(AI bot, Transform player, NavMeshAgent agent) : base (bot, player,agent)
    {
        state = STATE.CHASE;
    }

    public override void Enter()
    {
        agent.speed = speed;
        base.Enter();
    }

    public override void Update()
    {
        if(Input.GetKeyDown(KeyCode.I))
        {
            nextState = new Idle(bot, player,agent);
            currentEvent = EVENT.EXIT;
        }
        
        Pursue();
        DistanceDetection();

        base.Update();
    }

    void Pursue()
    {
        agent.SetDestination(player.position);

        /*
        bot.transform.Translate(bot.transform.forward * speed * Time.deltaTime, Space.World);

        Quaternion lookAtWP = Quaternion.LookRotation(player.position - bot.transform.position);
        bot.transform.rotation = Quaternion.Slerp(bot.transform.rotation, lookAtWP, Time.deltaTime * rotationSpeed);  
        */
    }

    void DistanceDetection()
    {
        Vector3 playerPos = new Vector3(player.position.x, bot.transform.position.y, player.position.z);

        float angle = Vector3.Angle(playerPos - bot.transform.position, bot.transform.forward);
        float distance = Vector3.Distance(playerPos, bot.transform.position);

        if (distance < attackDistance)
        {
            //DETECTED
            agent.ResetPath();
            nextState = new Attack(bot, player, agent);
            currentEvent = EVENT.EXIT;
        }  

        if (distance >= escapeDistance)
        {
            nextState = new Patrol(bot, player, agent);
            currentEvent = EVENT.EXIT;
        }
    }

    public override void Exit()
    {
        base.Exit();
    }
}
