using UnityEngine;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine.Rendering;
using Unity.Collections;


public class Volume : MonoBehaviour {
    // Defines the dimension of the container
    [Range(1.0f, 500.0f)] 
    public float x, y, z;

    // List of wall vectors. Walk vectors are made up of ({the wall direction (2D)}, {wall start (2D)})
    // Show probably split this list up
    public List<Vector4> walls;

    // Number of boids to be spawned in
    public int numBoids = 10;

    // Define the boid prefab
    public GameObject boidPrefab;

    // List of boids gameobjects in the simulation
    public List<GameObject> boids;

    Vector3 size;

    // Bool turn on and off rays
    public bool drawRays; 
    public bool drawResultants;

    // Strength of how much effect each rule has on the boids
    public float flockingStrength           = 0.0005f;
    public float flockingDistance           = 5f;
    public float avoidingStrength           = 1f;
    public float avoidingDistance           = 5f; 
    public float velocityMatchStregnth      = 1f;
    public float velocityMatchDistance      = 8f;
    public float maxVelocity                = 1f;
    public float maxSteeringVector          = 0.03f;
    public float randomness                 = 0.1f;
    public float collisionViewDst           = 10f;
    public float collisionAvoidanceStrength = 1f;
    public string environmentTag = "Environment";

    public Vector3[] directions;
    public int numDirections = 21;


    void Start() {
        this.size = new Vector3(x, y, z);
        this.transform.localScale = new Vector3(x / 10f, 1, y / 10f);
        this.transform.position   = new Vector3(0, 0, z / 10f);

        this.walls = DefineWalls(size);
        this.boids = PopulateWithBoids();
        this.directions = DefineDirections(numDirections);

        this.GetComponent<BoxCollider>().size = new Vector3(10f, 1, 10f);

        GameObject.Find("Main Camera").GetComponent<Camera>().orthographicSize = x / 2f + 10f;
    }

    void FixedUpdate() {
        // this.centreOfMass = CentreOfMass();
        for (int i = 0; i < boids.Count; i++) {
            GameObject boid = boids[i];
            // KeepWithinWall(boid);

            // Rule 1: Flocking
            Vector3 flockingVector = MoveCentreOfMass(boid);

            // Rule 2: Bird avoidance
            Vector3 avoidanceVector = AvoidBoids(boid);

            // Rule 3: MatchVelocity
            Vector3 matchVelocity = MatchVelocity(boid);

            // Rule 4: Random motion
            Vector3 randomMotion = new Vector3(Random.Range(-1, 1), Random.Range(-1, 1), 0) * randomness;

            // Collision Avoision
            Vector3 collisionAvoision = CollisionAvoision(boid);

            // Final Vector
            Vector3 finalVector = flockingVector + avoidanceVector + matchVelocity + randomMotion + collisionAvoision;

            // Draw rays if the drawRays checked
            if (drawResultants) {
                Debug.DrawRay(boid.transform.position, finalVector, Color.red);
                Debug.DrawRay(boid.transform.position, boid.GetComponent<Boid>().velocity * 10, Color.blue);
            }
            

            // Maximum steering angle
            Vector3 steeringVector = finalVector - boid.GetComponent<Boid>().velocity;

            if (Vector3.Magnitude(steeringVector) > maxSteeringVector) {
                steeringVector = steeringVector.normalized * maxSteeringVector;
                finalVector = steeringVector + boid.GetComponent<Boid>().velocity;
            }
        
            // Debug.Log("Final Vector after steering clamp");
            // Debug.Log(finalVector);

            // Move the boid
            boid.transform.position += (boid.GetComponent<Boid>().velocity + finalVector);
            boid.GetComponent<Boid>().velocity += finalVector; 

            // Make sure velocity has no y component
            boid.GetComponent<Boid>().velocity.z = 0f; 
            boid.transform.position = new Vector3(boid.transform.position.x, boid.transform.position.y, 0f);

            LimitSpeed(boid);
            KeepWithinWall(boid);
        }
    }

    List<Vector4> DefineWalls(Vector3 size) {
        List<Vector4> walls = new List<Vector4>();
        // The walls list contains a list of vector4s ({the wall direction (2D)}, {wall start (2D)})

        Vector4 topWall    = new Vector4(  x,  0, -x / 2f,  y / 2f);
        Vector4 bottomWall = new Vector4( -x,  0,  x / 2f, -y / 2f);
        Vector4 leftWall   = new Vector4(  0,  y, -x / 2f, -y / 2f);
        Vector4 rightWall  = new Vector4(  0, -y,  x / 2f,  y / 2f);

        walls.Add(topWall); 
        walls.Add(bottomWall);
        walls.Add(leftWall);
        walls.Add(rightWall);

        return walls;
    }

