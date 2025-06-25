using UnityEngine;
using System.Collections.Generic;


public class Volume : MonoBehaviour {
    // Defines the dimension of the container
    [Range(1.0f, 100.0f)] 
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

    // Defines the centre of flocks
    public Vector3 centreOfMass;

    Vector3 size;

    void Start() {

        // * Define the walls
        // * Generate N number of boids from the prefab class
        // * The start function in the prefab should give it it's initial properties

        this.size = new Vector3(x, y, z);
        this.transform.localScale = new Vector3(Mathf.Pow(x, 1.0f / 3.0f), 1, Mathf.Pow(y, 1.0f / 3.0f));

        this.walls = DefineWalls(size);
        this.boids = PopulateWithBoids();
    }

    void Update() {
        foreach (GameObject boid in boids) {
            boid.transform.position += boid.GetComponent<Boid>().velocity;
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
        if (boid.transform.position.x <= -x / 2f || boid.transform.position.x >= x / 2f) {
            Debug.Log("x");
            boid.transform.position = new Vector3(boid.transform.position.x * -1, boid.transform.position.y, boid.transform.position.z);
        };
        if (boid.transform.position.y <= -y / 2f || boid.transform.position.y >= y / 2f) {
            Debug.Log("y");
            boid.transform.position = new Vector3(boid.transform.position.x, boid.transform.position.y * -1, boid.transform.position.z);
        };
    }

    List<GameObject> PopulateWithBoids() {
        List<GameObject> boids = new List<GameObject>();
        for (int i = 0; i < numBoids; i++) {
            Vector3 position = new Vector3(Random.Range(-x / 2f, x / 2f), Random.Range(-y / 2f, y / 2f), 0);
            boids.Add(Instantiate(boidPrefab, position, Quaternion.identity));
        }
        return boids;
    }

    Vector3 CentreOfMass() {
        Vector3 centreOfMass = new Vector3();
        foreach (GameObject boid in boids) {
            centreOfMass += boid.transform.position;
        }
        centreOfMass /= boids.Count;
        return centreOfMass;
    }
}
