using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Pathfinding;

public class NPC : MonoBehaviour
{
    // Infection Status
    public bool infected = false;
    bool findHero = false;
    bool isFindingHero = false;
    List<GameObject> heroList;
    float pathUpdateTimeSince = 0f;

    Path path;
    Seeker seeker;
    CharacterController controller;
    Vector3 targetPos = new Vector3(40, 0, 40);
    float speed = 100;
    float nextWaypointDistance = 3;
    int currentWaypoint = 0;
    
    bool startPathCalled = false;

    public void StartPath()
    {
        if (Network.isServer)
        {
            seeker = GetComponent<Seeker>();
            controller = GetComponent<CharacterController>();

            seeker.StartPath(transform.position, targetPos, OnPathComplete);
        }
    }

    void OnGUI()
    {
        if (Network.isServer)
            GUI.Label(new Rect(Screen.width - 150, 80, 300, 20), "NPC: PLAYERS Count: " + NetworkManager.Instance.PLAYERS.Count);
    }

    NetworkManager.PLAYER FindInstigator(Collider c)
    {
        NetworkManager.PLAYER p = NetworkManager.Instance.PLAYERS.Find(i => i.player == c.networkView.owner);
        return p;
    }

    Vector3 FindTargetPos()
    {
        NetworkManager.PLAYER target;
        target = NetworkManager.Instance.PLAYERS.Find(i => i.playerType == 2);
        if (target.pObject == null)
        {
            Debug.Log("NPC can't find a target. Is there a TYPE 2 player in-game?");
            return targetPos;
        }
        else
            return target.pObject.transform.position;
    }

    void OnTriggerEnter(Collider c)
    {
        if (Network.isServer && c.gameObject.tag == "Player")
        {
            NetworkManager.PLAYER p = FindInstigator(c);
            Debug.Log("Player: " + p.playerID + " infected the NPC");

            infected = true;
            targetPos = FindTargetPos();

            if (!isFindingHero)
                findHero = true;
            
            Vector3 green = new Vector3(0, 1, 0);
            networkView.RPC("SetColor", RPCMode.AllBuffered, green);
        }
    }

    void GetHeroList()
    {
        heroList = new List<GameObject>();
        GameObject[] playerList;
        playerList = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in playerList)
        {
            if (player.GetComponent<Player>().type == 2)
                heroList.Add(player);
        }
    }

    Vector3 FindHero()
    {
        // Hero list should be dynamic...And refresh with new players.
        Debug.Log("Count: " + heroList.Count);
        int r = Random.Range(0, heroList.Count());
        Vector3 pos;
        if (heroList[r].transform)
            pos = heroList[r].transform.position;
        else
        {
            GetHeroList();
            pos = heroList[r].transform.position;
        }
        isFindingHero = true;
        return pos;
    }

    public void OnPathComplete(Path p)
    {
        if (Network.isServer)
        {
            Debug.Log("Yay, we got a path back. Did it have an error? " + p.error);

            if (!p.error)
            {
                path = p;
                currentWaypoint = 0;
            }
        }
    }

    void Update()
    {
        if (NetworkManager.Instance.serverStarted && !startPathCalled)
        {
            StartPath();
            startPathCalled = true;
        }
        else if (Network.isServer && NetworkManager.Instance.serverStarted && startPathCalled && findHero)
        {
            if ((pathUpdateTimeSince += Time.deltaTime) > 1.1f)
            {
                pathUpdateTimeSince = 0f;
                targetPos = FindTargetPos();
                seeker.StartPath(transform.position, targetPos, OnPathComplete);
                Debug.Log("Infected NPC now tracking enemy hero at position: " + targetPos);
            }
        }
    }

    void FixedUpdate()
    {
        if (Network.isServer)
        {
            if (path == null)
            {
                Debug.Log("No path");
                return;
            }

            if (currentWaypoint >= path.vectorPath.Count)
            {
                Debug.Log("End Of Path Reached");
                return;
            }

            // Direction to the next waypoint
            Vector3 dir = (path.vectorPath[currentWaypoint] - transform.position).normalized;
            dir *= speed * Time.fixedDeltaTime;
            controller.SimpleMove(dir);

            // Check if we are close enough to the next waypoint, if we are, proceed to follow the next waypoint
            if (Vector3.Distance(transform.position, path.vectorPath[currentWaypoint]) < nextWaypointDistance / 2)
            {
                currentWaypoint++;
                return;
            }
        }
    }

    [RPC]
    void SetColor(Vector3 c)
    {
        renderer.material.color = new Color(c.x, c.y, c.z, 1);
    }
}