    void KeepWithinWall(GameObject boid) {
        // if (boid.transform.position.x <= -x / 2f || boid.transform.position.x >= x / 2f) {
        //     boid.transform.position = new Vector3(boid.transform.position.x * -1, boid.transform.position.y, boid.transform.position.z);
        // };
        // if (boid.transform.position.y <= -y / 2f || boid.transform.position.y >= y / 2f) {
        //     boid.transform.position = new Vector3(boid.transform.position.x, boid.transform.position.y * -1, boid.transform.position.z);
        // };
        if (boid.transform.position.x <= -x / 2f) {
            boid.transform.position = new Vector3(-x / 2f + 0.1f, boid.transform.position.y, boid.transform.position.z);
        };
        if (boid.transform.position.x >= x / 2f) {
            boid.transform.position = new Vector3(x / 2f - 0.1f, boid.transform.position.y, boid.transform.position.z);
        };


        if (boid.transform.position.y <= -y / 2f) {
            boid.transform.position = new Vector3(boid.transform.position.x, -y / 2f + 0.1f, boid.transform.position.z);
        };
        if (boid.transform.position.y >= y / 2f) {
            boid.transform.position = new Vector3(boid.transform.position.x, y / 2f - 0.1f, boid.transform.position.z);
        };
    }

    List<GameObject> PopulateWithBoids() {
        List<GameObject> boids = new List<GameObject>();
        for (int i = 0; i < numBoids; i++) {
            Vector3 position = new Vector3(Random.Range(-x / 2f * 0.8f, x / 2f * 0.8f), Random.Range(-y / 2f * 0.8f, y / 2f * 0.8f), 0);
            boids.Add(Instantiate(boidPrefab, position, Quaternion.identity));
        }
        return boids;
    }


    Vector3 MoveCentreOfMass(GameObject boid) {
        List<GameObject> othersBoids = new List<GameObject>(boids);
        othersBoids.Remove(boid);

        Vector3 centreOfMass = new Vector3();

        int i = 0;
        foreach (GameObject otherBoid in othersBoids) {
            if (Vector3.Distance(boid.transform.position, otherBoid.transform.position) < flockingDistance) {
                centreOfMass += otherBoid.transform.position;
                i += 1;
            }            
        }

        if (i != 0) {
            centreOfMass /= i;
            return (centreOfMass - boid.transform.position) * flockingStrength; 
        } else {
            return new Vector3(0, 0, 0);
        }
    }

    Vector3 AvoidBoids(GameObject boid) {
        List<GameObject> othersBoids = new List<GameObject>(boids);
        othersBoids.Remove(boid);

        Vector3 avoidanceVector = new Vector3(0, 0, 0);

        foreach (GameObject otherBoid in othersBoids) {
            if (Vector3.Distance(boid.transform.position, otherBoid.transform.position) < avoidingDistance) {
                avoidanceVector -= (otherBoid.transform.position - boid.transform.position) * avoidingStrength;
            }            
        }

        return avoidanceVector * collisionAvoidanceStrength;
    }

    Vector3 MatchVelocity(GameObject boid) {
        List<GameObject> othersBoids = new List<GameObject>(boids);
        othersBoids.Remove(boid);

        Vector3 percievedVelocity = new Vector3(0, 0, 0);
        
        foreach (GameObject otherBoid in othersBoids) {
            if (Vector3.Distance(boid.transform.position, otherBoid.transform.position) < velocityMatchDistance) {
                percievedVelocity += otherBoid.GetComponent<Boid>().velocity;
            }            
        }

        percievedVelocity = percievedVelocity / othersBoids.Count;
        return (percievedVelocity - boid.GetComponent<Boid>().velocity) * velocityMatchStregnth;
    }

    void LimitSpeed(GameObject boid) {
        if (boid.GetComponent<Boid>().velocity.magnitude > maxVelocity) {
            boid.GetComponent<Boid>().velocity = boid.GetComponent<Boid>().velocity.normalized * maxVelocity;
        }
    }

    Vector3[] DefineDirections(int numDirections) {
        Vector3[] directions = new Vector3[numDirections];

        float pow = 0.5f;
        float turnFraction = 0.618033f;
        // float dst = Mathf.Pow(i / (numDirections - 1f), pow);
        float angleIncrement = 2 * Mathf.PI * turnFraction;

        for (int i = 0; i < numDirections; i++) {
            float angle = angleIncrement * i;

            float x = Mathf.Cos(angle);
            float y = Mathf.Sin(angle);

            directions[i] = new Vector3(x, 0, y);
            // Debug.Log(angle);
            // Debug.DrawRay(transform.position, transform.TransformDirection(directions[i]) * 5f, Color.yellow); 
        }

        return directions;
    }

    Vector3 CollisionAvoision(GameObject boid) {
        // // Prepare the command array
        // var raycastCommands = new NativeArray<RaycastCommand>(numDirections, Allocator.TempJob);
        // for (int i = 0; i < numDirections; i++)
        // {
        //     // Example: Cast a ray from the object's position forward
        //     raycastCommands[i] = new RaycastCommand(transform.position, directions[i], collisionViewDst);
        //     Debug.DrawRay(transform.position, directions[i] * collisionViewDst, Color.red, f);
        // }

        // // Prepare the results array
        // var raycastResults = new NativeArray<RaycastHit>(numDirections, Allocator.TempJob);

        // // Schedule the batch raycast job
        // JobHandle handle = RaycastCommand.ScheduleBatch(raycastCommands, raycastResults, 1, 1);

        // // Complete the job and process the results
        // handle.Complete();

        // for (int i = 0; i < numDirections; i++) {
        //     int index = i;
        //     if (raycastResults[index].collider != null) {
        //         // Process the hit
        //         Debug.Log($"Raycast {i} hit: {raycastResults[index].collider.name}");
        //     } else {
        //         // No more hits for this raycast
        //         Debug.Log("No hits");
        //         break;
        //     }
        // }


        // // Dispose of the native collections
        // raycastCommands.Dispose();
        // raycastResults.Dispose();

        Vector3 avoidanceVector = new Vector3(0, 0, 0);
        Ray ray = new Ray(boid.transform.position + boid.GetComponent<Boid>().velocity * collisionViewDst, -boid.GetComponent<Boid>().velocity * collisionViewDst);

        if (drawRays) {
            Debug.DrawRay(boid.transform.position + boid.GetComponent<Boid>().velocity * collisionViewDst, -boid.GetComponent<Boid>().velocity * collisionViewDst, Color.green);
        }

        RaycastHit hit = new RaycastHit();
        Physics.Raycast(ray, out hit);

        // If the ray collided with a wall
        if (hit.collider != null && hit.collider.tag == environmentTag) {
            // Debug.Log("Hit");
            float distance = (hit.point - boid.transform.position).magnitude;

            if (distance <= collisionViewDst) {
                if (hit.point.x >= (x * 0.5f) || hit.point.x <= -(x * 0.5f)) {
                    // Debug.Log("Hit point:");
                    // Debug.Log(hit.point);
                    if (hit.point.y >=  0.5f * (y * 0.5f)) {
                        avoidanceVector = new Vector3((hit.point.x / -(x * 0.5f)) * 1f, -1f, 0) * (1f / distance);
                    }
                    else if (hit.point.y <=  0.5f * -(y * 0.5f)) {
                        avoidanceVector = new Vector3((hit.point.x / -(x * 0.5f)) * 1f, 1f, 0) * (1f / distance);
                    }
                    else {
                        avoidanceVector = Vector3.Reflect(boid.GetComponent<Boid>().velocity, hit.normal).normalized * (1f / distance) * collisionAvoidanceStrength;
                    }
                } 
                if (hit.point.y >= (y * 0.5f) || hit.point.y <= -(y * 0.5f)) {
                    // Debug.Log("Hit point");
                    // Debug.Log(hit.point);
                    if (hit.point.x >=  0.5f * (x * 0.5f)) {
                        avoidanceVector = new Vector3(-1f, (hit.point.y / -(y * 0.5f)) * 1f, 0) * (1f / distance);
                    }
                    else if (hit.point.x <=  0.5f * -(x * 0.5f)) {
                        avoidanceVector = new Vector3(1f, (hit.point.y / -(y * 0.5f)) * 1f, 0) * (1f / distance);
                    }
                    else {
                        avoidanceVector = Vector3.Reflect(boid.GetComponent<Boid>().velocity, hit.normal).normalized * (1f / distance) * collisionAvoidanceStrength;
                    }
                } 
            }
            
            // avoidanceVector = new Vector3(avoidanceVector.x, avoidanceVector.z, avoidanceVector.y);
            // Debug.Log("Returned Avoidance Vector");
            // Debug.Log(avoidanceVector);

        }
        return avoidanceVector;
    }
}
